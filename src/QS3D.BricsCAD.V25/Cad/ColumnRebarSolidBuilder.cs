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
    internal static class ColumnRebarSolidBuilder
    {
        private const int MaxBarsPerElement = 1200;
        private const int MaxBarsPerBatch = 4000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double DiameterMm { get; set; }
            public double CoverM { get; set; }
        }

        public static int BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return 0;

            var pending = new List<PendingUpdate>();
            var processedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ownership = GeneratedRebarOwnershipGuard.Build(project);
            var totalBars = 0;
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    foreach (var id in ids)
                    {
                        var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                        if (polyline == null || polyline.IsErased) continue;
                        if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException("QS3DREBAR3D hiện hỗ trợ cột bằng closed 4-vertex POLYLINE chữ nhật.");
                        var normal = polyline.Normal;
                        if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                            throw new InvalidOperationException("QS3DREBAR3D yêu cầu column footprint nằm trên mặt phẳng XY.");
                        for (var vertex = 0; vertex < 4; vertex++)
                            if (Math.Abs(polyline.GetBulgeAt(vertex)) > 1e-12d) throw new InvalidOperationException("QS3DREBAR3D chưa hỗ trợ cột rectangle có bulge.");
                        CadGeometryGuard.Finite(polyline.Elevation, "Column footprint elevation");

                        var handle = polyline.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == ElementCategory.Column && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("Column source " + handle + " đang thuộc nhiều QS3D element.");
                        var element = matches[0];
                        if (!processedElements.Add(element.Id)) throw new InvalidOperationException("Column element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi tạo rebar 3D.");
                        var family = project.FindFamily(element.FamilyId);
                        if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation))
                            throw new InvalidOperationException(element.Id + " chưa có RebarNotation.");
                        var groups = RebarNotationParser.Parse(notation);
                        if (groups.Count != 1) throw new InvalidOperationException("QS3DREBAR3D column geometry yêu cầu một nhóm đường kính duy nhất; compound notation vẫn được dùng cho BBS nhưng chưa dùng cho 3D placement.");
                        var group = groups[0];
                        var diameterMm = CadGeometryGuard.Positive(group.DiameterMm, element.Id + "/rebar diameter");
                        var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", 0.04d);
                        if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM phải >= 0.");
                        var bars = ResolveBarGrid(element, group);

                        var p0 = polyline.GetPoint2dAt(0);
                        var p1 = polyline.GetPoint2dAt(1);
                        var p2 = polyline.GetPoint2dAt(2);
                        var p3 = polyline.GetPoint2dAt(3);
                        var e1x = CadGeometryGuard.Subtract(p1.X, p0.X, element.Id + "/edge1 X");
                        var e1y = CadGeometryGuard.Subtract(p1.Y, p0.Y, element.Id + "/edge1 Y");
                        var e2x = CadGeometryGuard.Subtract(p2.X, p1.X, element.Id + "/edge2 X");
                        var e2y = CadGeometryGuard.Subtract(p2.Y, p1.Y, element.Id + "/edge2 Y");
                        var widthDrawing = CadGeometryGuard.Hypot(e1x, e1y, element.Id + "/column width");
                        var depthDrawing = CadGeometryGuard.Hypot(e2x, e2y, element.Id + "/column depth");
                        if (widthDrawing <= 1e-8d || depthDrawing <= 1e-8d) throw new InvalidOperationException("Column footprint bị suy biến: " + element.Id);
                        var ux = e1x / widthDrawing; var uy = e1y / widthDrawing;
                        var vx = e2x / depthDrawing; var vy = e2y / depthDrawing;
                        var orthogonality = Math.Abs(CadGeometryGuard.Add(
                            CadGeometryGuard.Multiply(ux, vx, element.Id + "/rectangle dot X"),
                            CadGeometryGuard.Multiply(uy, vy, element.Id + "/rectangle dot Y"),
                            element.Id + "/rectangle orthogonality"));
                        if (orthogonality > 1e-6d) throw new InvalidOperationException("Column footprint không vuông góc: " + element.Id);
                        var expectedP2X = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.X, e1x, element.Id + "/expected P2 X"), e2x, element.Id + "/expected P2 X");
                        var expectedP2Y = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.Y, e1y, element.Id + "/expected P2 Y"), e2y, element.Id + "/expected P2 Y");
                        var expectedP3X = CadGeometryGuard.Add(p0.X, e2x, element.Id + "/expected P3 X");
                        var expectedP3Y = CadGeometryGuard.Add(p0.Y, e2y, element.Id + "/expected P3 Y");
                        var geometryTolerance = CadGeometryGuard.Add(CadGeometryGuard.Multiply(Math.Max(widthDrawing, depthDrawing), 1e-6d, element.Id + "/geometry tolerance scale"), 1e-8d, element.Id + "/geometry tolerance");
                        if (Distance(p2.X, p2.Y, expectedP2X, expectedP2Y, element.Id + "/P2 closure") > geometryTolerance || Distance(p3.X, p3.Y, expectedP3X, expectedP3Y, element.Id + "/P3 closure") > geometryTolerance)
                            throw new InvalidOperationException("Column footprint phải là rectangle/parallelogram vuông kín theo thứ tự vertex.");

                        var widthM = CadGeometryGuard.ToMeters(document, widthDrawing, element.Id + "/column width");
                        var depthM = CadGeometryGuard.ToMeters(document, depthDrawing, element.Id + "/column depth");
                        var layout = RectangularRebarLayoutPlanner.Plan(new RectangularRebarLayoutInput
                        {
                            WidthM = widthM,
                            DepthM = depthM,
                            CoverM = coverM,
                            DiameterMm = diameterMm,
                            BarsAlongWidth = bars.Item1,
                            BarsAlongDepth = bars.Item2
                        });
                        if (layout.BarCenters.Count > MaxBarsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " thanh cột 3D.");
                        if (totalBars > MaxBarsPerBatch - layout.BarCenters.Count) throw new InvalidOperationException("Column Rebar 3D vượt giới hạn " + MaxBarsPerBatch + " thanh/batch.");

                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/rebar height"), element.Id + "/rebar drawing height");
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, diameterMm / 2000d, element.Id + "/rebar radius"), element.Id + "/rebar drawing radius");
                        var bottom = CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM");
                        var centerX = CadGeometryGuard.Midpoint(p0.X, p2.X, element.Id + "/center X");
                        var centerY = CadGeometryGuard.Midpoint(p0.Y, p2.Y, element.Id + "/center Y");
                        var baseZ = CadGeometryGuard.Add(polyline.Elevation, bottom, element.Id + "/rebar base Z");

                        ErasePrevious(document, transaction, project, element, ownership);
                        var update = new PendingUpdate { Element = element, DiameterMm = diameterMm, CoverM = coverM };
                        foreach (var local in layout.BarCenters)
                        {
                            var localX = CadGeometryGuard.ToDrawingUnits(document, local.X, element.Id + "/rebar local X");
                            var localY = CadGeometryGuard.ToDrawingUnits(document, local.Y, element.Id + "/rebar local Y");
                            var xOffset = CadGeometryGuard.Add(
                                CadGeometryGuard.Multiply(ux, localX, element.Id + "/rebar Ux"),
                                CadGeometryGuard.Multiply(vx, localY, element.Id + "/rebar Vx"),
                                element.Id + "/rebar X offset");
                            var yOffset = CadGeometryGuard.Add(
                                CadGeometryGuard.Multiply(uy, localX, element.Id + "/rebar Uy"),
                                CadGeometryGuard.Multiply(vy, localY, element.Id + "/rebar Vy"),
                                element.Id + "/rebar Y offset");
                            var x = CadGeometryGuard.Add(centerX, xOffset, element.Id + "/rebar X");
                            var y = CadGeometryGuard.Add(centerY, yOffset, element.Id + "/rebar Y");
                            var bar = CreateVerticalBar(document, x, y, baseZ, height, radius, element.Id);
                            try
                            {
                                bar.Layer = polyline.Layer;
                                modelSpace.AppendEntity(bar);
                                transaction.AddNewlyCreatedDBObject(bar, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, bar, project, element, "GeneratedRebarHandles");
                                update.Handles.Add(bar.Handle.ToString());
                                bar = null!;
                            }
                            finally { bar?.Dispose(); }
                        }
                        pending.Add(update);
                        totalBars = checked(totalBars + update.Handles.Count);
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
                            "Column rebar replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return totalBars;
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties["GeneratedRebarHandles"] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedRebarCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarDiameterMm"] = update.DiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarMode"] = "ColumnVerticalBars";
            update.Element.ClearGeneratedRebarStale();
            AuditTrail.ForProject(project).Record("geometry.rebar.column", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars");
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
                var completed = solid;
                solid = null!;
                return completed;
            }
            finally { solid?.Dispose(); }
        }

        private static Tuple<int, int> ResolveBarGrid(ProjectElement element, RebarGroup group)
        {
            var explicitWidth = Integer(element, "RebarBarsAlongWidth");
            var explicitDepth = Integer(element, "RebarBarsAlongDepth");
            if (explicitWidth.HasValue || explicitDepth.HasValue)
            {
                if (!explicitWidth.HasValue || !explicitDepth.HasValue) throw new InvalidOperationException("Khai báo đồng thời RebarBarsAlongWidth và RebarBarsAlongDepth.");
                return Tuple.Create(explicitWidth.Value, explicitDepth.Value);
            }

            if (!group.Quantity.HasValue || group.Quantity.Value < 4 || group.Quantity.Value % 2 != 0)
                throw new InvalidOperationException("Không thể suy ra layout cột từ notation. Dùng số thanh chẵn >= 4 hoặc khai báo RebarBarsAlongWidth/RebarBarsAlongDepth.");
            var sum = checked((group.Quantity.Value + 4) / 2);
            var width = Math.Max(2, sum / 2);
            var depth = sum - width;
            if (depth < 2) { depth = 2; width = sum - depth; }
            if (checked(2 * width + 2 * (depth - 2)) != group.Quantity.Value) throw new InvalidOperationException("Rebar quantity không khớp rectangular perimeter layout.");
            return Tuple.Create(width, depth);
        }

        private static int? Integer(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return null;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 2 || value > 5000) throw new InvalidOperationException(element.Id + "/" + key + " phải là integer từ 2 đến 5000.");
            return value;
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue("GeneratedRebarHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) return;
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
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "GeneratedRebarHandles", "erase generated column rebar " + handle);
                solid.Erase();
            }
        }

        private static double Distance(double x1, double y1, double x2, double y2, string label)
        {
            var dx = CadGeometryGuard.Subtract(x2, x1, label + "/dx");
            var dy = CadGeometryGuard.Subtract(y2, y1, label + "/dy");
            return CadGeometryGuard.Hypot(dx, dy, label);
        }
    }
}