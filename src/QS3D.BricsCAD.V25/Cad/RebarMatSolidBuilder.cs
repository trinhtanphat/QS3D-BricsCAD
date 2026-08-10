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
    internal sealed class RebarMatBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class RebarMatSolidBuilder
    {
        internal const string HandlesKey = "GeneratedRebarMatHandles";
        private const int MaxBarsPerElement = 1200;
        private const int MaxBarsPerBatch = 4000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public string XNotation { get; set; } = string.Empty;
            public string YNotation { get; set; } = string.Empty;
            public string Faces { get; set; } = string.Empty;
            public double XActualSpacingM { get; set; }
            public double YActualSpacingM { get; set; }
        }

        public static RebarMatBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new RebarMatBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return new RebarMatBuildResult();

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchBars = 0;

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in ids)
                {
                    var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                    if (polyline == null || polyline.IsErased) continue;
                    var sourceHandle = polyline.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => (x.Category == ElementCategory.Slab || x.Category == ElementCategory.Foundation) && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("Slab/Foundation source " + sourceHandle + " đang thuộc nhiều QS3D element.");
                    var element = matches[0];
                    if (!processed.Add(element.Id)) throw new InvalidOperationException(element.Id + " có nhiều source đang được chọn; Rebar Mat yêu cầu một rectangle source/element.");
                    ValidateRectanglePolyline(polyline, element.Id);

                    var family = project.FindFamily(element.FamilyId);
                    var xNotation = ResolveNotation(element, family, "RebarMatXNotation");
                    var yNotation = ResolveNotation(element, family, "RebarMatYNotation");
                    var xGroup = ParseSpacingGroup(element, xNotation, "RebarMatXNotation");
                    var yGroup = ParseSpacingGroup(element, yNotation, "RebarMatYNotation");
                    var faces = ResolveText(element, family, "RebarMatFaces") ?? "Bottom";
                    bool bottomEnabled;
                    bool topEnabled;
                    ParseFaces(faces, out bottomEnabled, out topEnabled);
                    var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", .025d);
                    if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM phải >= 0.");
                    var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", element.Category == ElementCategory.Foundation ? .5d : .12d), element.Id + "/ThicknessM");

                    var p0 = polyline.GetPoint2dAt(0);
                    var p1 = polyline.GetPoint2dAt(1);
                    var p2 = polyline.GetPoint2dAt(2);
                    var p3 = polyline.GetPoint2dAt(3);
                    var e1x = CadGeometryGuard.Finite(p1.X - p0.X, element.Id + "/mat edge1 X");
                    var e1y = CadGeometryGuard.Finite(p1.Y - p0.Y, element.Id + "/mat edge1 Y");
                    var e2x = CadGeometryGuard.Finite(p2.X - p1.X, element.Id + "/mat edge2 X");
                    var e2y = CadGeometryGuard.Finite(p2.Y - p1.Y, element.Id + "/mat edge2 Y");
                    var widthDrawing = CadGeometryGuard.Hypot(e1x, e1y, element.Id + "/mat width");
                    var depthDrawing = CadGeometryGuard.Hypot(e2x, e2y, element.Id + "/mat depth");
                    if (widthDrawing <= 1e-8d || depthDrawing <= 1e-8d) throw new InvalidOperationException("Rebar Mat rectangle bị suy biến: " + element.Id);
                    var ux = e1x / widthDrawing;
                    var uy = e1y / widthDrawing;
                    var vx = e2x / depthDrawing;
                    var vy = e2y / depthDrawing;
                    if (Math.Abs(ux * vx + uy * vy) > 1e-6d) throw new InvalidOperationException("Rebar Mat source phải là rectangle vuông góc: " + element.Id);
                    var expectedP2X = CadGeometryGuard.Add(p0.X, CadGeometryGuard.Finite(e1x + e2x, element.Id + "/mat expected p2 dx"), element.Id + "/mat expected p2 X");
                    var expectedP2Y = CadGeometryGuard.Add(p0.Y, CadGeometryGuard.Finite(e1y + e2y, element.Id + "/mat expected p2 dy"), element.Id + "/mat expected p2 Y");
                    var expectedP3X = CadGeometryGuard.Add(p0.X, e2x, element.Id + "/mat expected p3 X");
                    var expectedP3Y = CadGeometryGuard.Add(p0.Y, e2y, element.Id + "/mat expected p3 Y");
                    var tolerance = Math.Max(widthDrawing, depthDrawing) * 1e-6d + 1e-8d;
                    if (Distance(p2.X, p2.Y, expectedP2X, expectedP2Y) > tolerance || Distance(p3.X, p3.Y, expectedP3X, expectedP3Y) > tolerance)
                        throw new InvalidOperationException("Rebar Mat source phải là rectangle 4 đỉnh theo thứ tự liên tục: " + element.Id);

                    var layout = OrthogonalRebarMatPlanner.Plan(new OrthogonalRebarMatInput
                    {
                        WidthM = CadGeometryGuard.ToMeters(document, widthDrawing, element.Id + "/mat width"),
                        DepthM = CadGeometryGuard.ToMeters(document, depthDrawing, element.Id + "/mat depth"),
                        ThicknessM = thicknessM,
                        CoverM = coverM,
                        XDiameterMm = xGroup.DiameterMm,
                        YDiameterMm = yGroup.DiameterMm,
                        XSpacingMm = xGroup.SpacingMm!.Value,
                        YSpacingMm = yGroup.SpacingMm!.Value,
                        BottomEnabled = bottomEnabled,
                        TopEnabled = topEnabled
                    });
                    if (layout.Count > MaxBarsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " Rebar Mat bar/element.");
                    if (batchBars > MaxBarsPerBatch - layout.Count) throw new InvalidOperationException("Rebar Mat batch vượt giới hạn " + MaxBarsPerBatch + " bar.");

                    ErasePrevious(document, transaction, element, ownership);
                    var centerX = CadGeometryGuard.Midpoint(CadGeometryGuard.Midpoint(p0.X, p2.X, element.Id + "/mat diagonal X 1"), CadGeometryGuard.Midpoint(p1.X, p3.X, element.Id + "/mat diagonal X 2"), element.Id + "/mat center X");
                    var centerY = CadGeometryGuard.Midpoint(CadGeometryGuard.Midpoint(p0.Y, p2.Y, element.Id + "/mat diagonal Y 1"), CadGeometryGuard.Midpoint(p1.Y, p3.Y, element.Id + "/mat diagonal Y 2"), element.Id + "/mat center Y");
                    var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                    var baseZ = CadGeometryGuard.Add(polyline.Elevation, CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM"), element.Id + "/mat base Z");
                    var update = new PendingUpdate
                    {
                        Element = element,
                        XNotation = xNotation,
                        YNotation = yNotation,
                        Faces = CanonicalFaces(bottomEnabled, topEnabled),
                        XActualSpacingM = layout.XActualSpacingM,
                        YActualSpacingM = layout.YActualSpacingM
                    };
                    foreach (var planned in layout.Bars)
                    {
                        var start = World(document, centerX, centerY, baseZ, ux, uy, vx, vy, planned.Start, planned.ElevationFromBottomM, element.Id + "/mat start");
                        var end = World(document, centerX, centerY, baseZ, ux, uy, vx, vy, planned.End, planned.ElevationFromBottomM, element.Id + "/mat end");
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, planned.DiameterMm / 2000d, element.Id + "/mat radius"), element.Id + "/mat radius drawing");
                        var bar = Cylinder(document, start, end, radius, element.Id + "/mat bar");
                        try
                        {
                            bar.Layer = polyline.Layer;
                            modelSpace.AppendEntity(bar);
                            transaction.AddNewlyCreatedDBObject(bar, true);
                            update.Handles.Add(bar.Handle.ToString());
                            bar = null!;
                        }
                        finally { bar?.Dispose(); }
                    }
                    pending.Add(update);
                    batchBars = checked(batchBars + update.Handles.Count);
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
                update.Element.Properties["GeneratedRebarMatCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedRebarMatXNotation"] = update.XNotation;
                update.Element.Properties["GeneratedRebarMatYNotation"] = update.YNotation;
                update.Element.Properties["GeneratedRebarMatFaces"] = update.Faces;
                update.Element.Properties["GeneratedRebarMatXActualSpacingM"] = update.XActualSpacingM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedRebarMatYActualSpacingM"] = update.YActualSpacingM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedRebarMatMode"] = "Rectangular.OrthogonalMat";
                AuditTrail.ForProject(project).Record("geometry.rebar.mat", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars / " + update.Faces);
            }
            if (pending.Count > 0)
            {
                project.Touch();
                document.Editor.Regen();
            }
            return new RebarMatBuildResult { Elements = pending.Count, Bars = pending.Sum(x => x.Handles.Count) };
        }

        private static void ValidateRectanglePolyline(Polyline polyline, string elementId)
        {
            if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException("QS3DREBARMAT3D hiện chỉ hỗ trợ closed 4-vertex rectangle POLYLINE cho " + elementId + ".");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException("QS3DREBARMAT3D yêu cầu footprint nằm trên mặt phẳng XY: " + elementId);
            for (var vertex = 0; vertex < 4; vertex++)
                if (Math.Abs(polyline.GetBulgeAt(vertex)) > 1e-12d) throw new InvalidOperationException("QS3DREBARMAT3D chưa hỗ trợ rectangle có bulge: " + elementId);
        }

        private static RebarGroup ParseSpacingGroup(ProjectElement element, string notation, string propertyName)
        {
            var groups = RebarNotationParser.Parse(notation);
            if (groups.Count != 1 || !groups[0].SpacingMm.HasValue || groups[0].Quantity.HasValue)
                throw new InvalidOperationException(element.Id + "/" + propertyName + " phải là một notation dạng Dxx@spacing, ví dụ D12@200.");
            return groups[0];
        }

        private static string ResolveNotation(ProjectElement element, ProjectFamily? family, string directionKey)
        {
            var text = ResolveText(element, family, directionKey)
                ?? ResolveText(element, family, "RebarMatNotation")
                ?? ResolveText(element, family, "RebarNotation");
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException(element.Id + " thiếu " + directionKey + "/RebarMatNotation/RebarNotation.");
            return text.Trim();
        }

        private static string? ResolveText(ProjectElement element, ProjectFamily? family, string key)
        {
            if (element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            if (family != null && family.Properties.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            return null;
        }

        private static void ParseFaces(string raw, out bool bottom, out bool top)
        {
            var normalized = (raw ?? string.Empty).Trim();
            if (string.Equals(normalized, "Bottom", StringComparison.OrdinalIgnoreCase)) { bottom = true; top = false; return; }
            if (string.Equals(normalized, "Top", StringComparison.OrdinalIgnoreCase)) { bottom = false; top = true; return; }
            if (string.Equals(normalized, "Both", StringComparison.OrdinalIgnoreCase)) { bottom = true; top = true; return; }
            throw new InvalidOperationException("RebarMatFaces chỉ nhận Bottom, Top hoặc Both.");
        }

        private static string CanonicalFaces(bool bottom, bool top) => bottom && top ? "Both" : top ? "Top" : "Bottom";

        private static Point3d World(Document document, double centerX, double centerY, double baseZ, double ux, double uy, double vx, double vy, QS3D.Core.Geometry.Point2 local, double elevationM, string label)
        {
            var localX = CadGeometryGuard.ToDrawingUnits(document, local.X, label + "/local X");
            var localY = CadGeometryGuard.ToDrawingUnits(document, local.Y, label + "/local Y");
            var dx = CadGeometryGuard.Finite(ux * localX + vx * localY, label + "/dx");
            var dy = CadGeometryGuard.Finite(uy * localX + vy * localY, label + "/dy");
            var dz = CadGeometryGuard.ToDrawingUnits(document, elevationM, label + "/elevation");
            return new Point3d(CadGeometryGuard.Add(centerX, dx, label + "/X"), CadGeometryGuard.Add(centerY, dy, label + "/Y"), CadGeometryGuard.Add(baseZ, dz, label + "/Z"));
        }

        private static Solid3d Cylinder(Document document, Point3d start, Point3d end, double radius, string label)
        {
            var dx = CadGeometryGuard.Finite(end.X - start.X, label + "/dx");
            var dy = CadGeometryGuard.Finite(end.Y - start.Y, label + "/dy");
            var dz = CadGeometryGuard.Finite(end.Z - start.Z, label + "/dz");
            var maximum = Math.Max(Math.Abs(dx), Math.Max(Math.Abs(dy), Math.Abs(dz)));
            if (maximum <= 1e-12d) throw new InvalidOperationException("Rebar Mat bar segment bị rỗng: " + label);
            var sx = dx / maximum;
            var sy = dy / maximum;
            var sz = dz / maximum;
            var length = CadGeometryGuard.Positive(CadGeometryGuard.Finite(maximum * Math.Sqrt(sx * sx + sy * sy + sz * sz), label + "/length"), label + "/length");
            var unit = new Vector3d(dx / length, dy / length, dz / length);
            Solid3d? solid = new Solid3d();
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
                solid = null;
                return complete;
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
                if (ids.Count > 1) throw new InvalidOperationException("Generated Rebar Mat handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated Rebar Mat handle " + handle + " is not a Solid3d. Refusing destructive erase.");
                solid.Erase();
            }
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var maximum = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (maximum <= 0d) return 0d;
            var sx = dx / maximum;
            var sy = dy / maximum;
            return CadGeometryGuard.Finite(maximum * Math.Sqrt(sx * sx + sy * sy), "Rebar Mat rectangle distance");
        }
    }
}
