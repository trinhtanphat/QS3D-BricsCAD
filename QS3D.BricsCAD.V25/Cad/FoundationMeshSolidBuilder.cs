using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class FoundationMeshBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class FoundationMeshSolidBuilder
    {
        internal const string HandlesKey = "GeneratedFoundationMeshHandles";
        private const string Mode = "FoundationMeshXY";
        private const string RectangleFootprintMode = "RectangleLocalXY";
        private const string PolygonFootprintMode = "PolygonGlobalXY";
        private const int MaxBarsPerBatch = 12000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double XDiameterMm { get; set; }
            public double YDiameterMm { get; set; }
            public double CoverM { get; set; }
            public double XSpacingM { get; set; }
            public double YSpacingM { get; set; }
            public string Faces { get; set; } = string.Empty;
            public string FootprintMode { get; set; } = string.Empty;
        }

        public static FoundationMeshBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new FoundationMeshBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds())
                try { selectedHandles.Add(id.Handle.ToString()); } catch { }

            var elements = project.Elements
                .Where(x => x.Category == ElementCategory.Foundation && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (elements.Count == 0) return new FoundationMeshBuildResult();

            var duplicateSelectedSource = elements
                .SelectMany(element => element.SourceHandles
                    .Where(selectedHandles.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(handle => new { Handle = handle, Element = element.Id }))
                .GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Select(x => x.Element).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).Count() > 1);
            if (duplicateSelectedSource != null)
                throw new InvalidOperationException("Foundation source " + duplicateSelectedSource.Key + " đang thuộc nhiều QS3D element; sửa semantic ownership trước khi dựng foundation mesh 3D.");

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
                        var polyline = OpenSelectedFoundationSource(document, transaction, element, selectedHandles);
                        if (polyline == null) continue;
                        var family = project.FindFamily(element.FamilyId);
                        var xGroup = ParseDirection(element, family, "RebarFoundationXNotation");
                        var yGroup = ParseDirection(element, family, "RebarFoundationYNotation");
                        var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", .5d), element.Id + "/ThicknessM");
                        var coverM = CadGeometryGuard.Number(element, family, "RebarFoundationCoverM", CadGeometryGuard.Number(element, family, "RebarCoverM", .05d));
                        if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarFoundationCoverM phải >= 0.");
                        var faces = Text(element, family, "RebarFoundationFaces", "Bottom");
                        var includeBottom = string.Equals(faces, "Bottom", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                        var includeTop = string.Equals(faces, "Top", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                        if (!includeBottom && !includeTop) throw new InvalidOperationException(element.Id + "/RebarFoundationFaces phải là Bottom, Top hoặc Both.");
                        var xClosest = Boolean(element, family, "RebarFoundationXClosestToFace", true);
                        var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var centerOffsetM = CadGeometryGuard.Add(bottomM, thicknessM / 2d, element.Id + "/foundation mesh center offset Z");
                        var centerZ = CadGeometryGuard.Add(
                            polyline.Elevation,
                            CadGeometryGuard.ToDrawingUnits(document, centerOffsetM, element.Id + "/foundation mesh center Z"),
                            element.Id + "/foundation mesh world center Z");

                        var rectangle = TryReadRectangle(document, element, polyline);
                        if (rectangle != null)
                        {
                            var layout = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
                            {
                                SpanXM = rectangle.SpanXM,
                                SpanYM = rectangle.SpanYM,
                                ThicknessM = thicknessM,
                                CoverM = coverM,
                                XDiameterMm = xGroup.DiameterMm,
                                YDiameterMm = yGroup.DiameterMm,
                                XSpacingMm = xGroup.SpacingMm,
                                XCount = xGroup.Quantity,
                                YSpacingMm = yGroup.SpacingMm,
                                YCount = yGroup.Quantity,
                                IncludeBottom = includeBottom,
                                IncludeTop = includeTop,
                                XClosestToFace = xClosest
                            });
                            ReserveBatchBars(ref batchBars, layout.Count);
                            ErasePrevious(document, transaction, project, element, ownership);
                            var update = CreateUpdate(element, xGroup, yGroup, coverM, layout.XActualSpacingM, layout.YActualSpacingM, includeBottom, includeTop, RectangleFootprintMode);
                            AppendRectangleBars(document, transaction, modelSpace, polyline, element, rectangle, centerZ, layout, update);
                            GeneratedRebarNativeOwnershipService.MarkFreshGeneratedHandles(document, transaction, project, element, HandlesKey, update.Handles);
                            pending.Add(update);
                            continue;
                        }

                        var footprintM = ReadPolygonFootprint(document, element, polyline);
                        var polygonLayout = PolygonalSlabMeshPlanner.Plan(new PolygonalSlabMeshInput
                        {
                            FootprintM = footprintM,
                            ThicknessM = thicknessM,
                            CoverM = coverM,
                            XDiameterMm = xGroup.DiameterMm,
                            YDiameterMm = yGroup.DiameterMm,
                            XSpacingMm = xGroup.SpacingMm,
                            XCount = xGroup.Quantity,
                            YSpacingMm = yGroup.SpacingMm,
                            YCount = yGroup.Quantity,
                            IncludeBottom = includeBottom,
                            IncludeTop = includeTop,
                            XClosestToFace = xClosest
                        });
                        ReserveBatchBars(ref batchBars, polygonLayout.Count);
                        ErasePrevious(document, transaction, project, element, ownership);
                        var polygonUpdate = CreateUpdate(element, xGroup, yGroup, coverM, polygonLayout.XActualSpacingM, polygonLayout.YActualSpacingM, includeBottom, includeTop, PolygonFootprintMode);
                        AppendPolygonBars(document, transaction, modelSpace, polyline, element, centerZ, polygonLayout, polygonUpdate);
                        GeneratedRebarNativeOwnershipService.MarkFreshGeneratedHandles(document, transaction, project, element, HandlesKey, polygonUpdate.Handles);
                        pending.Add(polygonUpdate);
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
                            "Foundation mesh replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new FoundationMeshBuildResult { Elements = pending.Count, Bars = pending.Sum(x => x.Handles.Count) };
        }

        private static PendingUpdate CreateUpdate(
            ProjectElement element,
            RebarGroup xGroup,
            RebarGroup yGroup,
            double coverM,
            double xSpacingM,
            double ySpacingM,
            bool includeBottom,
            bool includeTop,
            string footprintMode)
        {
            return new PendingUpdate
            {
                Element = element,
                XDiameterMm = xGroup.DiameterMm,
                YDiameterMm = yGroup.DiameterMm,
                CoverM = coverM,
                XSpacingM = xSpacingM,
                YSpacingM = ySpacingM,
                Faces = includeBottom && includeTop ? "Both" : (includeTop ? "Top" : "Bottom"),
                FootprintMode = footprintMode
            };
        }

        private static void AppendRectangleBars(
            Document document,
            Transaction transaction,
            BlockTableRecord modelSpace,
            Polyline source,
            ProjectElement element,
            RectangleFrame frame,
            double centerZ,
            RectangularSlabMeshLayout layout,
            PendingUpdate update)
        {
            foreach (var placement in layout.Bars)
            {
                var run = placement.Direction == SlabMeshDirection.X ? frame.XAxis : frame.YAxis;
                var distribution = placement.Direction == SlabMeshDirection.X ? frame.YAxis : frame.XAxis;
                var distributionOffset = CadGeometryGuard.ToDrawingUnits(document, placement.DistributionOffsetM, element.Id + "/foundation mesh distribution");
                var elevationOffset = CadGeometryGuard.ToDrawingUnits(document, placement.ElevationOffsetM, element.Id + "/foundation mesh elevation");
                var length = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.LengthM, element.Id + "/foundation mesh bar length"), element.Id + "/foundation mesh bar length drawing");
                var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.DiameterMm / 2000d, element.Id + "/foundation mesh bar radius"), element.Id + "/foundation mesh bar radius drawing");
                var center = new Point3d(
                    CadGeometryGuard.Add(frame.Center.X, CadGeometryGuard.Multiply(distribution.X, distributionOffset, element.Id + "/foundation mesh distribution X"), element.Id + "/foundation mesh center X"),
                    CadGeometryGuard.Add(frame.Center.Y, CadGeometryGuard.Multiply(distribution.Y, distributionOffset, element.Id + "/foundation mesh distribution Y"), element.Id + "/foundation mesh center Y"),
                    CadGeometryGuard.Add(centerZ, elevationOffset, element.Id + "/foundation mesh center Z"));
                var half = length / 2d;
                var start = new Point3d(
                    CadGeometryGuard.Subtract(center.X, CadGeometryGuard.Multiply(run.X, half, element.Id + "/foundation mesh start X offset"), element.Id + "/foundation mesh start X"),
                    CadGeometryGuard.Subtract(center.Y, CadGeometryGuard.Multiply(run.Y, half, element.Id + "/foundation mesh start Y offset"), element.Id + "/foundation mesh start Y"),
                    CadGeometryGuard.Finite(center.Z, element.Id + "/foundation mesh start Z"));
                AppendBar(document, transaction, modelSpace, source, element, start, run, length, radius, update);
            }
        }

        private static void AppendPolygonBars(
            Document document,
            Transaction transaction,
            BlockTableRecord modelSpace,
            Polyline source,
            ProjectElement element,
            double centerZ,
            PolygonalSlabMeshLayout layout,
            PendingUpdate update)
        {
            foreach (var placement in layout.Bars)
            {
                var startX = CadGeometryGuard.ToDrawingUnits(document, placement.StartM.X, element.Id + "/foundation polygon mesh start X");
                var startY = CadGeometryGuard.ToDrawingUnits(document, placement.StartM.Y, element.Id + "/foundation polygon mesh start Y");
                var endX = CadGeometryGuard.ToDrawingUnits(document, placement.EndM.X, element.Id + "/foundation polygon mesh end X");
                var endY = CadGeometryGuard.ToDrawingUnits(document, placement.EndM.Y, element.Id + "/foundation polygon mesh end Y");
                var elevationOffset = CadGeometryGuard.ToDrawingUnits(document, placement.ElevationOffsetM, element.Id + "/foundation polygon mesh elevation");
                var startZ = CadGeometryGuard.Add(centerZ, elevationOffset, element.Id + "/foundation polygon mesh start Z");
                var run = new Vector3d(
                    CadGeometryGuard.Subtract(endX, startX, element.Id + "/foundation polygon mesh run X"),
                    CadGeometryGuard.Subtract(endY, startY, element.Id + "/foundation polygon mesh run Y"),
                    0d);
                var length = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.LengthM, element.Id + "/foundation polygon mesh bar length"), element.Id + "/foundation polygon mesh bar length drawing");
                var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.DiameterMm / 2000d, element.Id + "/foundation polygon mesh bar radius"), element.Id + "/foundation polygon mesh bar radius drawing");
                var start = new Point3d(startX, startY, startZ);
                AppendBar(document, transaction, modelSpace, source, element, start, run, length, radius, update);
            }
        }

        private static void AppendBar(
            Document document,
            Transaction transaction,
            BlockTableRecord modelSpace,
            Polyline source,
            ProjectElement element,
            Point3d start,
            Vector3d direction,
            double length,
            double radius,
            PendingUpdate update)
        {
            Solid3d? bar = CreateCylinder(document, start, direction, length, radius, element.Id + "/foundation mesh bar");
            try
            {
                bar.Layer = source.Layer;
                modelSpace.AppendEntity(bar);
                transaction.AddNewlyCreatedDBObject(bar, true);
                update.Handles.Add(bar.Handle.ToString());
                bar = null;
            }
            finally { bar?.Dispose(); }
        }

        private static void ReserveBatchBars(ref int batchBars, int count)
        {
            if (count < 0 || batchBars > MaxBarsPerBatch - count)
                throw new InvalidOperationException("Foundation mesh batch vượt giới hạn " + MaxBarsPerBatch + " bar.");
            batchBars = checked(batchBars + count);
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedFoundationMeshCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshXDiameterMm"] = update.XDiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshYDiameterMm"] = update.YDiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshMode"] = Mode;
            update.Element.Properties["GeneratedFoundationMeshFootprintMode"] = update.FootprintMode;
            update.Element.Properties["GeneratedFoundationMeshXActualSpacingM"] = update.XSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshYActualSpacingM"] = update.YSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedFoundationMeshFaces"] = update.Faces;
            update.Element.ClearGeneratedFoundationMeshStale();
            AuditTrail.ForProject(project).Record(
                "geometry.rebar.foundation.mesh",
                update.Element.Id,
                update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars • " + update.FootprintMode);
        }

        private sealed class RectangleFrame
        {
            public Point3d Center { get; set; }
            public Vector3d XAxis { get; set; }
            public Vector3d YAxis { get; set; }
            public double SpanXM { get; set; }
            public double SpanYM { get; set; }
        }

        private static RectangleFrame? TryReadRectangle(Document document, ProjectElement element, Polyline polyline)
        {
            ValidateCommonFootprint(element, polyline);
            if (polyline.NumberOfVertices != 4) return null;

            var p0 = polyline.GetPoint2dAt(0);
            var p1 = polyline.GetPoint2dAt(1);
            var p2 = polyline.GetPoint2dAt(2);
            var p3 = polyline.GetPoint2dAt(3);
            var xdx = CadGeometryGuard.Subtract(p1.X, p0.X, element.Id + "/foundation X dx");
            var xdy = CadGeometryGuard.Subtract(p1.Y, p0.Y, element.Id + "/foundation X dy");
            var ydx = CadGeometryGuard.Subtract(p2.X, p1.X, element.Id + "/foundation Y dx");
            var ydy = CadGeometryGuard.Subtract(p2.Y, p1.Y, element.Id + "/foundation Y dy");
            var spanXDrawing = CadGeometryGuard.Hypot(xdx, xdy, element.Id + "/foundation X span");
            var spanYDrawing = CadGeometryGuard.Hypot(ydx, ydy, element.Id + "/foundation Y span");
            if (spanXDrawing <= 1e-9d || spanYDrawing <= 1e-9d) return null;

            var ux = xdx / spanXDrawing;
            var uy = xdy / spanXDrawing;
            var vx = ydx / spanYDrawing;
            var vy = ydy / spanYDrawing;
            var orthogonality = Math.Abs(CadGeometryGuard.Add(
                CadGeometryGuard.Multiply(ux, vx, element.Id + "/foundation dot X"),
                CadGeometryGuard.Multiply(uy, vy, element.Id + "/foundation dot Y"),
                element.Id + "/foundation orthogonality"));
            if (orthogonality > 1e-6d) return null;

            var tolerance = CadGeometryGuard.Add(
                CadGeometryGuard.Multiply(Math.Max(spanXDrawing, spanYDrawing), 1e-6d, element.Id + "/foundation tolerance scale"),
                1e-8d,
                element.Id + "/foundation tolerance");
            var expectedP2X = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.X, xdx, element.Id + "/foundation expected P2 X"), ydx, element.Id + "/foundation expected P2 X");
            var expectedP2Y = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.Y, xdy, element.Id + "/foundation expected P2 Y"), ydy, element.Id + "/foundation expected P2 Y");
            var expectedP3X = CadGeometryGuard.Add(p0.X, ydx, element.Id + "/foundation expected P3 X");
            var expectedP3Y = CadGeometryGuard.Add(p0.Y, ydy, element.Id + "/foundation expected P3 Y");
            if (Distance(p2.X, p2.Y, expectedP2X, expectedP2Y, element.Id + "/foundation P2 closure") > tolerance ||
                Distance(p3.X, p3.Y, expectedP3X, expectedP3Y, element.Id + "/foundation P3 closure") > tolerance)
                return null;

            return new RectangleFrame
            {
                Center = new Point3d(
                    CadGeometryGuard.Midpoint(p0.X, p2.X, element.Id + "/foundation center X"),
                    CadGeometryGuard.Midpoint(p0.Y, p2.Y, element.Id + "/foundation center Y"),
                    CadGeometryGuard.Finite(polyline.Elevation, element.Id + "/foundation center Z")),
                XAxis = new Vector3d(ux, uy, 0d),
                YAxis = new Vector3d(vx, vy, 0d),
                SpanXM = CadGeometryGuard.ToMeters(document, spanXDrawing, element.Id + "/foundation X span"),
                SpanYM = CadGeometryGuard.ToMeters(document, spanYDrawing, element.Id + "/foundation Y span")
            };
        }

        private static IReadOnlyList<Point2> ReadPolygonFootprint(Document document, ProjectElement element, Polyline polyline)
        {
            ValidateCommonFootprint(element, polyline);
            if (polyline.Normal.Z < 1d - 1e-9d)
                throw new InvalidOperationException(element.Id + ": polygonal Foundation mesh hiện yêu cầu plan-view POLYLINE có normal +Z.");

            var points = new List<Point2>(polyline.NumberOfVertices);
            for (var index = 0; index < polyline.NumberOfVertices; index++)
            {
                var point = polyline.GetPoint2dAt(index);
                points.Add(new Point2(
                    CadGeometryGuard.ToMeters(document, point.X, element.Id + "/foundation polygon X"),
                    CadGeometryGuard.ToMeters(document, point.Y, element.Id + "/foundation polygon Y")));
            }
            return points.AsReadOnly();
        }

        private static void ValidateCommonFootprint(ProjectElement element, Polyline polyline)
        {
            if (!polyline.Closed || polyline.NumberOfVertices < 3)
                throw new InvalidOperationException(element.Id + ": QS3DFOUNDATIONREBAR3D yêu cầu closed POLYLINE có ít nhất 3 vertex.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException(element.Id + ": Foundation mesh footprint phải nằm trên mặt phẳng XY.");
            for (var index = 0; index < polyline.NumberOfVertices; index++)
                if (Math.Abs(CadGeometryGuard.Finite(polyline.GetBulgeAt(index), element.Id + "/foundation bulge")) > 1e-12d)
                    throw new InvalidOperationException(element.Id + ": polygonal Foundation mesh chưa hỗ trợ bulge/curved boundary; dùng straight-segment closed POLYLINE.");
            CadGeometryGuard.Finite(polyline.Elevation, element.Id + "/foundation elevation");
        }

        private static RebarGroup ParseDirection(ProjectElement element, ProjectFamily? family, string key)
        {
            var notation = Text(element, family, key, string.Empty);
            if (string.IsNullOrWhiteSpace(notation)) throw new InvalidOperationException(element.Id + " chưa có " + key + " (ví dụ D16@200 hoặc 12D16).");
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
                if (ids.Count > 1) throw new InvalidOperationException("Generated foundation mesh handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated foundation mesh handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, HandlesKey, "erase generated foundation mesh " + handle);
                solid.Erase();
            }
        }

        private static Solid3d CreateCylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = CadGeometryGuard.Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Foundation mesh bar axis không hợp lệ: " + label);
            var unit = new Vector3d(direction.X / magnitude, direction.Y / magnitude, direction.Z / magnitude);
            var startX = CadGeometryGuard.Finite(start.X, label + "/start X");
            var startY = CadGeometryGuard.Finite(start.Y, label + "/start Y");
            var startZ = CadGeometryGuard.Finite(start.Z, label + "/start Z");
            Solid3d? solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(length, radius, radius, radius);
                var dot = Math.Max(-1d, Math.Min(1d, unit.Z));
                var angle = Math.Acos(dot);
                var rotationAxis = Vector3d.ZAxis.CrossProduct(unit);
                if (CadGeometryGuard.Hypot3(rotationAxis.X, rotationAxis.Y, rotationAxis.Z, label + "/rotation axis") > 1e-12d)
                    solid.TransformBy(Matrix3d.Rotation(angle, rotationAxis, Point3d.Origin));
                else if (unit.Z < 0d)
                    solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(startX, startY, startZ)));
                var complete = solid;
                solid = null;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static Polyline? OpenSelectedFoundationSource(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Polyline? selected = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Selected foundation source handle không hợp lệ cho " + element.Id + ": " + text);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Polyline polyline))
                    throw new InvalidOperationException(element.Id + " cần source closed plan-view POLYLINE để dựng foundation mesh 3D.");
                if (selected != null)
                    throw new InvalidOperationException(element.Id + " có nhiều selected live source. Chọn đúng một Foundation POLYLINE.");
                selected = polyline;
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

        private static double Distance(double x1, double y1, double x2, double y2, string label)
        {
            var dx = CadGeometryGuard.Subtract(x2, x1, label + "/dx");
            var dy = CadGeometryGuard.Subtract(y2, y1, label + "/dy");
            return CadGeometryGuard.Hypot(dx, dy, label);
        }
    }
}
