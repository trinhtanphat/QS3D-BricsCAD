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
    internal sealed class BeamStirrupBuildResult
    {
        public int Elements { get; set; }
        public int Stirrups { get; set; }
    }

    internal static class BeamStirrupSolidBuilder
    {
        private const string HandlesKey = "GeneratedBeamStirrupHandles";
        private const int MaxStirrupsPerElement = 1200;
        private const int MaxStirrupsPerBatch = 4000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double DiameterMm { get; set; }
            public double ActualSpacingM { get; set; }
            public double CenterlineLengthM { get; set; }
            public double PolylineLengthM { get; set; }
            public double BendRadiusM { get; set; }
            public double HookLengthM { get; set; }
            public double HookTailAngleDeg { get; set; }
            public bool HasHookTails { get; set; }
            public string Notation { get; set; } = string.Empty;
        }

        public static BeamStirrupBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new BeamStirrupBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds())
                try { selectedHandles.Add(id.Handle.ToString()); } catch { }

            var elements = project.Elements
                .Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (elements.Count == 0) return new BeamStirrupBuildResult();

            var duplicateSelectedSource = elements
                .SelectMany(element => element.SourceHandles
                    .Where(selectedHandles.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(handle => new { Handle = handle, Element = element.Id }))
                .GroupBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Select(x => x.Element).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).Count() > 1);
            if (duplicateSelectedSource != null)
                throw new InvalidOperationException("Beam source " + duplicateSelectedSource.Key + " đang thuộc nhiều QS3D element; sửa semantic ownership trước khi dựng stirrup 3D.");

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var batchCount = 0;
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
                        var source = OpenSelectedBeamSource(document, transaction, element, selectedHandles);
                        if (source == null) continue;
                        if (!element.Properties.TryGetValue("RebarStirrupNotation", out var notation) || string.IsNullOrWhiteSpace(notation))
                            throw new InvalidOperationException(element.Id + " chưa có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8).");

                        var groups = RebarNotationParser.Parse(notation);
                        if (groups.Count != 1) throw new InvalidOperationException(element.Id + "/RebarStirrupNotation chỉ hỗ trợ một nhóm diameter/count-or-spacing cho một stirrup set.");
                        var group = groups[0];
                        if (!group.Quantity.HasValue && !group.SpacingMm.HasValue)
                            throw new InvalidOperationException(element.Id + "/RebarStirrupNotation phải có count hoặc spacing; diameter-only không đủ để đặt đai.");
                        if (group.Quantity.HasValue && group.SpacingMm.HasValue)
                            throw new InvalidOperationException(element.Id + "/RebarStirrupNotation không được đồng thời có count và spacing.");

                        var family = project.FindFamily(element.FamilyId);
                        var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WidthM", .3d), element.Id + "/WidthM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", .5d), element.Id + "/HeightM");
                        var sectionCoverM = CadGeometryGuard.Number(element, family, "RebarStirrupCoverM", CadGeometryGuard.Number(element, family, "RebarCoverM", .025d));
                        var endCoverM = CadGeometryGuard.Number(element, family, "RebarStirrupEndCoverM", sectionCoverM);
                        var bendRadiusM = CadGeometryGuard.Number(element, family, "RebarStirrupBendRadiusM", 0d);
                        var hookLengthM = CadGeometryGuard.Number(element, family, "RebarStirrupHookLengthM", 0d);
                        var hookTailAngleDeg = CadGeometryGuard.Number(element, family, "RebarStirrupHookTailAngleDeg", 0d);
                        var maximumSagittaM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "RebarStirrupMaximumSagittaM", .001d), element.Id + "/RebarStirrupMaximumSagittaM");
                        if (sectionCoverM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupCoverM phải >= 0.");
                        if (endCoverM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupEndCoverM phải >= 0.");
                        if (bendRadiusM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupBendRadiusM phải >= 0.");
                        if (hookLengthM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupHookLengthM phải >= 0.");
                        var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);

                        var dx = CadGeometryGuard.Subtract(source.EndPoint.X, source.StartPoint.X, element.Id + "/beam dx");
                        var dy = CadGeometryGuard.Subtract(source.EndPoint.Y, source.StartPoint.Y, element.Id + "/beam dy");
                        var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/beam length");
                        if (lengthDrawing <= 1e-9d) throw new InvalidOperationException("Beam LINE quá ngắn cho stirrup 3D: " + element.Id);
                        var zDelta = CadGeometryGuard.Subtract(source.EndPoint.Z, source.StartPoint.Z, element.Id + "/beam dz");
                        var zTolerance = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/stirrup planarity tolerance"), element.Id + "/stirrup planarity tolerance drawing units");
                        if (Math.Abs(zDelta) > zTolerance) throw new InvalidOperationException("Beam stirrup 3D hiện yêu cầu source LINE gần ngang (|ΔZ| <= 0.005 m): " + element.Id);

                        var layout = BeamStirrupLayoutPlanner.Plan(new BeamStirrupLayoutInput
                        {
                            LengthM = CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/beam length"),
                            WidthM = widthM,
                            HeightM = heightM,
                            SectionCoverM = sectionCoverM,
                            EndCoverM = endCoverM,
                            DiameterMm = group.DiameterMm,
                            Count = group.Quantity,
                            SpacingMm = group.SpacingMm,
                            BendRadiusM = bendRadiusM,
                            MaximumSagittaM = maximumSagittaM,
                            HookLengthM = hookLengthM,
                            HookTailAngleDeg = hookTailAngleDeg
                        });
                        if (layout.Count > MaxStirrupsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxStirrupsPerElement + " stirrup/element.");
                        if (batchCount > MaxStirrupsPerBatch - layout.Count) throw new InvalidOperationException("Beam stirrup 3D vượt giới hạn " + MaxStirrupsPerBatch + " stirrup/batch.");
                        batchCount = checked(batchCount + layout.Count);

                        ErasePrevious(document, transaction, project, element, ownership);
                        var update = new PendingUpdate
                        {
                            Element = element,
                            DiameterMm = group.DiameterMm,
                            ActualSpacingM = layout.ActualSpacingM,
                            CenterlineLengthM = layout.CenterlineLengthM,
                            PolylineLengthM = layout.PolylineLengthM,
                            BendRadiusM = layout.BendRadiusM,
                            HookLengthM = hookLengthM,
                            HookTailAngleDeg = hookTailAngleDeg,
                            HasHookTails = layout.HasHookTails,
                            Notation = notation.Trim()
                        };

                        var ux = dx / lengthDrawing;
                        var uy = dy / lengthDrawing;
                        var perpendicular = new Vector3d(-uy, ux, 0d);
                        var midX = CadGeometryGuard.Midpoint(source.StartPoint.X, source.EndPoint.X, element.Id + "/beam mid X");
                        var midY = CadGeometryGuard.Midpoint(source.StartPoint.Y, source.EndPoint.Y, element.Id + "/beam mid Y");
                        var baseZ = CadGeometryGuard.Add(source.StartPoint.Z, CadGeometryGuard.ToDrawingUnits(document, bottomM, element.Id + "/BottomOffsetM"), element.Id + "/beam base Z");
                        var centerZ = CadGeometryGuard.Add(baseZ, CadGeometryGuard.ToDrawingUnits(document, heightM / 2d, element.Id + "/half height"), element.Id + "/beam center Z");
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, group.DiameterMm / 2000d, element.Id + "/stirrup radius"), element.Id + "/stirrup radius drawing");

                        foreach (var stationM in layout.StationOffsetsM)
                        {
                            var station = CadGeometryGuard.ToDrawingUnits(document, stationM, element.Id + "/stirrup station");
                            var deltaX = CadGeometryGuard.Multiply(ux, station, element.Id + "/stirrup station X");
                            var deltaY = CadGeometryGuard.Multiply(uy, station, element.Id + "/stirrup station Y");
                            var center = new Point3d(
                                CadGeometryGuard.Add(midX, deltaX, element.Id + "/stirrup center X"),
                                CadGeometryGuard.Add(midY, deltaY, element.Id + "/stirrup center Y"),
                                centerZ);
                            var stirrup = BuildLoop(document, center, perpendicular, layout.SectionLoop, radius, element.Id + "/stirrup");
                            try
                            {
                                stirrup.Layer = source.Layer;
                                modelSpace.AppendEntity(stirrup);
                                transaction.AddNewlyCreatedDBObject(stirrup, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, stirrup, project, element, HandlesKey);
                                update.Handles.Add(stirrup.Handle.ToString());
                                stirrup = null!;
                            }
                            finally { stirrup?.Dispose(); }
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
                            "Beam stirrup replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            var count = pending.Sum(x => x.Handles.Count);
            return new BeamStirrupBuildResult { Elements = pending.Count, Stirrups = count };
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedBeamStirrupCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupDiameterMm"] = update.DiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupActualSpacingM"] = update.ActualSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupCenterlineLengthM"] = update.CenterlineLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupTotalCenterlineLengthM"] = CadGeometryGuard.Multiply(update.CenterlineLengthM, update.Handles.Count, update.Element.Id + "/stirrup total centerline").ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupPolylineLengthM"] = update.PolylineLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupBendRadiusM"] = update.BendRadiusM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupHookLengthM"] = update.HookLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupHookTailAngleDeg"] = update.HookTailAngleDeg.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedBeamStirrupNotation"] = update.Notation;
            update.Element.Properties["GeneratedBeamStirrupMode"] = update.HasHookTails ? "Beam.Line.RectangularHookedPath" : (update.BendRadiusM > 1e-12d ? "Beam.Line.RectangularRoundedLoop" : "Beam.Line.RectangularClosedLoop");
            update.Element.ClearGeneratedBeamStirrupStale();
            AuditTrail.ForProject(project).Record("geometry.rebar.beam.stirrup", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " stirrups");
        }

        private static Solid3d BuildLoop(Document document, Point3d center, Vector3d horizontal, IReadOnlyList<QS3D.Core.Geometry.Point2> loop, double radius, string label)
        {
            if (loop == null || loop.Count < 2) throw new ArgumentException("Beam stirrup section path is incomplete.", nameof(loop));
            var closed = loop[0].DistanceTo(loop[loop.Count - 1]) <= 1e-12d;
            Solid3d? result = null;
            try
            {
                radius = CadGeometryGuard.Positive(radius, label + "/radius");
                for (var index = 1; index < loop.Count; index++)
                {
                    var start = World(document, center, horizontal, loop[index - 1], label + "/p" + (index - 1));
                    var end = World(document, center, horizontal, loop[index], label + "/p" + index);
                    var dx = CadGeometryGuard.Subtract(end.X, start.X, label + "/segment dx");
                    var dy = CadGeometryGuard.Subtract(end.Y, start.Y, label + "/segment dy");
                    var dz = CadGeometryGuard.Subtract(end.Z, start.Z, label + "/segment dz");
                    var length = CadGeometryGuard.Hypot3(dx, dy, dz, label + "/segment length");
                    if (length <= 1e-9d) throw new InvalidOperationException("Beam stirrup chứa segment rỗng: " + label);
                    var overlap = Math.Min(CadGeometryGuard.Multiply(radius, .75d, label + "/overlap radius"), CadGeometryGuard.Multiply(length, .1d, label + "/overlap length"));
                    var before = closed || index > 1 ? overlap : 0d;
                    var after = closed || index < loop.Count - 1 ? overlap : 0d;
                    var unit = new Vector3d(dx / length, dy / length, dz / length);
                    var extendedStart = new Point3d(
                        CadGeometryGuard.Subtract(start.X, CadGeometryGuard.Multiply(unit.X, before, label + "/overlap X"), label + "/extended X"),
                        CadGeometryGuard.Subtract(start.Y, CadGeometryGuard.Multiply(unit.Y, before, label + "/overlap Y"), label + "/extended Y"),
                        CadGeometryGuard.Subtract(start.Z, CadGeometryGuard.Multiply(unit.Z, before, label + "/overlap Z"), label + "/extended Z"));
                    var extension = CadGeometryGuard.Add(before, after, label + "/combined overlap");
                    var extendedLength = CadGeometryGuard.Add(length, extension, label + "/extended length");
                    var part = Cylinder(document, extendedStart, unit, extendedLength, radius, label + "/segment" + index);
                    if (result == null) { result = part; continue; }
                    try { result.BooleanOperation(BooleanOperationType.BoolUnite, part); }
                    finally { part.Dispose(); }
                }
                if (result == null) throw new InvalidOperationException("Không tạo được beam stirrup loop: " + label);
                var complete = result;
                result = null;
                return complete;
            }
            finally { result?.Dispose(); }
        }

        private static Point3d World(Document document, Point3d center, Vector3d horizontal, QS3D.Core.Geometry.Point2 sectionPoint, string label)
        {
            var horizontalOffset = CadGeometryGuard.ToDrawingUnits(document, sectionPoint.X, label + "/horizontal");
            var verticalOffset = CadGeometryGuard.ToDrawingUnits(document, sectionPoint.Y, label + "/vertical");
            var deltaX = CadGeometryGuard.Multiply(horizontal.X, horizontalOffset, label + "/horizontal X");
            var deltaY = CadGeometryGuard.Multiply(horizontal.Y, horizontalOffset, label + "/horizontal Y");
            return new Point3d(
                CadGeometryGuard.Add(center.X, deltaX, label + "/X"),
                CadGeometryGuard.Add(center.Y, deltaY, label + "/Y"),
                CadGeometryGuard.Add(center.Z, verticalOffset, label + "/Z"));
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = CadGeometryGuard.Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Beam stirrup axis không hợp lệ: " + label);
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
                if (rotationAxis.Length > 1e-12d) solid.TransformBy(Matrix3d.Rotation(angle, rotationAxis, Point3d.Origin));
                else if (unit.Z < 0d) solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(startX, startY, startZ)));
                var complete = solid;
                solid = null!;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, HandlesKey);
                if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Generated beam stirrup handle không hợp lệ: " + handle);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated beam stirrup handle " + handle + " không trỏ tới Solid3d. Refusing destructive erase.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, HandlesKey, "erase generated beam stirrup " + handle);
                solid.Erase();
            }
        }

        private static Line? OpenSelectedBeamSource(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Line? selected = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Selected beam source handle không hợp lệ cho " + element.Id + ": " + text);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Line line)) throw new InvalidOperationException(element.Id + " cần source LINE để dựng beam stirrup 3D.");
                if (selected != null) throw new InvalidOperationException(element.Id + " có nhiều selected live source. Chọn đúng một Beam LINE.");
                selected = line;
            }
            return selected;
        }
    }
}