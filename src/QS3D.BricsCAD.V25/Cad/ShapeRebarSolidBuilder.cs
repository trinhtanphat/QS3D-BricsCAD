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
            public bool DistributionCentered { get; set; }
            public bool AxisStartsAtBoundary { get; set; }
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
            var ownership = GeneratedRebarOwnershipGuard.Build(project);

            var pending = new List<PendingElement>();
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
                        if (!rows.TryGetValue(element.Id, out var elementRows) || elementRows.Count == 0) continue;
                        var requested = elementRows.Sum(x => (long)x.Quantity);
                        if (requested > MaxBarsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " thanh shape 3D.");
                        var source = OpenSelectedSource(document, transaction, element, selectedHandles) ?? throw new InvalidOperationException("Không tìm thấy selected live source CAD cho " + element.Id);
                        var placement = ResolvePlacement(document, project, element, source);
                        ErasePrevious(document, transaction, project, element, ownership);
                        var item = new PendingElement { Element = element };
                        foreach (var row in elementRows)
                        {
                            var path = RebarShapePathBuilder.Build(row.ShapeCode, row.CuttingLengthM, Text(element, "RebarShapeLegsM"), Text(element, "RebarShapeTurnsDeg"));
                            var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, row.DiameterMm / 2000d, element.Id + "/bar radius"), element.Id + "/bar radius drawing units");
                            var distributionPlan = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
                            {
                                Span = placement.Span,
                                Cover = placement.Cover,
                                Radius = radius,
                                Count = row.Quantity,
                                Centered = placement.DistributionCentered
                            });
                            var edgeInset = distributionPlan.CenterClearance;
                            for (var index = 0; index < row.Quantity; index++)
                            {
                                var offset = distributionPlan.Offsets[index];
                                var axisInset = placement.AxisStartsAtBoundary ? edgeInset : 0d;
                                var origin = OffsetPoint(placement.Origin, placement.Axis, axisInset, placement.Distribution, offset, edgeInset, element.Id + "/shape rebar origin");
                                var solid = BuildShape(document, origin, placement.Axis, placement.Distribution, path, radius, element.Id + "/" + row.BarMark);
                                try
                                {
                                    solid.Layer = source.Layer;
                                    modelSpace.AppendEntity(solid);
                                    transaction.AddNewlyCreatedDBObject(solid, true);
                                    GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, solid, project, element, HandlesKey);
                                    item.Handles.Add(solid.Handle.ToString());
                                    solid = null!;
                                }
                                finally { solid?.Dispose(); }
                            }
                        }
                        pending.Add(item);
                    }

                    foreach (var item in pending) CommitSemanticUpdate(project, item);
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
                            "Shape rebar replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            var bars = pending.Sum(x => x.Handles.Count);
            return new ShapeRebarBuildResult { Elements = pending.Count, Bars = bars };
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingElement item)
        {
            item.Element.Properties[HandlesKey] = string.Join(";", item.Handles);
            item.Element.Properties["GeneratedShapeRebarCount"] = item.Handles.Count.ToString(CultureInfo.InvariantCulture);
            item.Element.Properties["GeneratedShapeRebarMode"] = "BBS.ShapePath.SegmentedCylinder";
            item.Element.ClearGeneratedShapeRebarStale();
            AuditTrail.ForProject(project).Record("geometry.rebar.shape", item.Element.Id, item.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars");
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
                var dx = SubtractFinite(line.EndPoint.X, line.StartPoint.X, element.Id + "/axis dx");
                var dy = SubtractFinite(line.EndPoint.Y, line.StartPoint.Y, element.Id + "/axis dy");
                var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/axis");
                if (length <= 1e-9) throw new InvalidOperationException("Source LINE quá ngắn cho shape rebar: " + element.Id);
                var axis = new Vector3d(dx / length, dy / length, 0d);
                var distribution = new Vector3d(-axis.Y, axis.X, 0d);
                var wallLike = IsWallLike(element.Category);
                var spanM = wallLike ? CadGeometryGuard.Number(element, family, "ThicknessM", .2d) : CadGeometryGuard.Number(element, family, "WidthM", .3d);
                var span = CadGeometryGuard.ToDrawingUnits(document, CadGeometryGuard.Positive(spanM, element.Id + "/spanM"), element.Id + "/span");
                return new Placement
                {
                    Origin = Point(line.StartPoint.X, line.StartPoint.Y, AddFinite(line.StartPoint.Z, bottom, element.Id + "/shape rebar base Z"), element.Id + "/line origin"),
                    Axis = axis,
                    Distribution = distribution,
                    Span = span,
                    Cover = cover,
                    DistributionCentered = true,
                    AxisStartsAtBoundary = false
                };
            }

            var extents = source.GeometricExtents;
            var width = SubtractFinite(extents.MaxPoint.X, extents.MinPoint.X, element.Id + "/source width");
            var depth = SubtractFinite(extents.MaxPoint.Y, extents.MinPoint.Y, element.Id + "/source depth");
            if (width <= 1e-9d || depth <= 1e-9d) throw new InvalidOperationException("Source extents quá nhỏ cho shape rebar: " + element.Id);
            var alongX = width >= depth;
            return new Placement
            {
                Origin = Point(extents.MinPoint.X, extents.MinPoint.Y, AddFinite(extents.MinPoint.Z, bottom, element.Id + "/shape rebar base Z"), element.Id + "/extents origin"),
                Axis = alongX ? Vector3d.XAxis : Vector3d.YAxis,
                Distribution = alongX ? Vector3d.YAxis : Vector3d.XAxis,
                Span = alongX ? depth : width,
                Cover = cover,
                DistributionCentered = false,
                AxisStartsAtBoundary = true
            };
        }

        private static Solid3d BuildShape(Document document, Point3d origin, Vector3d axis, Vector3d distribution, RebarShapePath path, double radius, string label)
        {
            var normal = Normalize(axis.CrossProduct(distribution), label + "/normal");
            Solid3d? result = null;
            try
            {
                for (var i = 1; i < path.Points.Count; i++)
                {
                    var start = World(document, origin, axis, distribution, normal, path.Points[i - 1], label + "/p" + (i - 1));
                    var end = World(document, origin, axis, distribution, normal, path.Points[i], label + "/p" + i);
                    var vx = SubtractFinite(end.X, start.X, label + "/segment dx");
                    var vy = SubtractFinite(end.Y, start.Y, label + "/segment dy");
                    var vz = SubtractFinite(end.Z, start.Z, label + "/segment dz");
                    var length = Hypot3(vx, vy, vz, label + "/segment length");
                    if (length <= 1e-9d) throw new InvalidOperationException("Shape rebar chứa segment rỗng: " + label);
                    var overlap = Math.Min(MultiplyFinite(radius, .75d, label + "/overlap radius"), MultiplyFinite(length, .1d, label + "/overlap length"));
                    var unit = new Vector3d(vx / length, vy / length, vz / length);
                    var before = i == 1 ? 0d : overlap;
                    var after = i == path.Points.Count - 1 ? 0d : overlap;
                    var extendedStart = Point(
                        SubtractFinite(start.X, MultiplyFinite(unit.X, before, label + "/extended start X delta"), label + "/extended start X"),
                        SubtractFinite(start.Y, MultiplyFinite(unit.Y, before, label + "/extended start Y delta"), label + "/extended start Y"),
                        SubtractFinite(start.Z, MultiplyFinite(unit.Z, before, label + "/extended start Z delta"), label + "/extended start Z"),
                        label + "/extended start");
                    var extendedLength = AddFinite(AddFinite(length, before, label + "/extended length"), after, label + "/extended length");
                    var part = Cylinder(document, extendedStart, new Vector3d(vx, vy, vz), extendedLength, radius, label + "/s" + i);
                    if (result == null) { result = part; continue; }
                    try { result.BooleanOperation(BooleanOperationType.BoolUnite, part); }
                    finally { part.Dispose(); }
                }
                if (result == null) throw new InvalidOperationException("Không tạo được shape rebar: " + label);
                var completed = result;
                result = null;
                return completed;
            }
            finally { result?.Dispose(); }
        }

        private static Point3d World(Document document, Point3d origin, Vector3d axis, Vector3d distribution, Vector3d normal, RebarShapePoint point, string label)
        {
            var x = CadGeometryGuard.ToDrawingUnits(document, point.X, label + "/x");
            var y = CadGeometryGuard.ToDrawingUnits(document, point.Y, label + "/y");
            var z = CadGeometryGuard.ToDrawingUnits(document, point.Z, label + "/z");
            var worldX = AddFinite(origin.X, AddFinite(MultiplyFinite(axis.X, x, label + "/axis X"), AddFinite(MultiplyFinite(distribution.X, y, label + "/distribution X"), MultiplyFinite(normal.X, z, label + "/normal X"), label + "/secondary X"), label + "/local X"), label + "/world X");
            var worldY = AddFinite(origin.Y, AddFinite(MultiplyFinite(axis.Y, x, label + "/axis Y"), AddFinite(MultiplyFinite(distribution.Y, y, label + "/distribution Y"), MultiplyFinite(normal.Y, z, label + "/normal Y"), label + "/secondary Y"), label + "/local Y"), label + "/world Y");
            var worldZ = AddFinite(origin.Z, AddFinite(MultiplyFinite(axis.Z, x, label + "/axis Z"), AddFinite(MultiplyFinite(distribution.Z, y, label + "/distribution Z"), MultiplyFinite(normal.Z, z, label + "/normal Z"), label + "/secondary Z"), label + "/local Z"), label + "/world Z");
            return Point(worldX, worldY, worldZ, label + "/world");
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Rebar axis không hợp lệ: " + label);
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
                var completed = solid;
                solid = null!;
                return completed;
            }
            finally { solid?.Dispose(); }
        }

        // ShapeRebarDistributionPlanner preserves the diagnostic "Multiple shape rebars require a positive usable distribution span."
        // while the ModuleInitializer smoke test now locks the actual cover/radius and centered-offset behavior.

        private static Point3d OffsetPoint(Point3d origin, Vector3d axis, double axial, Vector3d distribution, double distributed, double zOffset, string label)
        {
            var x = AddFinite(origin.X, AddFinite(MultiplyFinite(axis.X, axial, label + "/axis X"), MultiplyFinite(distribution.X, distributed, label + "/distribution X"), label + "/XY X"), label + "/X");
            var y = AddFinite(origin.Y, AddFinite(MultiplyFinite(axis.Y, axial, label + "/axis Y"), MultiplyFinite(distribution.Y, distributed, label + "/distribution Y"), label + "/XY Y"), label + "/Y");
            var z = AddFinite(origin.Z, zOffset, label + "/Z");
            return Point(x, y, z, label);
        }

        private static Point3d Point(double x, double y, double z, string label)
            => new Point3d(CadGeometryGuard.Finite(x, label + "/X"), CadGeometryGuard.Finite(y, label + "/Y"), CadGeometryGuard.Finite(z, label + "/Z"));

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

        private static double MultiplyFinite(double first, double second, string label)
            => CadGeometryGuard.Finite(CadGeometryGuard.Finite(first, label + "/first") * CadGeometryGuard.Finite(second, label + "/second"), label);

        private static double AddFinite(double first, double second, string label) => CadGeometryGuard.Add(first, second, label);

        private static double SubtractFinite(double first, double second, string label)
            => CadGeometryGuard.Finite(CadGeometryGuard.Finite(first, label + "/first") - CadGeometryGuard.Finite(second, label + "/second"), label);

        private static bool IsWallLike(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.StructuralWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        private static Vector3d Normalize(Vector3d vector, string label)
        {
            var length = Hypot3(vector.X, vector.Y, vector.Z, label);
            if (length <= 1e-12d) throw new InvalidOperationException("Không xác định được " + label + ".");
            return new Vector3d(vector.X / length, vector.Y / length, vector.Z / length);
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, HandlesKey);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated shape rebar handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var solid = entity as Solid3d;
                if (solid == null) throw new InvalidOperationException("Generated shape rebar handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, HandlesKey, "erase generated shape rebar " + handle);
                solid.Erase();
            }
        }

        private static Entity? OpenSelectedSource(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Entity? selected = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                    throw new InvalidOperationException("Selected source handle is invalid for " + element.Id + ": " + text);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (selected != null) throw new InvalidOperationException("Element " + element.Id + " có nhiều selected live source. Chọn đúng một source CAD để xác định placement shape rebar 3D.");
                selected = entity;
            }
            return selected;
        }

        private static string? Text(ProjectElement element, string key) => element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }
}