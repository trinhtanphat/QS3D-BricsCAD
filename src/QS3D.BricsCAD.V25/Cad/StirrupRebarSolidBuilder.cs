using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class StirrupRebarBuildResult
    {
        public int Elements { get; set; }
        public int Ties { get; set; }
    }

    internal static class StirrupRebarSolidBuilder
    {
        private const string HandlesKey = "GeneratedStirrupRebarHandles";
        private const int MaxTiesPerElement = 800;
        private const int MaxTiesPerBatch = 2500;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double DiameterMm { get; set; }
            public double CoverM { get; set; }
            public double EndCoverM { get; set; }
            public double CenterlineLengthM { get; set; }
            public double TotalCenterlineLengthM { get; set; }
            public string Notation { get; set; } = string.Empty;
        }

        private sealed class HostPlacement
        {
            public double WidthM { get; set; }
            public double DepthM { get; set; }
            public double HostSpanM { get; set; }
            public Point3d Origin { get; set; }
            public Vector3d LocalX { get; set; }
            public Vector3d LocalY { get; set; }
            public Vector3d Distribution { get; set; }
        }

        public static StirrupRebarBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new StirrupRebarBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var pending = new List<PendingUpdate>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var totalTies = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased) continue;
                    var sourceHandle = source.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => (x.Category == ElementCategory.Beam || x.Category == ElementCategory.Column) &&
                                    x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("Stirrup source " + sourceHandle + " đang thuộc nhiều QS3D Beam/Column element.");
                    var element = matches[0];
                    if (!processed.Add(element.Id)) throw new InvalidOperationException("Element " + element.Id + " có nhiều source được chọn. Tách/capture từng source trước khi tạo stirrup 3D.");

                    var family = project.FindFamily(element.FamilyId);
                    var notation = RequiredText(element, family, "StirrupNotation");
                    var groups = RebarNotationParser.Parse(notation);
                    if (groups.Count != 1) throw new InvalidOperationException(element.Id + "/StirrupNotation phải chứa đúng một nhóm đường kính/phân bố.");
                    var group = groups[0];
                    if (group.SpacingMm.HasValue == group.Quantity.HasValue)
                        throw new InvalidOperationException(element.Id + "/StirrupNotation phải khai báo đúng một kiểu phân bố: D8@150 hoặc 12D8.");

                    var coverM = RequiredNonNegative(element, family, "RebarCoverM");
                    var endCoverM = RequiredNonNegative(element, family, "StirrupEndCoverM");
                    var bendRadiusM = RequiredNonNegative(element, family, "StirrupBendRadiusM");
                    var hookLengthM = RequiredNonNegative(element, family, "StirrupHookLengthM");
                    var hookAngleDeg = RequiredFinite(element, family, "StirrupHookTailAngleDeg");
                    var maximumSagittaM = OptionalPositive(element, family, "StirrupMaximumSagittaM", 0.001d);
                    var placement = ResolvePlacement(document, element, family, source);
                    var plan = RectangularStirrupPlanner.PlanSet(new RectangularStirrupSetInput
                    {
                        Shape = new RectangularStirrupInput
                        {
                            WidthM = placement.WidthM,
                            DepthM = placement.DepthM,
                            CoverM = coverM,
                            DiameterMm = group.DiameterMm,
                            BendRadiusM = bendRadiusM,
                            MaximumSagittaM = maximumSagittaM,
                            HookLengthM = hookLengthM,
                            HookTailAngleDeg = hookAngleDeg
                        },
                        HostSpanM = placement.HostSpanM,
                        EndCoverM = endCoverM,
                        SpacingMm = group.SpacingMm,
                        Count = group.Quantity
                    });
                    if (plan.Distribution.Count > MaxTiesPerElement)
                        throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxTiesPerElement + " stirrup/element.");
                    if (checked(totalTies + plan.Distribution.Count) > MaxTiesPerBatch)
                        throw new InvalidOperationException("Stirrup Rebar 3D vượt giới hạn " + MaxTiesPerBatch + " stirrup/batch.");

                    var radius = CadGeometryGuard.Positive(
                        CadGeometryGuard.ToDrawingUnits(document, group.DiameterMm / 2000d, element.Id + "/stirrup radius"),
                        element.Id + "/stirrup radius drawing units");
                    ErasePrevious(document, transaction, element, ownership);
                    var update = new PendingUpdate
                    {
                        Element = element,
                        DiameterMm = group.DiameterMm,
                        CoverM = coverM,
                        EndCoverM = endCoverM,
                        CenterlineLengthM = plan.Shape.CenterlineLengthM,
                        TotalCenterlineLengthM = plan.TotalCenterlineLengthM,
                        Notation = notation
                    };
                    foreach (var offsetM in plan.Distribution.OffsetsM)
                    {
                        var offset = CadGeometryGuard.ToDrawingUnits(document, offsetM, element.Id + "/stirrup distribution offset");
                        var origin = Add(placement.Origin, placement.Distribution, offset, element.Id + "/stirrup origin");
                        var tie = BuildTie(document, origin, placement.LocalX, placement.LocalY, plan.Shape.Path, radius, element.Id);
                        try
                        {
                            tie.Layer = source.Layer;
                            modelSpace.AppendEntity(tie);
                            transaction.AddNewlyCreatedDBObject(tie, true);
                            update.Handles.Add(tie.Handle.ToString());
                            tie = null!;
                        }
                        finally { tie?.Dispose(); }
                    }
                    pending.Add(update);
                    totalTies += update.Handles.Count;
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
                update.Element.Properties["GeneratedStirrupRebarCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarDiameterMm"] = update.DiameterMm.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarEndCoverM"] = update.EndCoverM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarCenterlineLengthM"] = update.CenterlineLengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarTotalCenterlineLengthM"] = update.TotalCenterlineLengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedStirrupRebarMode"] = "RectangularStirrup.SegmentedCylinder";
                update.Element.Properties["GeneratedStirrupRebarNotation"] = update.Notation;
                AuditTrail.ForProject(project).Record("geometry.rebar.stirrup", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " ties");
            }
            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return new StirrupRebarBuildResult { Elements = pending.Count, Ties = totalTies };
        }

        private static HostPlacement ResolvePlacement(Document document, ProjectElement element, ProjectFamily? family, Entity source)
        {
            if (element.Category == ElementCategory.Beam)
            {
                var line = source as Line ?? throw new InvalidOperationException(element.Id + " Beam stirrup 3D yêu cầu source LINE.");
                var dz = CadGeometryGuard.Finite(line.EndPoint.Z - line.StartPoint.Z, element.Id + "/beam dZ");
                var planarityTolerance = CadGeometryGuard.ToDrawingUnits(document, 0.000001d, element.Id + "/beam planarity tolerance");
                if (Math.Abs(dz) > planarityTolerance) throw new InvalidOperationException(element.Id + " Beam stirrup hiện yêu cầu LINE nằm ngang để section local không bị suy đoán.");
                var dx = CadGeometryGuard.Finite(line.EndPoint.X - line.StartPoint.X, element.Id + "/beam dX");
                var dy = CadGeometryGuard.Finite(line.EndPoint.Y - line.StartPoint.Y, element.Id + "/beam dY");
                var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/beam length");
                if (lengthDrawing <= 1e-8d) throw new InvalidOperationException(element.Id + " Beam source LINE quá ngắn.");
                var axis = new Vector3d(dx / lengthDrawing, dy / lengthDrawing, 0d);
                var localX = new Vector3d(-axis.Y, axis.X, 0d);
                var widthM = RequiredPositive(element, family, "WidthM");
                var depthM = RequiredPositive(element, family, "HeightM");
                var bottomOffsetM = OptionalFinite(element, family, "BottomOffsetM", 0d);
                var halfDepth = CadGeometryGuard.ToDrawingUnits(document, depthM / 2d, element.Id + "/beam half depth");
                var bottom = CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM");
                var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/beam mid X");
                var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/beam mid Y");
                var midZ = CadGeometryGuard.Add(line.StartPoint.Z, bottom, element.Id + "/beam bottom Z");
                midZ = CadGeometryGuard.Add(midZ, halfDepth, element.Id + "/beam center Z");
                return new HostPlacement
                {
                    WidthM = widthM,
                    DepthM = depthM,
                    HostSpanM = CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/beam span"),
                    Origin = new Point3d(midX, midY, midZ),
                    LocalX = localX,
                    LocalY = Vector3d.ZAxis,
                    Distribution = axis
                };
            }

            if (element.Category != ElementCategory.Column) throw new InvalidOperationException("Stirrup 3D chỉ hỗ trợ Beam/Column semantic.");
            var polyline = source as Polyline ?? throw new InvalidOperationException(element.Id + " Column stirrup 3D yêu cầu closed rectangle POLYLINE.");
            if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException(element.Id + " Column stirrup 3D yêu cầu closed 4-vertex POLYLINE chữ nhật.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException(element.Id + " Column stirrup footprint phải nằm trên mặt phẳng XY.");
            for (var vertex = 0; vertex < 4; vertex++)
                if (Math.Abs(polyline.GetBulgeAt(vertex)) > 1e-12d) throw new InvalidOperationException(element.Id + " Column stirrup rectangle không hỗ trợ bulge.");

            var p0 = polyline.GetPoint2dAt(0);
            var p1 = polyline.GetPoint2dAt(1);
            var p2 = polyline.GetPoint2dAt(2);
            var p3 = polyline.GetPoint2dAt(3);
            var e1x = CadGeometryGuard.Finite(p1.X - p0.X, element.Id + "/column edge1 X");
            var e1y = CadGeometryGuard.Finite(p1.Y - p0.Y, element.Id + "/column edge1 Y");
            var e2x = CadGeometryGuard.Finite(p2.X - p1.X, element.Id + "/column edge2 X");
            var e2y = CadGeometryGuard.Finite(p2.Y - p1.Y, element.Id + "/column edge2 Y");
            var widthDrawing = CadGeometryGuard.Hypot(e1x, e1y, element.Id + "/column width");
            var depthDrawing = CadGeometryGuard.Hypot(e2x, e2y, element.Id + "/column depth");
            if (widthDrawing <= 1e-8d || depthDrawing <= 1e-8d) throw new InvalidOperationException(element.Id + " Column footprint bị suy biến.");
            var ux = e1x / widthDrawing; var uy = e1y / widthDrawing;
            var vx = e2x / depthDrawing; var vy = e2y / depthDrawing;
            if (Math.Abs(ux * vx + uy * vy) > 1e-6d) throw new InvalidOperationException(element.Id + " Column footprint không vuông góc.");
            var tolerance = Math.Max(widthDrawing, depthDrawing) * 1e-6d + 1e-8d;
            if (Distance(p2.X, p2.Y, p0.X + e1x + e2x, p0.Y + e1y + e2y) > tolerance ||
                Distance(p3.X, p3.Y, p0.X + e2x, p0.Y + e2y) > tolerance)
                throw new InvalidOperationException(element.Id + " Column footprint phải là rectangle/parallelogram vuông kín theo thứ tự vertex.");

            var heightM = RequiredPositive(element, family, "HeightM");
            var bottomOffset = CadGeometryGuard.ToDrawingUnits(document, OptionalFinite(element, family, "BottomOffsetM", 0d), element.Id + "/BottomOffsetM");
            var halfHeight = CadGeometryGuard.ToDrawingUnits(document, heightM / 2d, element.Id + "/column half height");
            var centerX = (p0.X + p1.X + p2.X + p3.X) / 4d;
            var centerY = (p0.Y + p1.Y + p2.Y + p3.Y) / 4d;
            var centerZ = CadGeometryGuard.Add(polyline.Elevation, bottomOffset, element.Id + "/column bottom Z");
            centerZ = CadGeometryGuard.Add(centerZ, halfHeight, element.Id + "/column center Z");
            return new HostPlacement
            {
                WidthM = CadGeometryGuard.ToMeters(document, widthDrawing, element.Id + "/column width"),
                DepthM = CadGeometryGuard.ToMeters(document, depthDrawing, element.Id + "/column depth"),
                HostSpanM = heightM,
                Origin = new Point3d(centerX, centerY, centerZ),
                LocalX = new Vector3d(ux, uy, 0d),
                LocalY = new Vector3d(vx, vy, 0d),
                Distribution = Vector3d.ZAxis
            };
        }

        private static Solid3d BuildTie(Document document, Point3d origin, Vector3d localX, Vector3d localY, RebarShapePath path, double radius, string label)
        {
            Solid3d? result = null;
            try
            {
                for (var index = 1; index < path.Points.Count; index++)
                {
                    var start = World(document, origin, localX, localY, path.Points[index - 1], label + "/p" + (index - 1));
                    var end = World(document, origin, localX, localY, path.Points[index], label + "/p" + index);
                    var vector = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
                    var length = vector.Length;
                    if (double.IsNaN(length) || double.IsInfinity(length) || length <= 1e-9d) throw new InvalidOperationException("Stirrup path chứa segment rỗng: " + label);
                    var overlap = Math.Min(radius * 0.75d, length * 0.1d);
                    var unit = new Vector3d(vector.X / length, vector.Y / length, vector.Z / length);
                    var before = index == 1 ? 0d : overlap;
                    var after = index == path.Points.Count - 1 ? 0d : overlap;
                    var extendedStart = new Point3d(start.X - unit.X * before, start.Y - unit.Y * before, start.Z - unit.Z * before);
                    var part = Cylinder(document, extendedStart, vector, length + before + after, radius, label + "/s" + index);
                    if (result == null) { result = part; continue; }
                    try { result.BooleanOperation(BooleanOperationType.BoolUnite, part); }
                    finally { part.Dispose(); }
                }
                if (result == null) throw new InvalidOperationException("Không tạo được stirrup solid: " + label);
                var completed = result; result = null; return completed;
            }
            finally { result?.Dispose(); }
        }

        private static Point3d World(Document document, Point3d origin, Vector3d localX, Vector3d localY, RebarShapePoint point, string label)
        {
            var x = CadGeometryGuard.ToDrawingUnits(document, point.X, label + "/x");
            var y = CadGeometryGuard.ToDrawingUnits(document, point.Y, label + "/y");
            return new Point3d(
                CadGeometryGuard.Add(origin.X, localX.X * x + localY.X * y, label + "/world X"),
                CadGeometryGuard.Add(origin.Y, localX.Y * x + localY.Y * y, label + "/world Y"),
                CadGeometryGuard.Add(origin.Z, localX.Z * x + localY.Z * y, label + "/world Z"));
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            var magnitude = direction.Length;
            if (magnitude <= 1e-12d || double.IsNaN(magnitude) || double.IsInfinity(magnitude)) throw new InvalidOperationException("Stirrup axis không hợp lệ: " + label);
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
                var completed = solid; solid = null!; return completed;
            }
            finally { solid?.Dispose(); }
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, HandlesKey);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated stirrup handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var solid = entity as Solid3d;
                if (solid == null) throw new InvalidOperationException("Generated stirrup handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                solid.Erase();
            }
        }

        private static Point3d Add(Point3d origin, Vector3d direction, double distance, string label)
        {
            return new Point3d(
                CadGeometryGuard.Add(origin.X, direction.X * distance, label + "/X"),
                CadGeometryGuard.Add(origin.Y, direction.Y * distance, label + "/Y"),
                CadGeometryGuard.Add(origin.Z, direction.Z * distance, label + "/Z"));
        }

        private static string RequiredText(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text)) return text.Trim();
            if (family != null && family.Properties.TryGetValue(key, out text) && !string.IsNullOrWhiteSpace(text)) return text.Trim();
            throw new InvalidOperationException(element.Id + "/" + key + " bắt buộc cho stirrup 3D; QS3D không tự đoán tham số kỹ thuật.");
        }

        private static double RequiredPositive(ProjectElement element, ProjectFamily? family, string key) =>
            CadGeometryGuard.Positive(RequiredFinite(element, family, key), element.Id + "/" + key);

        private static double RequiredNonNegative(ProjectElement element, ProjectFamily? family, string key)
        {
            var value = RequiredFinite(element, family, key);
            if (value < 0d) throw new InvalidOperationException(element.Id + "/" + key + " phải >= 0.");
            return value;
        }

        private static double RequiredFinite(ProjectElement element, ProjectFamily? family, string key)
        {
            string? text = null;
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance)) text = instance;
            else if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) text = inherited;
            if (text == null || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new InvalidOperationException(element.Id + "/" + key + " bắt buộc và phải là số hữu hạn; QS3D không tự đoán tham số kỹ thuật.");
            return CadGeometryGuard.Finite(value, element.Id + "/" + key);
        }

        private static double OptionalFinite(ProjectElement element, ProjectFamily? family, string key, double fallback)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance))
            {
                if (!double.TryParse(instance, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException(element.Id + "/" + key + " không hợp lệ.");
                return CadGeometryGuard.Finite(value, element.Id + "/" + key);
            }
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited))
            {
                if (!double.TryParse(inherited, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException("family " + family.Id + "/" + key + " không hợp lệ.");
                return CadGeometryGuard.Finite(value, "family " + family.Id + "/" + key);
            }
            return CadGeometryGuard.Finite(fallback, "fallback " + key);
        }

        private static double OptionalPositive(ProjectElement element, ProjectFamily? family, string key, double fallback) =>
            CadGeometryGuard.Positive(OptionalFinite(element, family, key, fallback), element.Id + "/" + key);

        private static double Distance(double x1, double y1, double x2, double y2) =>
            Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }
}
