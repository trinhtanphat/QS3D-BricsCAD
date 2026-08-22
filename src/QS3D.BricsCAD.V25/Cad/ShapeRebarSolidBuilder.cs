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
    internal sealed class ShapeRebarBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class ShapeRebarSolidBuilder
    {
        private const string HandlesKey = "GeneratedShapeRebarHandles";
        private const int MaxBarsPerElement = 1200;
        private const int MaxBarsPerBatch = 4000;

        private sealed class PendingElement
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
        }

        private sealed class Placement
        {
            public Point3d Origin { get; set; }
            public Vector3d Axis { get; set; }
            public Vector3d Distribution { get; set; }
            public double Span { get; set; }
            public double Cover { get; set; }
        }

        public static ShapeRebarBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new ShapeRebarBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }
            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds()) { try { selectedHandles.Add(id.Handle.ToString()); } catch { } }
            var elements = project.Elements.Where(x => x.SourceHandles.Any(selectedHandles.Contains) && x.Properties.TryGetValue("RebarNotation", out var n) && !string.IsNullOrWhiteSpace(n)).ToList();
            if (elements.Count == 0) return new ShapeRebarBuildResult();
            var elementIds = new HashSet<string>(elements.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
            var rows = ProjectRebarScheduleBuilder.Build(project).Where(x => elementIds.Contains(x.ElementId)).GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var totalRequested = rows.Values.SelectMany(x => x).Sum(x => (long)x.Quantity);
            if (totalRequested > MaxBarsPerBatch) throw new InvalidOperationException("Shape Rebar 3D vượt giới hạn " + MaxBarsPerBatch + " thanh/batch.");

            var pending = new List<PendingElement>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var element in elements)
                {
                    if (!rows.TryGetValue(element.Id, out var elementRows) || elementRows.Count == 0) continue;
                    var requested = elementRows.Sum(x => (long)x.Quantity);
                    if (requested > MaxBarsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " thanh shape 3D.");
                    var source = OpenFirstSource(document, transaction, element) ?? throw new InvalidOperationException("Không tìm thấy source CAD cho " + element.Id);
                    var placement = ResolvePlacement(document, project, element, source);
                    ErasePrevious(document, transaction, element);
                    var item = new PendingElement { Element = element };
                    var rowIndex = 0;
                    foreach (var row in elementRows)
                    {
                        var path = RebarShapePathBuilder.Build(row.ShapeCode, row.CuttingLengthM, Text(element, "RebarShapeLegsM"), Text(element, "RebarShapeTurnsDeg"));
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, row.DiameterMm / 2000d, element.Id + "/bar radius"), element.Id + "/bar radius drawing units");
                        for (var index = 0; index < row.Quantity; index++)
                        {
                            var offset = DistributionOffset(index, row.Quantity, placement.Span, placement.Cover, radius);
                            var lift = rowIndex * radius * 2.5d;
                            var origin = new Point3d(placement.Origin.X + placement.Distribution.X * offset, placement.Origin.Y + placement.Distribution.Y * offset, placement.Origin.Z + lift);
                            var solid = BuildShape(document, origin, placement.Axis, placement.Distribution, path, radius, element.Id + "/" + row.BarMark);
                            try
                            {
                                solid.Layer = source.Layer;
                                modelSpace.AppendEntity(solid);
                                transaction.AddNewlyCreatedDBObject(solid, true);
                                item.Handles.Add(solid.Handle.ToString());
                                solid = null!;
                            }
                            finally { solid?.Dispose(); }
                        }
                        rowIndex++;
                    }
                    pending.Add(item);
                }
                transaction.Commit();
            }
            foreach (var item in pending)
            {
                item.Element.Properties[HandlesKey] = string.Join(";", item.Handles);
                item.Element.Properties["GeneratedShapeRebarCount"] = item.Handles.Count.ToString(CultureInfo.InvariantCulture);
                item.Element.Properties["GeneratedShapeRebarMode"] = "BBS.ShapePath.SegmentedCylinder";
            }
            var bars = pending.Sum(x => x.Handles.Count);
            if (bars > 0) { project.Touch(); document.Editor.Regen(); }
            return new ShapeRebarBuildResult { Elements = pending.Count, Bars = bars };
        }

        private static Placement ResolvePlacement(Document document, ProjectState project, ProjectElement element, Entity source)
        {
            var family = project.FindFamily(element.FamilyId);
            var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", .025d);
            if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM không được âm.");
            var cover = CadGeometryGuard.ToDrawingUnits(document, coverM, element.Id + "/cover");
            var bottom = CadGeometryGuard.ToDrawingUnits(document, CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d), element.Id + "/bottom");
            if (source is Line line)
            {
                var dx = line.EndPoint.X - line.StartPoint.X; var dy = line.EndPoint.Y - line.StartPoint.Y;
                var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/axis");
                if (length <= 1e-9) throw new InvalidOperationException("Source LINE quá ngắn cho shape rebar: " + element.Id);
                var axis = new Vector3d(dx / length, dy / length, 0d); var distribution = new Vector3d(-axis.Y, axis.X, 0d);
                var spanM = element.Category == ElementCategory.StructuralWall || element.Category == ElementCategory.ArchitecturalWall ? CadGeometryGuard.Number(element, family, "ThicknessM", .2d) : CadGeometryGuard.Number(element, family, "WidthM", .3d);
                var span = CadGeometryGuard.ToDrawingUnits(document, CadGeometryGuard.Positive(spanM, element.Id + "/spanM"), element.Id + "/span");
                return new Placement { Origin = new Point3d(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z + bottom + cover), Axis = axis, Distribution = distribution, Span = span, Cover = cover };
            }
            var extents = source.GeometricExtents; var width = extents.MaxPoint.X - extents.MinPoint.X; var depth = extents.MaxPoint.Y - extents.MinPoint.Y; var alongX = width >= depth;
            return new Placement { Origin = new Point3d(extents.MinPoint.X + cover, extents.MinPoint.Y + cover, extents.MinPoint.Z + bottom + cover), Axis = alongX ? Vector3d.XAxis : Vector3d.YAxis, Distribution = alongX ? Vector3d.YAxis : Vector3d.XAxis, Span = Math.Max(1e-9, alongX ? depth : width), Cover = cover };
        }

        private static Solid3d BuildShape(Document document, Point3d origin, Vector3d axis, Vector3d distribution, RebarShapePath path, double radius, string label)
        {
            var normal = Normalize(axis.CrossProduct(distribution), label + "/normal"); Solid3d? result = null;
            try
            {
                for (var i = 1; i < path.Points.Count; i++)
                {
                    var start = World(document, origin, axis, distribution, normal, path.Points[i - 1], label + "/p" + (i - 1)); var end = World(document, origin, axis, distribution, normal, path.Points[i], label + "/p" + i);
                    var vector = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z); var length = vector.Length;
                    if (double.IsNaN(length) || double.IsInfinity(length) || length <= 1e-9) throw new InvalidOperationException("Shape rebar chứa segment rỗng: " + label);
                    var overlap = Math.Min(radius * .75d, length * .1d); var unit = new Vector3d(vector.X / length, vector.Y / length, vector.Z / length); var before = i == 1 ? 0d : overlap; var after = i == path.Points.Count - 1 ? 0d : overlap;
                    var extendedStart = new Point3d(start.X - unit.X * before, start.Y - unit.Y * before, start.Z - unit.Z * before); var part = Cylinder(document, extendedStart, vector, length + before + after, radius, label + "/s" + i);
                    if (result == null) { result = part; continue; }
                    try { result.BooleanOperation(BooleanOperationType.BoolUnite, part); } finally { part.Dispose(); }
                }
                if (result == null) throw new InvalidOperationException("Không tạo được shape rebar: " + label); var completed = result; result = null; return completed;
            }
            finally { result?.Dispose(); }
        }

        private static Point3d World(Document document, Point3d origin, Vector3d axis, Vector3d distribution, Vector3d normal, RebarShapePoint point, string label)
        {
            var x = CadGeometryGuard.ToDrawingUnits(document, point.X, label + "/x"); var y = CadGeometryGuard.ToDrawingUnits(document, point.Y, label + "/y"); var z = CadGeometryGuard.ToDrawingUnits(document, point.Z, label + "/z");
            return new Point3d(origin.X + axis.X * x + distribution.X * y + normal.X * z, origin.Y + axis.Y * x + distribution.Y * y + normal.Y * z, origin.Z + axis.Z * x + distribution.Z * y + normal.Z * z);
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            var magnitude = direction.Length; if (magnitude <= 1e-12 || double.IsNaN(magnitude) || double.IsInfinity(magnitude)) throw new InvalidOperationException("Rebar axis không hợp lệ: " + label);
            var unit = new Vector3d(direction.X / magnitude, direction.Y / magnitude, direction.Z / magnitude); var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database); solid.CreateFrustum(length, radius, radius, radius); var dot = Math.Max(-1d, Math.Min(1d, unit.Z)); var angle = Math.Acos(dot); var rotationAxis = Vector3d.ZAxis.CrossProduct(unit);
                if (rotationAxis.Length > 1e-12) solid.TransformBy(Matrix3d.Rotation(angle, rotationAxis, Point3d.Origin)); else if (unit.Z < 0d) solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(start.X, start.Y, start.Z))); var completed = solid; solid = null!; return completed;
            }
            finally { solid?.Dispose(); }
        }

        private static double DistributionOffset(int index, int count, double span, double cover, double radius) { if (count <= 1) return 0d; var usable = Math.Max(0d, span - 2d * (cover + radius)); return usable * index / (count - 1d); }
        private static Vector3d Normalize(Vector3d vector, string label) { var length = vector.Length; if (length <= 1e-12 || double.IsNaN(length) || double.IsInfinity(length)) throw new InvalidOperationException("Không xác định được " + label + "."); return new Vector3d(vector.X / length, vector.Y / length, vector.Z / length); }
        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var text in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try { var id = document.Database.GetObjectId(false, new Handle(value), 0); if (id.IsNull || !id.IsValid) continue; var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity; if (entity != null && !entity.IsErased) entity.Erase(); } catch { }
            }
        }
        private static Entity? OpenFirstSource(Document document, Transaction transaction, ProjectElement element)
        {
            foreach (var text in element.SourceHandles)
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                try { var id = document.Database.GetObjectId(false, new Handle(value), 0); if (id.IsNull || !id.IsValid) continue; var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; if (entity != null && !entity.IsErased) return entity; } catch { }
            }
            return null;
        }
        private static string? Text(ProjectElement element, string key) => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }
}
