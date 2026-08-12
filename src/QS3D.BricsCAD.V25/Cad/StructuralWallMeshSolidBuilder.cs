using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class StructuralWallMeshBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class StructuralWallMeshSolidBuilder
    {
        private const string HandlesKey = "GeneratedWallMeshHandles";
        private const string Mode = "StructuralWallMesh";
        private const int MaxBarsPerBatch = 12000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double HorizontalDiameterMm { get; set; }
            public double VerticalDiameterMm { get; set; }
            public double CoverM { get; set; }
            public double HorizontalSpacingM { get; set; }
            public double VerticalSpacingM { get; set; }
            public string Faces { get; set; } = string.Empty;
        }

        public static StructuralWallMeshBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new StructuralWallMeshBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds())
                try { selectedHandles.Add(id.Handle.ToString()); } catch { }

            var elements = project.Elements
                .Where(x => x.Category == ElementCategory.StructuralWall && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (elements.Count == 0) return new StructuralWallMeshBuildResult();

            var duplicateSelectedSource = elements
                .SelectMany(element => element.SourceHandles
                    .Where(selectedHandles.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(handle => new { Handle = handle, Element = element.Id }))
                .GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Select(x => x.Element).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).Count() > 1);
            if (duplicateSelectedSource != null)
                throw new InvalidOperationException("StructuralWall source " + duplicateSelectedSource.Key + " đang thuộc nhiều QS3D element; sửa semantic ownership trước khi dựng wall mesh 3D.");

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var batchBars = 0;
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    foreach (var element in elements)
                    {
                        var line = OpenSelectedWallSource(document, transaction, element, selectedHandles);
                        if (line == null) continue;
                        var family = project.FindFamily(element.FamilyId);
                        var horizontal = ParseDirection(element, "RebarWallHorizontalNotation");
                        var vertical = ParseDirection(element, "RebarWallVerticalNotation");

                        var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, element.Id + "/wall dx");
                        var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, element.Id + "/wall dy");
                        var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, element.Id + "/wall dz");
                        var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/wall length");
                        if (lengthDrawing <= 1e-9d) throw new InvalidOperationException("StructuralWall LINE quá ngắn cho mesh 3D: " + element.Id);
                        var planarityTolerance = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/wall mesh planarity tolerance"), element.Id + "/wall mesh planarity tolerance drawing");
                        if (Math.Abs(dz) > planarityTolerance) throw new InvalidOperationException("StructuralWall mesh 3D hiện yêu cầu source LINE gần ngang (|ΔZ| <= 0.005 m): " + element.Id);
                        var lengthM = CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/wall length");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3d), element.Id + "/HeightM");
                        var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", .2d), element.Id + "/ThicknessM");
                        var coverM = CadGeometryGuard.Number(element, family, "RebarWallCoverM", CadGeometryGuard.Number(element, family, "RebarCoverM", .02d));
                        if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarWallCoverM phải >= 0.");
                        var faces = Text(element, family, "RebarWallFaces", "Both");
                        var includeNear = string.Equals(faces, "Near", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                        var includeFar = string.Equals(faces, "Far", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                        if (!includeNear && !includeFar) throw new InvalidOperationException(element.Id + "/RebarWallFaces phải là Near, Far hoặc Both.");
                        var horizontalClosest = Boolean(element, family, "RebarWallHorizontalClosestToFace", true);
                        var layout = RectangularWallMeshPlanner.Plan(new RectangularWallMeshInput
                        {
                            LengthM = lengthM,
                            HeightM = heightM,
                            ThicknessM = thicknessM,
                            CoverM = coverM,
                            HorizontalDiameterMm = horizontal.DiameterMm,
                            VerticalDiameterMm = vertical.DiameterMm,
                            HorizontalSpacingMm = horizontal.SpacingMm,
                            HorizontalCount = horizontal.Quantity,
                            VerticalSpacingMm = vertical.SpacingMm,
                            VerticalCount = vertical.Quantity,
                            IncludeNear = includeNear,
                            IncludeFar = includeFar,
                            HorizontalClosestToFace = horizontalClosest
                        });
                        if (batchBars > MaxBarsPerBatch - layout.Count) throw new InvalidOperationException("StructuralWall mesh batch vượt giới hạn " + MaxBarsPerBatch + " bar.");
                        batchBars = checked(batchBars + layout.Count);

                        ErasePrevious(document, transaction, project, element, ownership);
                        var ux = dx / lengthDrawing;
                        var uy = dy / lengthDrawing;
                        var axis = new Vector3d(ux, uy, 0d);
                        var normal = new Vector3d(-uy, ux, 0d);
                        var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/wall mid X");
                        var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/wall mid Y");
                        var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var centerOffsetM = CadGeometryGuard.Add(bottomM, heightM / 2d, element.Id + "/wall mesh center offset Z");
                        var centerZ = CadGeometryGuard.Add(line.StartPoint.Z, CadGeometryGuard.ToDrawingUnits(document, centerOffsetM, element.Id + "/wall mesh center offset Z"), element.Id + "/wall mesh center Z");
                        var wallCenter = new Point3d(midX, midY, centerZ);
                        var update = new PendingUpdate
                        {
                            Element = element,
                            HorizontalDiameterMm = horizontal.DiameterMm,
                            VerticalDiameterMm = vertical.DiameterMm,
                            CoverM = coverM,
                            HorizontalSpacingM = layout.HorizontalActualSpacingM,
                            VerticalSpacingM = layout.VerticalActualSpacingM,
                            Faces = includeNear && includeFar ? "Both" : (includeFar ? "Far" : "Near")
                        };
                        foreach (var placement in layout.Bars)
                        {
                            var faceOffset = CadGeometryGuard.ToDrawingUnits(document, placement.FaceOffsetM, element.Id + "/wall mesh face offset");
                            var distributionOffset = CadGeometryGuard.ToDrawingUnits(document, placement.DistributionOffsetM, element.Id + "/wall mesh distribution");
                            var length = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.LengthM, element.Id + "/wall mesh bar length"), element.Id + "/wall mesh bar length drawing");
                            var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.DiameterMm / 2000d, element.Id + "/wall mesh radius"), element.Id + "/wall mesh radius drawing");
                            Point3d start;
                            Vector3d direction;
                            if (placement.Direction == WallMeshDirection.Horizontal)
                            {
                                var cx = CadGeometryGuard.Add(wallCenter.X, CadGeometryGuard.Multiply(normal.X, faceOffset, element.Id + "/wall H face X"), element.Id + "/wall H center X");
                                var cy = CadGeometryGuard.Add(wallCenter.Y, CadGeometryGuard.Multiply(normal.Y, faceOffset, element.Id + "/wall H face Y"), element.Id + "/wall H center Y");
                                var cz = CadGeometryGuard.Add(wallCenter.Z, distributionOffset, element.Id + "/wall H center Z");
                                var half = length / 2d;
                                start = new Point3d(
                                    CadGeometryGuard.Subtract(cx, CadGeometryGuard.Multiply(axis.X, half, element.Id + "/wall H start X offset"), element.Id + "/wall H start X"),
                                    CadGeometryGuard.Subtract(cy, CadGeometryGuard.Multiply(axis.Y, half, element.Id + "/wall H start Y offset"), element.Id + "/wall H start Y"),
                                    CadGeometryGuard.Finite(cz, element.Id + "/wall H start Z"));
                                direction = axis;
                            }
                            else
                            {
                                var xOffset = CadGeometryGuard.Add(
                                    CadGeometryGuard.Multiply(axis.X, distributionOffset, element.Id + "/wall V distribution X"),
                                    CadGeometryGuard.Multiply(normal.X, faceOffset, element.Id + "/wall V face X"),
                                    element.Id + "/wall V X offset");
                                var yOffset = CadGeometryGuard.Add(
                                    CadGeometryGuard.Multiply(axis.Y, distributionOffset, element.Id + "/wall V distribution Y"),
                                    CadGeometryGuard.Multiply(normal.Y, faceOffset, element.Id + "/wall V face Y"),
                                    element.Id + "/wall V Y offset");
                                var cx = CadGeometryGuard.Add(wallCenter.X, xOffset, element.Id + "/wall V center X");
                                var cy = CadGeometryGuard.Add(wallCenter.Y, yOffset, element.Id + "/wall V center Y");
                                start = new Point3d(cx, cy, CadGeometryGuard.Subtract(wallCenter.Z, length / 2d, element.Id + "/wall V start Z"));
                                direction = Vector3d.ZAxis;
                            }
                            Solid3d? bar = CreateCylinder(document, start, direction, length, radius, element.Id + "/wall mesh bar");
                            try
                            {
                                bar.Layer = line.Layer;
                                modelSpace.AppendEntity(bar);
                                transaction.AddNewlyCreatedDBObject(bar, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, bar, project, element, HandlesKey);
                                update.Handles.Add(bar.Handle.ToString());
                                bar = null;
                            }
                            finally { bar?.Dispose(); }
                        }
                        pending.Add(update);
                    }

                    foreach (var update in pending) CommitSemanticUpdate(project, update);
                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "StructuralWall mesh replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new StructuralWallMeshBuildResult { Elements = pending.Count, Bars = pending.Sum(x => x.Handles.Count) };
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedWallMeshCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshHorizontalDiameterMm"] = update.HorizontalDiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshVerticalDiameterMm"] = update.VerticalDiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshMode"] = Mode;
            update.Element.Properties["GeneratedWallMeshHorizontalActualSpacingM"] = update.HorizontalSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshVerticalActualSpacingM"] = update.VerticalSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedWallMeshFaces"] = update.Faces;
            AuditTrail.ForProject(project).Record("geometry.rebar.wall.mesh", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars");
        }

        private static RebarGroup ParseDirection(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var notation) || string.IsNullOrWhiteSpace(notation)) throw new InvalidOperationException(element.Id + " chưa có " + key + " (ví dụ D10@200 hoặc 20D10).");
            var groups = RebarNotationParser.Parse(notation);
            if (groups.Count != 1) throw new InvalidOperationException(element.Id + "/" + key + " chỉ hỗ trợ một group.");
            var group = groups[0];
            if (!group.Quantity.HasValue && !group.SpacingMm.HasValue) throw new InvalidOperationException(element.Id + "/" + key + " phải có count hoặc spacing.");
            if (group.Quantity.HasValue && group.SpacingMm.HasValue) throw new InvalidOperationException(element.Id + "/" + key + " không được đồng thời có count và spacing.");
            return group;
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, HandlesKey);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated StructuralWall mesh handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated StructuralWall mesh handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, HandlesKey, "erase generated StructuralWall mesh " + handle);
                solid.Erase();
            }
        }

        private static Solid3d CreateCylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = CadGeometryGuard.Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("StructuralWall mesh axis không hợp lệ: " + label);
            var unit = new Vector3d(direction.X / magnitude, direction.Y / magnitude, direction.Z / magnitude);
            var startX = CadGeometryGuard.Finite(start.X, label + "/start X");
            var startY = CadGeometryGuard.Finite(start.Y, label + "/start Y");
            var startZ = CadGeometryGuard.Finite(start.Z, label + "/start Z");
            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(length, radius, radius, radius);
                var dot = Math.Max(-1d, Math.Min(1d, unit.Z));
                var angle = Math.Acos(dot);
                var rotationAxis = Vector3d.ZAxis.CrossProduct(unit);
                if (CadGeometryGuard.Hypot3(rotationAxis.X, rotationAxis.Y, rotationAxis.Z, label + "/rotation axis") > 1e-12d)
                    solid.TransformBy(Matrix3d.Rotation(angle, rotationAxis, Point3d.Origin));
                else if (unit.Z < 0d) solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(startX, startY, startZ)));
                var complete = solid;
                solid = null!;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static Line? OpenSelectedWallSource(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Line? selected = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException("Selected StructuralWall source handle không hợp lệ cho " + element.Id + ": " + text);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Line line)) throw new InvalidOperationException(element.Id + " cần source LINE để dựng StructuralWall mesh 3D.");
                if (selected != null) throw new InvalidOperationException(element.Id + " có nhiều selected live source. Chọn đúng một StructuralWall LINE.");
                selected = line;
            }
            return selected;
        }

        private static string Text(ProjectElement element, ProjectFamily? family, string key, string fallback)
        {
            if (element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return fallback;
        }

        private static bool Boolean(ProjectElement element, ProjectFamily? family, string key, bool fallback)
        {
            var raw = Text(element, family, key, fallback ? "true" : "false");
            if (bool.TryParse(raw, out var value)) return value;
            if (raw == "1") return true;
            if (raw == "0") return false;
            throw new InvalidOperationException(element.Id + "/" + key + " phải là true/false hoặc 1/0.");
        }
    }
}
