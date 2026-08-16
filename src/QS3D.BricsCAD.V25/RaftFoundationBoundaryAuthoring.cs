using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25
{
    internal static class RaftFoundationBoundaryAuthoring
    {
        private const double GeometryTolerance = 1e-9d;

        public static void Execute()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                RequireModelSpace(document);
                var boundary = AcquireExactBoundary(document);
                if (boundary == null) return;
                EnsureActive(document, "QS3DDRAWRAFTFOUNDATION / before mutation");
                ExecuteFoundation(document, boundary);
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DDRAWRAFTFOUNDATION error: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DDRAWRAFTFOUNDATION lỗi: " + ex.Message);
            }
        }

        private static IReadOnlyList<Point3d>? AcquireExactBoundary(Document document)
        {
            var options = new PromptEntityOptions("\nMóng Bè - chọn closed Polyline hoặc Region kín: ");
            var result = document.Editor.GetEntity(options);
            if (result.Status != PromptStatus.OK) return null;

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var selected = transaction.GetObject(result.ObjectId, OpenMode.ForRead, false);
                IReadOnlyList<Point3d> points;
                if (selected is Polyline polyline)
                    points = ReadClosedLinearPolyline(polyline);
                else if (selected is Region region)
                    points = ReadSingleLinearRegion(region);
                else
                    throw new InvalidOperationException("Móng Bè chỉ nhận closed Polyline hoặc Region kín. Entity đã chọn là " + selected.GetType().Name + ".");

                transaction.Commit();
                return points;
            }
        }

        private static IReadOnlyList<Point3d> ReadClosedLinearPolyline(Polyline polyline)
        {
            if (!polyline.Closed)
                throw new InvalidOperationException("Polyline Móng Bè phải Closed.");
            if (polyline.NumberOfVertices < 3)
                throw new InvalidOperationException("Polyline Móng Bè cần ít nhất 3 đỉnh.");

            var normal = polyline.Normal;
            var normalLength = normal.Length;
            if (double.IsNaN(normalLength) || double.IsInfinity(normalLength) || !(normalLength > 0d))
                throw new InvalidOperationException("Polyline Móng Bè có normal không hợp lệ.");
            var nx = normal.X / normalLength;
            var ny = normal.Y / normalLength;
            var nz = normal.Z / normalLength;
            if (Math.Abs(nx) > GeometryTolerance || Math.Abs(ny) > GeometryTolerance || Math.Abs(Math.Abs(nz) - 1d) > GeometryTolerance)
                throw new InvalidOperationException("Polyline Móng Bè phải nằm chính xác trên mặt phẳng song song WCS XY; boundary nghiêng/3D không được phép suy đoán.");

            var points = new List<Point3d>(polyline.NumberOfVertices);
            for (var index = 0; index < polyline.NumberOfVertices; index++)
            {
                if (Math.Abs(polyline.GetBulgeAt(index)) > GeometryTolerance)
                    throw new InvalidOperationException("Polyline Móng Bè có ARC/bulge. Foundation hiện chỉ hỗ trợ polygon cạnh thẳng; QS3D không tessellate hoặc xấp xỉ boundary.");
                var point = polyline.GetPoint3dAt(index);
                RequireFinite(point, "Polyline Móng Bè vertex " + index);
                points.Add(point);
            }

            RequireSingleElevation(points, "Polyline Móng Bè");
            RequireNonDegenerateEdges(points, "Polyline Móng Bè");
            return points;
        }

        private static IReadOnlyList<Point3d> ReadSingleLinearRegion(Region region)
        {
            var exploded = new DBObjectCollection();
            try
            {
                region.Explode(exploded);
                if (exploded.Count < 3)
                    throw new InvalidOperationException("Region Móng Bè không tạo được một loop kín có ít nhất 3 cạnh.");

                var segments = new List<BoundarySegment>(exploded.Count);
                for (var index = 0; index < exploded.Count; index++)
                {
                    var item = exploded[index];
                    var line = item as Line;
                    if (line == null)
                        throw new InvalidOperationException("Region Móng Bè chứa ARC/SPLINE/curve. QS3D chỉ nhận Region polygon cạnh thẳng và không tessellate hoặc xấp xỉ hình học.");

                    RequireFinite(line.StartPoint, "Region Móng Bè line start " + index);
                    RequireFinite(line.EndPoint, "Region Móng Bè line end " + index);
                    if (SamePoint(line.StartPoint, line.EndPoint))
                        throw new InvalidOperationException("Region Móng Bè chứa cạnh LINE có độ dài bằng 0.");
                    segments.Add(new BoundarySegment(line.StartPoint, line.EndPoint));
                }

                var points = OrderSingleClosedLoop(segments);
                RequireSingleElevation(points, "Region Móng Bè");
                RequireNonDegenerateEdges(points, "Region Móng Bè");
                return points;
            }
            finally
            {
                for (var index = 0; index < exploded.Count; index++)
                    exploded[index]?.Dispose();
            }
        }

        private static IReadOnlyList<Point3d> OrderSingleClosedLoop(IReadOnlyList<BoundarySegment> segments)
        {
            var used = new bool[segments.Count];
            var ordered = new List<Point3d>(segments.Count)
            {
                segments[0].Start,
                segments[0].End
            };
            used[0] = true;
            var current = segments[0].End;
            var usedCount = 1;

            while (!SamePoint(current, ordered[0]))
            {
                var nextIndex = -1;
                var nextPoint = default(Point3d);
                for (var index = 0; index < segments.Count; index++)
                {
                    if (used[index]) continue;
                    var matchesStart = SamePoint(segments[index].Start, current);
                    var matchesEnd = SamePoint(segments[index].End, current);
                    if (!matchesStart && !matchesEnd) continue;
                    if (nextIndex >= 0)
                        throw new InvalidOperationException("Region Móng Bè có nhánh/touching loop; chỉ hỗ trợ đúng một loop kín đơn.");
                    nextIndex = index;
                    nextPoint = matchesStart ? segments[index].End : segments[index].Start;
                }

                if (nextIndex < 0)
                    throw new InvalidOperationException("Region Móng Bè bị hở hoặc các cạnh không tạo thành một loop liên tục.");

                used[nextIndex] = true;
                usedCount++;
                current = nextPoint;
                if (!SamePoint(current, ordered[0])) ordered.Add(current);
                if (usedCount > segments.Count)
                    throw new InvalidOperationException("Region Móng Bè có topology không hợp lệ.");
            }

            if (usedCount != segments.Count || used.Any(value => !value))
                throw new InvalidOperationException("Region Móng Bè có nhiều loop/hole. Foundation hiện chỉ hỗ trợ đúng một loop kín; QS3D không bỏ qua lỗ.");
            if (ordered.Count < 3)
                throw new InvalidOperationException("Region Móng Bè cần ít nhất 3 đỉnh khác nhau.");
            return ordered;
        }

        private static void ExecuteFoundation(Document document, IReadOnlyList<Point3d> boundary)
        {
            const string operation = "QS3DDRAWRAFTFOUNDATION";
            EnsureActive(document, operation);
            var projectExistedBeforeAuthoring = ProjectContextCoordinator.TryGetReadOnly(document, out _);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            ProjectElement? createdElement = null;
            var generatedHandle = string.Empty;

            try
            {
                sourceId = CreateExactWcsPolyline(document, boundary);
                if (sourceId.IsNull || !sourceId.IsValid)
                    throw new InvalidOperationException("Không tạo được CAD source clone cho Móng Bè.");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, ElementCategory.Foundation);
                if (captured != 1)
                    throw new InvalidOperationException("Móng Bè cần capture đúng một Foundation semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == ElementCategory.Foundation &&
                    x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null)
                    throw new InvalidOperationException("Không tìm thấy Foundation semantic element vừa tạo cho source clone " + sourceHandle + ".");

                var createdElementId = createdElement.Id;
                var thicknessM = FamilyNumber(project, ElementCategory.Foundation, "ThicknessM", 0.5d);
                var bottomOffsetM = FamilyFiniteNumber(project, ElementCategory.Foundation, "BottomOffsetM", 0d);
                createdElement.SetProperty("ThicknessM", thicknessM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BottomOffsetM", bottomOffsetM.ToString("R", CultureInfo.InvariantCulture));

                document.Editor.WriteMessage(
                    "\nQS3D Móng Bè: dùng exact closed boundary + Family Foundation hiện tại (dày " +
                    thicknessM.ToString("0.###", CultureInfo.InvariantCulture) + " m, offset " +
                    bottomOffsetM.ToString("0.###", CultureInfo.InvariantCulture) + " m).");

                EnsureActive(document, operation + " / QS3DBUILD3D");
                document.Editor.SetImpliedSelection(new[] { sourceId });
                new Build3DCommands().Build3D();
                EnsureActive(document, operation + " / post QS3DBUILD3D");

                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException("Foundation Móng Bè không còn tồn tại sau QS3DBUILD3D; operation được rollback.");

                if (!createdElement.Properties.TryGetValue("GeneratedSolidHandle", out generatedHandle) || string.IsNullOrWhiteSpace(generatedHandle))
                    throw new InvalidOperationException("QS3DBUILD3D không ghi GeneratedSolidHandle cho Foundation Móng Bè.");
                var liveGenerated = CadHandleService.GetLiveHandles(document, new[] { generatedHandle });
                if (!liveGenerated.Contains(generatedHandle, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated solid Móng Bè không còn live sau QS3DBUILD3D: " + generatedHandle + ".");

                project.Touch();
            }
            catch (Exception operationError)
            {
                var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Exception? ownershipDiscoveryError = null;
                if (createdElement != null)
                {
                    foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement))
                        if (!string.IsNullOrWhiteSpace(entry.Key)) generatedHandles.Add(entry.Key.Trim());
                    try
                    {
                        foreach (var handle in GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, createdElement.Id, createdElement.Category))
                            if (!string.IsNullOrWhiteSpace(handle)) generatedHandles.Add(handle.Trim());
                    }
                    catch (Exception ex) { ownershipDiscoveryError = ex; }
                }

                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseOwnedCad(document, project, createdElement, sourceId, generatedHandles); }
                catch (Exception ex) { cleanupError = ex; }
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (ownershipDiscoveryError != null || cleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (ownershipDiscoveryError != null) errors.Add(ownershipDiscoveryError);
                    if (cleanupError != null) errors.Add(cleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException("Móng Bè thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, createdElement!, sourceId, generatedHandle);
        }

        private static ObjectId CreateExactWcsPolyline(Document document, IReadOnlyList<Point3d> points)
        {
            RequireSingleElevation(points, "Móng Bè source clone");
            RequireNonDegenerateEdges(points, "Móng Bè source clone");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var polyline = new Polyline();
                polyline.SetDatabaseDefaults(document.Database);
                polyline.Elevation = CadGeometryGuard.Finite(points[0].Z, "Móng Bè source elevation");
                for (var index = 0; index < points.Count; index++)
                {
                    var x = CadGeometryGuard.Finite(points[index].X, "Móng Bè source X[" + index + "]");
                    var y = CadGeometryGuard.Finite(points[index].Y, "Móng Bè source Y[" + index + "]");
                    polyline.AddVertexAt(index, new Point2d(x, y), 0d, 0d, 0d);
                }
                polyline.Closed = true;
                var id = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                transaction.Commit();
                return id;
            }
        }

        private static void EraseOwnedCad(Document document, ProjectState project, ProjectElement? createdElement, ObjectId sourceId, IEnumerable<string> generatedHandles)
        {
            var normalized = new HashSet<string>(
                (generatedHandles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count > 0 && createdElement == null)
                throw new InvalidOperationException("Móng Bè rollback found generated CAD without the newly-created semantic owner.");
            var ids = CadHandleService.Resolve(document, normalized);

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                if (!sourceId.IsNull && sourceId.IsValid)
                {
                    var source = transaction.GetObject(sourceId, OpenMode.ForWrite, true) as Entity;
                    if (source != null && !source.IsErased) source.Erase(true);
                }

                foreach (var id in ids)
                {
                    if (id.IsNull || !id.IsValid || id == sourceId) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                    if (entity == null)
                        throw new InvalidOperationException("Móng Bè rollback generated handle " + id.Handle + " không còn trỏ tới Entity hợp lệ.");
                    if (entity.IsErased) continue;
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, createdElement!, "rollback Móng Bè generated CAD " + id.Handle);
                    entity.Erase(true);
                }
                transaction.Commit();
            }

            var remainingGenerated = CadHandleService.GetLiveHandles(document, normalized);
            if (remainingGenerated.Count > 0)
                throw new InvalidOperationException("Móng Bè rollback còn generated CAD handle chưa xóa: " + string.Join(", ", remainingGenerated.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
            if (!sourceId.IsNull && sourceId.IsValid)
            {
                var remainingSource = CadHandleService.GetLiveHandles(document, new[] { sourceId.Handle.ToString() });
                if (remainingSource.Count > 0)
                    throw new InvalidOperationException("Móng Bè rollback còn source clone chưa xóa: " + sourceId.Handle + ".");
            }
        }

        private static void FinalizeUi(Document document, ProjectElement element, ObjectId sourceId, string generatedHandle)
        {
            const string status = "Móng Bè: exact boundary + semantic + native 3D hoàn tất.";
            try
            {
                PaletteCoordinator.RefreshProject();
                if (!string.IsNullOrWhiteSpace(generatedHandle)) CadHandleService.Select(document, new[] { generatedHandle });
                else if (!sourceId.IsNull && sourceId.IsValid) document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D " + status + " UI sync warning: " + ex.Message); }
                catch { }
            }
        }

        private static double FamilyNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var value = FamilyFiniteNumber(project, category, key, fallback);
            if (!(value > 0d))
                throw new InvalidOperationException("Family " + category + "/" + key + " phải là số hữu hạn > 0 trước khi tạo Móng Bè.");
            return value;
        }

        private static double FamilyFiniteNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var family = PreferredFamily(project, category);
            if (family == null || !family.Properties.TryGetValue(key, out var raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Family '" + family.Name + "' (" + category + ") có " + key + " không hợp lệ: '" + (raw ?? string.Empty) + "'. Sửa Family trước khi tạo Móng Bè.");
            return value;
        }

        private static ProjectFamily? PreferredFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == category);
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (!document.Database.CurrentSpaceId.Equals(blockTable[BlockTableRecord.ModelSpace]))
                    throw new InvalidOperationException("Móng Bè hiện chỉ hỗ trợ Model Space. Chuyển sang tab Model trước khi tạo.");
                transaction.Commit();
            }
        }

        private static void RequireSingleElevation(IReadOnlyList<Point3d> points, string label)
        {
            if (points == null || points.Count < 3)
                throw new InvalidOperationException(label + " cần ít nhất 3 đỉnh.");
            var z = points[0].Z;
            if (double.IsNaN(z) || double.IsInfinity(z))
                throw new InvalidOperationException(label + " có Z không hữu hạn.");
            for (var index = 1; index < points.Count; index++)
            {
                if (Math.Abs(points[index].Z - z) > GeometryTolerance)
                    throw new InvalidOperationException(label + " phải nằm chính xác trên một mặt phẳng WCS XY; boundary nghiêng/3D không được phép suy đoán.");
            }
        }

        private static void RequireNonDegenerateEdges(IReadOnlyList<Point3d> points, string label)
        {
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                if (SamePoint(points[index], points[next]))
                    throw new InvalidOperationException(label + " chứa hai đỉnh liên tiếp trùng nhau.");
            }
        }

        private static void RequireFinite(Point3d point, string label)
        {
            if (double.IsNaN(point.X) || double.IsInfinity(point.X) ||
                double.IsNaN(point.Y) || double.IsInfinity(point.Y) ||
                double.IsNaN(point.Z) || double.IsInfinity(point.Z))
                throw new InvalidOperationException(label + " chứa tọa độ không hữu hạn.");
        }

        private static bool SamePoint(Point3d left, Point3d right)
        {
            return left.DistanceTo(right) <= GeometryTolerance;
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " yêu cầu đúng DWG đã bắt đầu lệnh vẫn là bản vẽ active.");
        }

        private sealed class BoundarySegment
        {
            public BoundarySegment(Point3d start, Point3d end)
            {
                Start = start;
                End = end;
            }

            public Point3d Start { get; }
            public Point3d End { get; }
        }
    }
}
