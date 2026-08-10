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
    internal sealed class ColumnRebarBuildResult
    {
        public int Elements { get; set; }
        public int Bars { get; set; }
    }

    internal static class ColumnRebarSolidBuilder
    {
        private const int MaxBarsPerElement = 1200;
        private const int MaxBarsPerBatch = 4000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandles { get; set; } = string.Empty;
            public List<string> Handles { get; } = new List<string>();
        }

        public static ColumnRebarBuildResult BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new ColumnRebarBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in selection.Value.GetObjectIds())
            {
                try { selectedHandles.Add(id.Handle.ToString()); }
                catch { }
            }
            var candidates = project.Elements
                .Where(x => x.Category == ElementCategory.Column && x.SourceHandles.Any(selectedHandles.Contains))
                .ToList();
            if (candidates.Count == 0) return new ColumnRebarBuildResult();

            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var totalBars = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var element in candidates)
                {
                    var family = project.FindFamily(element.FamilyId);
                    var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WidthM", 0.3d), element.Id + "/WidthM");
                    var depthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "DepthM", widthM), element.Id + "/DepthM");
                    var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3d), element.Id + "/HeightM");
                    var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", 0.04d);
                    if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM không được âm.");
                    var diameterMm = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "RebarDiameterMm", 20d), element.Id + "/RebarDiameterMm");
                    var barsWidth = ParseCount(element, family, "RebarBarsAlongWidth", 4);
                    var barsDepth = ParseCount(element, family, "RebarBarsAlongDepth", 4);
                    var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
                    {
                        WidthM = widthM,
                        DepthM = depthM,
                        CoverM = coverM,
                        DiameterMm = diameterMm,
                        BarsAlongWidth = barsWidth,
                        BarsAlongDepth = barsDepth
                    });
                    if (layout.BarCenters.Count > MaxBarsPerElement)
                        throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " thanh cột 3D.");
                    if ((long)totalBars + layout.BarCenters.Count > MaxBarsPerBatch)
                        throw new InvalidOperationException("Column Rebar 3D vượt giới hạn " + MaxBarsPerBatch + " thanh/batch.");

                    var source = OpenSelectedPolyline(document, transaction, element, selectedHandles);
                    if (source == null) throw new InvalidOperationException("Không tìm thấy selected closed POLYLINE source cho " + element.Id);
                    var frame = BuildFrame(source, element.Id);
                    var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, widthM, element.Id + "/width"), element.Id + "/width drawing units");
                    var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, element.Id + "/depth"), element.Id + "/depth drawing units");
                    var dimensionTolerance = AddFinite(MultiplyFinite(Math.Max(width, depth), 1e-4d, element.Id + "/dimension tolerance scale"), 1e-6d, element.Id + "/dimension tolerance");
                    if (Math.Abs(frame.Width - width) > dimensionTolerance || Math.Abs(frame.Depth - depth) > dimensionTolerance)
                        throw new InvalidOperationException("Kích thước POLYLINE source không khớp WidthM/DepthM của " + element.Id + ".");
                    var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/height"), element.Id + "/height drawing units");
                    var bottom = CadGeometryGuard.ToDrawingUnits(document, CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d), element.Id + "/bottom");
                    var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, diameterMm / 2000d, element.Id + "/radius"), element.Id + "/radius drawing units");

                    var pendingItem = new PendingUpdate { Element = element };
                    if (element.Properties.TryGetValue("GeneratedRebarHandles", out var previous)) pendingItem.PreviousHandles = previous;
                    ErasePrevious(document, transaction, element, pendingItem.PreviousHandles, ownership);
                    foreach (var center in layout.BarCenters)
                    {
                        var localX = CadGeometryGuard.ToDrawingUnits(document, center.X, element.Id + "/rebar local X");
                        var localY = CadGeometryGuard.ToDrawingUnits(document, center.Y, element.Id + "/rebar local Y");
                        var worldX = AddFinite(frame.CenterX, AddFinite(MultiplyFinite(frame.Ux, localX, element.Id + "/rebar Ux"), MultiplyFinite(frame.Vx, localY, element.Id + "/rebar Vx"), element.Id + "/rebar local X mix"), element.Id + "/rebar world X");
                        var worldY = AddFinite(frame.CenterY, AddFinite(MultiplyFinite(frame.Uy, localX, element.Id + "/rebar Uy"), MultiplyFinite(frame.Vy, localY, element.Id + "/rebar Vy"), element.Id + "/rebar local Y mix"), element.Id + "/rebar world Y");
                        var baseZ = AddFinite(frame.Elevation, bottom, element.Id + "/rebar base Z");
                        var solid = CreateVerticalBar(document, worldX, worldY, baseZ, height, radius, element.Id);
                        try
                        {
                            solid.Layer = source.Layer;
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            pendingItem.Handles.Add(solid.Handle.ToString());
                            solid = null!;
                        }
                        finally { solid?.Dispose(); }
                    }
                    totalBars += layout.BarCenters.Count;
                    pending.Add(pendingItem);
                }
                transaction.Commit();
            }

            foreach (var item in pending)
            {
                item.Element.Properties["GeneratedRebarHandles"] = string.Join(";", item.Handles);
                item.Element.Properties["GeneratedRebarCount"] = item.Handles.Count.ToString(CultureInfo.InvariantCulture);
                item.Element.Properties["GeneratedRebarMode"] = "Column.Perimeter.Longitudinal";
            }
            if (pending.Count > 0)
            {
                project.Touch();
                document.Editor.Regen();
                AuditTrail.ForProject(project).Record("geometry.rebar3d", string.Join(",", pending.Select(x => x.Element.Id)), "bars=" + totalBars.ToString(CultureInfo.InvariantCulture));
            }
            return new ColumnRebarBuildResult { Elements = pending.Count, Bars = totalBars };
        }

        private sealed class Frame
        {
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Ux { get; set; }
            public double Uy { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Width { get; set; }
            public double Depth { get; set; }
            public double Elevation { get; set; }
        }

        private static Frame BuildFrame(Polyline polyline, string elementId)
        {
            if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException("Column Rebar 3D yêu cầu closed POLYLINE 4 đỉnh: " + elementId);
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException("Column Rebar 3D yêu cầu POLYLINE plan-view: " + elementId);
            for (var i = 0; i < 4; i++)
                if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12d) throw new InvalidOperationException("Column Rebar 3D chưa hỗ trợ cạnh cong: " + elementId);
            var p0 = polyline.GetPoint2dAt(0); var p1 = polyline.GetPoint2dAt(1); var p2 = polyline.GetPoint2dAt(2); var p3 = polyline.GetPoint2dAt(3);
            var e1x = SubtractFinite(p1.X, p0.X, elementId + "/edge1 X"); var e1y = SubtractFinite(p1.Y, p0.Y, elementId + "/edge1 Y");
            var e2x = SubtractFinite(p3.X, p0.X, elementId + "/edge2 X"); var e2y = SubtractFinite(p3.Y, p0.Y, elementId + "/edge2 Y");
            var l1 = CadGeometryGuard.Hypot(e1x, e1y, elementId + "/edge1"); var l2 = CadGeometryGuard.Hypot(e2x, e2y, elementId + "/edge2");
            if (l1 <= 1e-9d || l2 <= 1e-9d) throw new InvalidOperationException("Column source rectangle có cạnh rỗng: " + elementId);
            var ux = e1x / l1; var uy = e1y / l1; var vx = e2x / l2; var vy = e2y / l2;
            var dot = Math.Abs(AddFinite(MultiplyFinite(ux, vx, elementId + "/rectangle dot X"), MultiplyFinite(uy, vy, elementId + "/rectangle dot Y"), elementId + "/rectangle orthogonality"));
            if (dot > 1e-6d) throw new InvalidOperationException("Column source POLYLINE phải là hình chữ nhật trực giao: " + elementId);
            var expectedP2X = AddFinite(AddFinite(p0.X, e1x, elementId + "/expected P2 X"), e2x, elementId + "/expected P2 X");
            var expectedP2Y = AddFinite(AddFinite(p0.Y, e1y, elementId + "/expected P2 Y"), e2y, elementId + "/expected P2 Y");
            var geometryTolerance = AddFinite(MultiplyFinite(Math.Max(l1, l2), 1e-6d, elementId + "/geometry tolerance"), 1e-8d, elementId + "/geometry tolerance");
            if (Distance(p2.X, p2.Y, expectedP2X, expectedP2Y, elementId + "/P2 closure") > geometryTolerance)
                throw new InvalidOperationException("Column source POLYLINE có đỉnh thứ ba không tạo hình chữ nhật: " + elementId);
            var centerX = CadGeometryGuard.Midpoint(p0.X, p2.X, elementId + "/center X");
            var centerY = CadGeometryGuard.Midpoint(p0.Y, p2.Y, elementId + "/center Y");
            return new Frame
            {
                CenterX = centerX,
                CenterY = centerY,
                Ux = ux,
                Uy = uy,
                Vx = vx,
                Vy = vy,
                Width = l1,
                Depth = l2,
                Elevation = CadGeometryGuard.Finite(polyline.Elevation, elementId + "/elevation")
            };
        }

        private static double Distance(double x1, double y1, double x2, double y2, string label)
        {
            var dx = SubtractFinite(x1, x2, label + "/dx");
            var dy = SubtractFinite(y1, y2, label + "/dy");
            return CadGeometryGuard.Hypot(dx, dy, label);
        }

        private static Solid3d CreateVerticalBar(Document document, double x, double y, double baseZ, double height, double radius, string label)
        {
            x = CadGeometryGuard.Finite(x, label + "/bar X");
            y = CadGeometryGuard.Finite(y, label + "/bar Y");
            baseZ = CadGeometryGuard.Finite(baseZ, label + "/bar Z");
            height = CadGeometryGuard.Positive(height, label + "/bar height");
            radius = CadGeometryGuard.Positive(radius, label + "/bar radius");
            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(height, radius, radius, radius);
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, baseZ)));
                var completed = solid; solid = null!; return completed;
            }
            finally { solid?.Dispose(); }
        }

        private static Polyline? OpenSelectedPolyline(Document document, Transaction transaction, ProjectElement element, ISet<string> selectedHandles)
        {
            Polyline? result = null;
            foreach (var text in element.SourceHandles.Where(selectedHandles.Contains))
            {
                if (!long.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) continue;
                ObjectId id;
                try { id = document.Database.GetObjectId(false, new Handle(value), 0); }
                catch { continue; }
                if (id.IsNull || !id.IsValid) continue;
                var polyline = transaction.GetObject(id, OpenMode.ForRead, true) as Polyline;
                if (polyline == null || polyline.IsErased) continue;
                if (result != null) throw new InvalidOperationException("Element " + element.Id + " có nhiều selected POLYLINE source; chọn đúng một source để dựng rebar.");
                result = polyline;
            }
            return result;
        }

        private static int ParseCount(ProjectElement element, ProjectFamily? family, string key, int fallback)
        {
            var raw = element.Properties.TryGetValue(key, out var local) ? local : family != null && family.Properties.TryGetValue(key, out var inherited) ? inherited : fallback.ToString(CultureInfo.InvariantCulture);
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 2) throw new InvalidOperationException(element.Id + "/" + key + " không hợp lệ.");
            return value;
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element, string raw, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element, "GeneratedRebarHandles");
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated rebar handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var solid = entity as Solid3d;
                if (solid == null) throw new InvalidOperationException("Generated rebar handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                solid.Erase();
            }
        }

        private static double MultiplyFinite(double first, double second, string label)
            => CadGeometryGuard.Finite(CadGeometryGuard.Finite(first, label + "/first") * CadGeometryGuard.Finite(second, label + "/second"), label);

        private static double AddFinite(double first, double second, string label) => CadGeometryGuard.Add(first, second, label);

        private static double SubtractFinite(double first, double second, string label)
            => CadGeometryGuard.Finite(CadGeometryGuard.Finite(first, label + "/first") - CadGeometryGuard.Finite(second, label + "/second"), label);
    }
}
