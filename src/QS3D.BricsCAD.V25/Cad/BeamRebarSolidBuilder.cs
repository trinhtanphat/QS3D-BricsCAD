using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class BeamRebarSolidBuilder
    {
        private const int MaxBarsPerElement = 1024;
        private const int MaxBarsPerBatch = 4096;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double DiameterMm { get; set; }
            public double CoverM { get; set; }
            public double EndCoverM { get; set; }
            public int TopCount { get; set; }
            public int BottomCount { get; set; }
            public CadElementVerticalPlacement VerticalPlacement { get; set; } = null!;
        }

        public static int BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));
            if (selectedIds.Length == 0) return 0;
            var ids = (ObjectId[])selectedIds.Clone();
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
                        var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                        if (line == null || line.IsErased) continue;
                        var handle = line.Handle.ToString();
                        var matches = project.Elements.Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase))).Take(2).ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("Beam source " + handle + " đang thuộc nhiều QS3D element.");
                        var element = matches[0];
                        if (!processedElements.Add(element.Id)) throw new InvalidOperationException("Beam element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi tạo rebar 3D.");
                        var family = project.FindFamily(element.FamilyId);
                        if (!element.Properties.TryGetValue("RebarNotation", out var notation) || string.IsNullOrWhiteSpace(notation)) throw new InvalidOperationException(element.Id + " chưa có RebarNotation.");
                        var groups = RebarNotationParser.Parse(notation);
                        var diameterMm = ResolveDiameter(element, groups);
                        var counts = ResolveLayerCounts(element, groups);
                        var elementBarCount = checked(counts.Item1 + counts.Item2);
                        if (elementBarCount > MaxBarsPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxBarsPerElement + " Beam longitudinal bar/element.");
                        if (totalBars > MaxBarsPerBatch - elementBarCount) throw new InvalidOperationException("Beam longitudinal rebar batch vượt giới hạn " + MaxBarsPerBatch + " bar.");
                        var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", 0.04d);
                        if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM phải >= 0.");
                        var endCoverM = CadGeometryGuard.Number(element, family, "RebarBeamEndCoverM", coverM);
                        if (endCoverM < 0d) throw new InvalidOperationException(element.Id + "/RebarBeamEndCoverM phải >= 0.");
                        var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WidthM", 0d), element.Id + "/WidthM");
                        var vertical = CadElementVerticalPlacement.Resolve(
                            document, project, element, family, line.StartPoint.Z, "HeightM", .5d);
                        var heightM = vertical.HeightM;
                        var layout = BeamLongitudinalRebarPlanner.Plan(new BeamLongitudinalRebarLayoutInput { WidthM = widthM, HeightM = heightM, CoverM = coverM, DiameterMm = diameterMm, TopCount = counts.Item1, BottomCount = counts.Item2 });
                        var dx = CadGeometryGuard.Finite(line.EndPoint.X - line.StartPoint.X, element.Id + "/beam direction X");
                        var dy = CadGeometryGuard.Finite(line.EndPoint.Y - line.StartPoint.Y, element.Id + "/beam direction Y");
                        var dz = CadGeometryGuard.Finite(line.EndPoint.Z - line.StartPoint.Z, element.Id + "/beam direction Z");
                        var xyLength = CadGeometryGuard.Hypot(dx, dy, element.Id + "/beam XY length");
                        if (xyLength <= 1e-8d) throw new InvalidOperationException("Beam source LINE bị suy biến: " + element.Id);
                        var horizontalTolerance = Math.Max(1e-8d, Math.Abs(CadGeometryGuard.ToDrawingUnits(document, 0.005d, element.Id + "/beam horizontal tolerance")));
                        if (Math.Abs(dz) > horizontalTolerance) throw new InvalidOperationException("QS3DBEAMREBAR3D hiện chỉ hỗ trợ Beam LINE gần nằm ngang trong mặt phẳng XY (|ΔZ| <= 5 mm).");
                        var lengthM = CadGeometryGuard.ToMeters(document, xyLength, element.Id + "/beam length");
                        var twoEndCovers = CadGeometryGuard.Finite(endCoverM * 2d, element.Id + "/two end covers");
                        var barLengthM = CadGeometryGuard.Finite(lengthM - twoEndCovers, element.Id + "/beam rebar usable length");
                        if (barLengthM <= 1e-9d) throw new InvalidOperationException(element.Id + ": RebarBeamEndCoverM không còn chiều dài thanh hữu dụng.");
                        var ux = dx / xyLength; var uy = dy / xyLength; var nx = -uy; var ny = ux; var angle = Math.Atan2(uy, ux);
                        var barLength = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, barLengthM, element.Id + "/beam rebar length"), element.Id + "/beam rebar drawing length");
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, diameterMm / 2000d, element.Id + "/beam rebar radius"), element.Id + "/beam rebar drawing radius");
                        var endCover = CadGeometryGuard.ToDrawingUnits(document, endCoverM, element.Id + "/beam end cover");
                        var startX = CadGeometryGuard.Add(line.StartPoint.X, CadGeometryGuard.Finite(ux * endCover, element.Id + "/beam rebar start dx"), element.Id + "/beam rebar start X");
                        var startY = CadGeometryGuard.Add(line.StartPoint.Y, CadGeometryGuard.Finite(uy * endCover, element.Id + "/beam rebar start dy"), element.Id + "/beam rebar start Y");
                        var halfBarLength = CadGeometryGuard.Finite(barLength / 2d, element.Id + "/beam rebar half length");
                        var longitudinalCenterX = CadGeometryGuard.Add(startX, CadGeometryGuard.Finite(ux * halfBarLength, element.Id + "/beam rebar center dx"), element.Id + "/beam rebar center X");
                        var longitudinalCenterY = CadGeometryGuard.Add(startY, CadGeometryGuard.Finite(uy * halfBarLength, element.Id + "/beam rebar center dy"), element.Id + "/beam rebar center Y");
                        var centerZ = vertical.CenterDrawing;
                        ErasePrevious(document, transaction, project, element, ownership);
                        var update = new PendingUpdate { Element = element, DiameterMm = diameterMm, CoverM = coverM, EndCoverM = endCoverM, TopCount = counts.Item1, BottomCount = counts.Item2, VerticalPlacement = vertical };
                        foreach (var local in layout.TopBarCenters.Concat(layout.BottomBarCenters))
                        {
                            var localX = CadGeometryGuard.ToDrawingUnits(document, local.X, element.Id + "/beam rebar transverse offset");
                            var localZ = CadGeometryGuard.ToDrawingUnits(document, local.Y, element.Id + "/beam rebar vertical offset");
                            var x = CadGeometryGuard.Add(longitudinalCenterX, CadGeometryGuard.Finite(nx * localX, element.Id + "/beam rebar transverse X"), element.Id + "/beam rebar X");
                            var y = CadGeometryGuard.Add(longitudinalCenterY, CadGeometryGuard.Finite(ny * localX, element.Id + "/beam rebar transverse Y"), element.Id + "/beam rebar Y");
                            var z = CadGeometryGuard.Add(centerZ, localZ, element.Id + "/beam rebar Z");
                            Solid3d? bar = new Solid3d();
                            try
                            {
                                bar.SetDatabaseDefaults(document.Database);
                                bar.CreateFrustum(barLength, radius, radius, radius);
                                bar.TransformBy(Matrix3d.Rotation(Math.PI / 2d, Vector3d.YAxis, Point3d.Origin));
                                bar.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                                bar.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, z)));
                                bar.Layer = line.Layer;
                                modelSpace.AppendEntity(bar);
                                transaction.AddNewlyCreatedDBObject(bar, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, bar, project, element, "GeneratedRebarHandles");
                                update.Handles.Add(bar.Handle.ToString());
                                bar = null;
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
                            "Beam longitudinal rebar replacement failed before CAD commit and project rollback also failed.",
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
            update.Element.Properties["GeneratedRebarBeamEndCoverM"] = update.EndCoverM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarBeamTopCount"] = update.TopCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarBeamBottomCount"] = update.BottomCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedRebarMode"] = "BeamLongitudinalBars";
            CadElementVerticalPlacement.CommitSnapshot(update.Element, "GeneratedRebar", update.VerticalPlacement);
            update.Element.ClearGeneratedRebarStale();
            AuditTrail.ForProject(project).Record("geometry.rebar.beam", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " bars");
        }

        private static double ResolveDiameter(ProjectElement element, IReadOnlyList<RebarGroup> groups)
        {
            if (element.Properties.TryGetValue("RebarBeamDiameterMm", out var text) && !string.IsNullOrWhiteSpace(text))
            {
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new InvalidOperationException(element.Id + "/RebarBeamDiameterMm phải là số hữu hạn > 0.");
                return value;
            }
            if (groups.Count == 0) throw new InvalidOperationException(element.Id + ": RebarNotation không có group hợp lệ.");
            var diameter = CadGeometryGuard.Positive(groups[0].DiameterMm, element.Id + "/beam rebar diameter");
            if (groups.Any(x => Math.Abs(x.DiameterMm - diameter) > 1e-9d)) throw new InvalidOperationException(element.Id + ": compound RebarNotation có nhiều đường kính; khai báo RebarBeamDiameterMm để xác định geometry 3D.");
            return diameter;
        }

        private static Tuple<int, int> ResolveLayerCounts(ProjectElement element, IReadOnlyList<RebarGroup> groups)
        {
            var top = Integer(element, "RebarBeamTopCount"); var bottom = Integer(element, "RebarBeamBottomCount");
            if (top.HasValue || bottom.HasValue)
            {
                if (!top.HasValue || !bottom.HasValue) throw new InvalidOperationException("Khai báo đồng thời RebarBeamTopCount và RebarBeamBottomCount.");
                return Tuple.Create(top.GetValueOrDefault(), bottom.GetValueOrDefault());
            }
            if (groups.Count == 1 && groups[0].Quantity.HasValue)
            {
                var quantity = groups[0].Quantity.GetValueOrDefault();
                if (quantity >= 4 && quantity % 2 == 0) return Tuple.Create(quantity / 2, quantity / 2);
            }
            throw new InvalidOperationException(element.Id + ": không thể suy ra top/bottom beam layout từ RebarNotation. Khai báo RebarBeamTopCount và RebarBeamBottomCount.");
        }

        private static int? Integer(ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return null;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 2 || value > 512) throw new InvalidOperationException(element.Id + "/" + key + " phải là integer từ 2 đến 512.");
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
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "GeneratedRebarHandles", "erase generated beam rebar " + handle);
                solid.Erase();
            }
        }
    }
}