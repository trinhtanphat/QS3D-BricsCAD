using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
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

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var batchCount = 0;

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
                    if (sectionCoverM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupCoverM phải >= 0.");
                    if (endCoverM < 0d) throw new InvalidOperationException(element.Id + "/RebarStirrupEndCoverM phải >= 0.");
                    var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);

                    var dx = CadGeometryGuard.Finite(source.EndPoint.X - source.StartPoint.X, element.Id + "/beam dx");
                    var dy = CadGeometryGuard.Finite(source.EndPoint.Y - source.StartPoint.Y, element.Id + "/beam dy");
                    var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/beam length");
                    if (lengthDrawing <= 1e-9d) throw new InvalidOperationException("Beam LINE quá ngắn cho stirrup 3D: " + element.Id);
                    var zDelta = CadGeometryGuard.Finite(source.EndPoint.Z - source.StartPoint.Z, element.Id + "/beam dz");
                    var zTolerance = CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/stirrup planarity tolerance");
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
                        SpacingMm = group.SpacingMm
                    });
                    if (layout.Count > MaxStirrupsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxStirrupsPerElement + " stirrup/element.");
                    batchCount = checked(batchCount + layout.Count);
                    if (batchCount > MaxStirrupsPerBatch) throw new InvalidOperationException("Beam stirrup 3D vượt giới hạn " + MaxStirrupsPerBatch + " stirrup/batch.");

                    ErasePrevious(document, transaction, element, ownership);
                    var update = new PendingUpdate
                    {
                        Element = element,
                        DiameterMm = group.DiameterMm,
                        ActualSpacingM = layout.ActualSpacingM,
                        Notation = notation.Trim()
                    };

                    var ux = dx / lengthDrawing;
                    var uy = dy / lengthDrawing;
                    var perpendicular = new Vector3d(-uy, ux, 0d);
                    var axis = new Vector3d(ux, uy, 0d);
                    var midX = CadGeometryGuard.Midpoint(source.StartPoint.X, source.EndPoint.X, element.Id + "/beam mid X");
                    var midY = CadGeometryGuard.Midpoint(source.StartPoint.Y, source.EndPoint.Y, element.Id + "/beam mid Y");
                    var baseZ = CadGeometryGuard.Add(source.StartPoint.Z, CadGeometryGuard.ToDrawingUnits(document, bottomM, element.Id + "/BottomOffsetM"), element.Id + "/beam base Z");
                    var centerZ = CadGeometryGuard.Add(baseZ, CadGeometryGuard.ToDrawingUnits(document, heightM / 2d, element.Id + "/half height"), element.Id + "/beam center Z");
                    var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, group.DiameterMm / 2000d, element.Id + "/stirrup radius"), element.Id + "/stirrup radius drawing");

                    foreach (var stationM in layout.StationOffsetsM)
                    {
                        var station = CadGeometryGuard.ToDrawingUnits(document, stationM, element.Id + "/stirrup station");
                        var deltaX = CadGeometryGuard.Finite(axis.X * station, element.Id + "/stirrup station X");
                        var deltaY = CadGeometryGuard.Finite(axis.Y * station, element.Id + "/stirrup station Y");
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
                            update.Handles.Add(stirrup.Handle.ToString());
                            stirrup = null!;
                        }
                        finally { stirrup?.Dispose(); }
                    }
                    pending.Add(update);
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
                update.Element.Properties["GeneratedBeamStirrupCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedBeamStirrupDiameterMm"] = update.DiameterMm.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedBeamStirrupActualSpacingM"] = update.ActualSpacingM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedBeamStirrupNotation"] = update.Notation;
                update.Element.Properties["GeneratedBeamStirrupMode"] = "Beam.Line.RectangularClosedLoop";
                update.Element.ClearGeneratedBeamStirrupStale();
            }

            var count = pending.Sum(x => x.Handles.Count);
            if (count > 0)
            {
                project.Touch();
                document.Editor.Regen();
            }
            return new BeamStirrupBuildResult { Elements = pending.Count, Stirrups = count };
        }

        private static Solid3d BuildLoop(Document document, Point3d center, Vector3d horizontal, IReadOnlyList<QS3D.Core.Geometry.Point2> loop, double radius, string label)
        {
            Solid3d? result = null;
            try
            {
                for (var index = 1; index < loop.Count; index++)
                {
                    var start = World(document, center, horizontal, loop[index - 1], label + "/p" + (index - 1));
                    var end = World(document, center, horizontal, loop[index], label + "/p" + index);
                    var vector = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
                    var length = Hypot3(vector.X, vector.Y, vector.Z, label + "/segment length");
                    if (length <= 1e-9d) throw new InvalidOperationException("Beam stirrup chứa segment rỗng: " + label);
                    var overlap = Math.Min(radius * .75d, length * .1d);
                    var unit = new Vector3d(vector.X / length, vector.Y / length, vector.Z / length);
                    var extendedStart = new Point3d(start.X - unit.X * overlap, start.Y - unit.Y * overlap, start.Z - unit.Z * overlap);
                    var part = Cylinder(document, extendedStart, vector, length + overlap * 2d, radius, label + "/segment" + index);
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
            var deltaX = CadGeometryGuard.Finite(horizontal.X * horizontalOffset, label + "/horizontal X");
            var deltaY = CadGeometryGuard.Finite(horizontal.Y * horizontalOffset, label + "/horizontal Y");
            return new Point3d(
                CadGeometryGuard.Add(center.X, deltaX, label + "/X"),
                CadGeometryGuard.Add(center.Y, deltaY, label + "/Y"),
                CadGeometryGuard.Add(center.Z, verticalOffset, label + "/Z"));
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Beam stirrup axis không hợp lệ: " + label);
            var unit = new Vector3d(direction.X / magnitude, direction.Y / magnitude, direction.Z / magnitude);
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
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(start.X, start.Y, start.Z)));
                var complete = solid;
                solid = null!;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static double Hypot3(double x, double y, double z, string label)
        {
            x = Math.Abs(CadGeometryGuard.Finite(x, label + "/x"));
            y = Math.Abs(CadGeometryGuard.Finite(y, label + "/y"));
            z = Math.Abs(CadGeometryGuard.Finite(z, label + "/z"));
            var maximum = Math.Max(x, Math.Max(y, z));
            if (maximum <= 0d) return 0d;
            var sx = x / maximum;
            var sy = y / maximum;
            var sz = z / maximum;
            return CadGeometryGuard.Finite(maximum * Math.Sqrt(sx * sx + sy * sy + sz * sz), label);
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
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
