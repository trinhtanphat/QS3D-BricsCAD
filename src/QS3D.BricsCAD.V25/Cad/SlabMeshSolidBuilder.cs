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
    internal sealed class SlabMeshBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class SlabMeshSolidBuilder
    {
        private const string HandlesKey = "GeneratedSlabMeshHandles";
        private const string Mode = "SlabMeshXY";
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
        }

        public static SlabMeshBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new SlabMeshBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }
            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds())
                try { selectedHandles.Add(id.Handle.ToString()); } catch { }

            var elements = project.Elements
                .Where(x => x.Category == ElementCategory.Slab && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (elements.Count == 0) return new SlabMeshBuildResult();

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var batchBars = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var element in elements)
                {
                    var polyline = OpenSelectedSlabSource(document, transaction, element, selectedHandles);
                    if (polyline == null) continue;
                    var family = project.FindFamily(element.FamilyId);
                    var xGroup = ParseDirection(element, "RebarSlabXNotation");
                    var yGroup = ParseDirection(element, "RebarSlabYNotation");

                    var frame = ReadRectangle(document, element, polyline);
                    var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", .15d), element.Id + "/ThicknessM");
                    var coverM = CadGeometryGuard.Number(element, family, "RebarSlabCoverM", CadGeometryGuard.Number(element, family, "RebarCoverM", .02d));
                    if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarSlabCoverM phải >= 0.");
                    var faces = Text(element, family, "RebarSlabFaces", "Bottom");
                    var includeBottom = string.Equals(faces, "Bottom", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                    var includeTop = string.Equals(faces, "Top", StringComparison.OrdinalIgnoreCase) || string.Equals(faces, "Both", StringComparison.OrdinalIgnoreCase);
                    if (!includeBottom && !includeTop) throw new InvalidOperationException(element.Id + "/RebarSlabFaces phải là Bottom, Top hoặc Both.");
                    var xClosest = Boolean(element, family, "RebarSlabXClosestToFace", true);

                    var layout = RectangularSlabMeshPlanner.Plan(new RectangularSlabMeshInput
                    {
                        SpanXM = frame.SpanXM,
                        SpanYM = frame.SpanYM,
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
                    if (batchBars > MaxBarsPerBatch - layout.Count) throw new InvalidOperationException("Slab mesh batch vượt giới hạn " + MaxBarsPerBatch + " bar.");
                    batchBars += layout.Count;

                    ErasePrevious(document, transaction, element, ownership);
                    var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                    var centerZ = CadGeometryGuard.Add(
                        polyline.Elevation,
                        CadGeometryGuard.ToDrawingUnits(document, bottomM + thicknessM / 2d, element.Id + "/slab mesh center Z"),
                        element.Id + "/slab mesh world center Z");
                    var update = new PendingUpdate
                    {
                        Element = element,
                        XDiameterMm = xGroup.DiameterMm,
                        YDiameterMm = yGroup.DiameterMm,
                        CoverM = coverM,
                        XSpacingM = layout.XActualSpacingM,
                        YSpacingM = layout.YActualSpacingM,
                        Faces = includeBottom && includeTop ? "Both" : (includeTop ? "Top" : "Bottom")
                    };
                    foreach (var placement in layout.Bars)
                    {
                        var run = placement.Direction == SlabMeshDirection.X ? frame.XAxis : frame.YAxis;
                        var distribution = placement.Direction == SlabMeshDirection.X ? frame.YAxis : frame.XAxis;
                        var distributionOffset = CadGeometryGuard.ToDrawingUnits(document, placement.DistributionOffsetM, element.Id + "/mesh distribution");
                        var elevationOffset = CadGeometryGuard.ToDrawingUnits(document, placement.ElevationOffsetM, element.Id + "/mesh elevation");
                        var length = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.LengthM, element.Id + "/mesh bar length"), element.Id + "/mesh bar length drawing");
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, placement.DiameterMm / 2000d, element.Id + "/mesh bar radius"), element.Id + "/mesh bar radius drawing");
                        var center = new Point3d(
                            CadGeometryGuard.Add(frame.Center.X, CadGeometryGuard.Finite(distribution.X * distributionOffset, element.Id + "/mesh distribution X"), element.Id + "/mesh center X"),
                            CadGeometryGuard.Add(frame.Center.Y, CadGeometryGuard.Finite(distribution.Y * distributionOffset, element.Id + "/mesh distribution Y"), element.Id + "/mesh center Y"),
                            CadGeometryGuard.Add(centerZ, elevationOffset, element.Id + "/mesh center Z"));
                        var half = length / 2d;
                        var start = new Point3d(
                            CadGeometryGuard.Add(center.X, CadGeometryGuard.Finite(-run.X * half, element.Id + "/mesh start X offset"), element.Id + "/mesh start X"),
                            CadGeometryGuard.Add(center.Y, CadGeometryGuard.Finite(-run.Y * half, element.Id + "/mesh start Y offset"), element.Id + "/mesh start Y"),
                            center.Z);
                        Solid3d? bar = CreateCylinder(document, start, run, length, radius, element.Id + "/slab mesh bar");
                        try
                        {
                            bar.Layer = polyline.Layer;
                            modelSpace.AppendEntity(bar);
                            transaction.AddNewlyCreatedDBObject(bar, true);
                            update.Handles.Add(bar.Handle.ToString());
                            bar = null;
                        }
                        finally { bar?.Dispose(); }
                    }
                    pending.Add(update);
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
                update.Element.Properties["GeneratedSlabMeshCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshXDiameterMm"] = update.XDiameterMm.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshYDiameterMm"] = update.YDiameterMm.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshMode"] = Mode;
                update.Element.Properties["GeneratedSlabMeshXActualSpacingM"] = update.XSpacingM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshYActualSpacingM"] = update.YSpacingM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedSlabMeshFaces"] = update.Faces;
                AuditTrail.ForProject(project).Record("geometry.rebar.slab.mesh", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars");
            }
            if (pending.Count > 0)
            {
                project.Touch();
                document.Editor.Regen();
            }
            return new SlabMeshBuildResult { Elements = pending.Count, Bars = pending.Sum(x => x.Handles.Count) };
        }

        private sealed class RectangleFrame
        {
            public Point3d Center { get; set; }
            public Vector3d XAxis { get; set; }
            public Vector3d YAxis { get; set; }
            public double SpanXM { get; set; }
            public double SpanYM { get; set; }
        }

        private static RectangleFrame ReadRectangle(Document document, ProjectElement element, Polyline polyline)
        {
            if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException(element.Id + ": QS3DSLABREBAR3D yêu cầu closed 4-vertex rectangular POLYLINE.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException(element.Id + ": Slab mesh footprint phải nằm trên mặt phẳng XY.");
            for (var i = 0; i < 4; i++) if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12d) throw new InvalidOperationException(element.Id + ": Slab mesh rectangle không hỗ trợ bulge.");
            var p0 = polyline.GetPoint2dAt(0); var p1 = polyline.GetPoint2dAt(1); var p2 = polyline.GetPoint2dAt(2); var p3 = polyline.GetPoint2dAt(3);
            var xdx = CadGeometryGuard.Finite(p1.X - p0.X, element.Id + "/slab X dx"); var xdy = CadGeometryGuard.Finite(p1.Y - p0.Y, element.Id + "/slab X dy");
            var ydx = CadGeometryGuard.Finite(p2.X - p1.X, element.Id + "/slab Y dx"); var ydy = CadGeometryGuard.Finite(p2.Y - p1.Y, element.Id + "/slab Y dy");
            var spanXDrawing = CadGeometryGuard.Hypot(xdx, xdy, element.Id + "/slab X span");
            var spanYDrawing = CadGeometryGuard.Hypot(ydx, ydy, element.Id + "/slab Y span");
            if (spanXDrawing <= 1e-9d || spanYDrawing <= 1e-9d) throw new InvalidOperationException(element.Id + ": Slab rectangle bị suy biến.");
            var ux = xdx / spanXDrawing; var uy = xdy / spanXDrawing; var vx = ydx / spanYDrawing; var vy = ydy / spanYDrawing;
            if (Math.Abs(ux * vx + uy * vy) > 1e-6d) throw new InvalidOperationException(element.Id + ": Slab footprint không vuông góc.");
            var tolerance = Math.Max(spanXDrawing, spanYDrawing) * 1e-6d + 1e-8d;
            if (Distance(p2.X, p2.Y, p0.X + xdx + ydx, p0.Y + xdy + ydy) > tolerance || Distance(p3.X, p3.Y, p0.X + ydx, p0.Y + ydy) > tolerance)
                throw new InvalidOperationException(element.Id + ": Slab footprint phải là rectangle kín theo thứ tự vertex.");
            return new RectangleFrame
            {
                Center = new Point3d((p0.X + p1.X + p2.X + p3.X) / 4d, (p0.Y + p1.Y + p2.Y + p3.Y) / 4d, polyline.Elevation),
                XAxis = new Vector3d(ux, uy, 0d),
                YAxis = new Vector3d(vx, vy, 0d),
                SpanXM = CadGeometryGuard.ToMeters(document, spanXDrawing, element.Id + "/slab X span"),
                SpanYM = CadGeometryGuard.ToMeters(document, spanYDrawing, element.Id + "/slab Y span")
            };
        }

        private static RebarGroup ParseDirection(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var notation) || string.IsNullOrWhiteSpace(notation)) throw new InvalidOperationException(element.Id + " chưa có " + key + " (ví dụ D10@200 hoặc 20D10).");
            var groups = RebarNotationParser.Parse(notation);
            if (groups.Count != 1) throw new InvalidOperationException(element.Id + "/" + key + " chỉ hỗ trợ một group.");
            var group = groups[0];
            if (!group.Quantity.HasValue && !group.SpacingMm.HasValue) throw new InvalidOperationException(element.Id + "/" + key + " phải có count hoặc spacing.");
            return group;
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, HandlesKey);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated slab mesh handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated slab mesh handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                solid.Erase();
            }
        }

        private static Solid3d CreateCylinder(Document document, Point3d start, Vector3d direction, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = Hypot3(direction.X, direction.Y, direction.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Slab mesh bar axis không hợp lệ: " + label);
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

        private static Polyline? OpenSelectedSlabSource(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Polyline? selected = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException("Selected slab source handle không hợp lệ cho " + element.Id + ": " + text);
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var entity = transaction.GetObject(id, OpenMode.ForRead, true) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Polyline polyline)) throw new InvalidOperationException(element.Id + " cần source POLYLINE chữ nhật để dựng slab mesh 3D.");
                if (selected != null) throw new InvalidOperationException(element.Id + " có nhiều selected live source. Chọn đúng một Slab POLYLINE.");
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

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1; var dy = y2 - y1;
            return CadGeometryGuard.Hypot(dx, dy, "slab rectangle closure");
        }

        private static double Hypot3(double x, double y, double z, string label)
        {
            x = Math.Abs(CadGeometryGuard.Finite(x, label + "/x")); y = Math.Abs(CadGeometryGuard.Finite(y, label + "/y")); z = Math.Abs(CadGeometryGuard.Finite(z, label + "/z"));
            var maximum = Math.Max(x, Math.Max(y, z));
            if (maximum <= 0d) return 0d;
            var sx = x / maximum; var sy = y / maximum; var sz = z / maximum;
            return CadGeometryGuard.Finite(maximum * Math.Sqrt(sx * sx + sy * sy + sz * sz), label);
        }
    }
}
