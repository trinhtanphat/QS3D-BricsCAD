using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Fast 2D-plan -> QS3D wall conversion for imported/legacy BricsCAD plans.
    /// The original LINE/open POLYLINE remains the semantic source; QS3D owns only
    /// the generated Solid3d. A fresh batch uses one shared wall style for all sources.
    /// </summary>
    public sealed class PlanTo3DCommands
    {
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        private enum SourceKind
        {
            Line,
            OpenPolyline
        }

        private sealed class SourceCandidate
        {
            public ObjectId Id { get; set; }
            public string Handle { get; set; } = string.Empty;
            public SourceKind Kind { get; set; }
            public string GeometryFingerprint { get; set; } = string.Empty;
        }

        [CommandMethod("QS3DCONVERT2D", CommandFlags.Modal)]
        public void Convert2D() => ConvertPlanWalls("QS3DCONVERT2D", promptStyle: false);

        [CommandMethod("QS3DPLAN2WALLS", CommandFlags.Modal)]
        public void PlanToWalls() => ConvertPlanWalls("QS3DPLAN2WALLS", promptStyle: false);

        [CommandMethod("QS3DCONVERT2DADV", CommandFlags.Modal)]
        public void Convert2DAdvanced() => ConvertPlanWalls("QS3DCONVERT2DADV", promptStyle: true);

        private static void ConvertPlanWalls(string operation, bool promptStyle)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            Guard(document, operation, () =>
            {
                EnsureActive(document, operation);
                RequireModelSpace(document);

                var selectedIds = AcquireSelection(document);
                if (selectedIds == null || selectedIds.Count == 0) return;
                var sources = PreflightSources(document, selectedIds);
                if (sources.Count == 0) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                if (defaultsProject != null) RequireFreshSources(defaultsProject, sources);

                var defaultThicknessM = defaultsProject != null ? FamilyNumber(defaultsProject, "ThicknessM", 0.2d) : 0.2d;
                var defaultHeightM = defaultsProject != null ? FamilyNumber(defaultsProject, "HeightM", 3.0d) : 3.0d;
                var defaultBottomOffsetM = defaultsProject != null ? FamilyFiniteNumber(defaultsProject, "BottomOffsetM", 0d) : 0d;

                double? thicknessM = promptStyle
                    ? PromptPositiveMeters(document.Editor, "Bề dày Tường cho toàn bộ selection (m)", defaultThicknessM)
                    : defaultThicknessM;
                if (!thicknessM.HasValue) return;

                double? heightM = promptStyle
                    ? PromptPositiveMeters(document.Editor, "Chiều cao Tường cho toàn bộ selection (m)", defaultHeightM)
                    : defaultHeightM;
                if (!heightM.HasValue) return;

                double? bottomOffsetM = promptStyle
                    ? PromptFiniteMeters(document.Editor, "Offset đáy Tường so với Z source (m)", defaultBottomOffsetM)
                    : defaultBottomOffsetM;
                if (!bottomOffsetM.HasValue) return;

                EnsureActive(document, operation);
                RequireModelSpace(document);

                var refreshedSources = PreflightSources(document, selectedIds);
                RequireSameSources(sources, refreshedSources);
                sources = refreshedSources;

                var project = projectPreview.ResolveForMutation(document, operation);
                RequireFreshSources(project, sources);
                var rollback = ProjectStateSnapshot.Capture(project);
                var createdElements = new List<ProjectElement>();
                var regenerated = 0;
                var solids = 0;

                try
                {
                    var regenerator = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
                    foreach (var source in sources)
                    {
                        EnsureActive(document, operation);
                        document.Editor.SetImpliedSelection(new[] { source.Id });

                        var captured = SemanticCaptureService.Capture(document, ElementCategory.ArchitecturalWall);
                        if (captured != 1)
                            throw new InvalidOperationException(
                                "2D -> 3D cần capture đúng một wall cho source " + source.Handle + ", nhận được " + captured + ".");

                        var element = project.Elements.SingleOrDefault(x =>
                            x.Category == ElementCategory.ArchitecturalWall &&
                            x.SourceHandles.Any(h => string.Equals(h, source.Handle, StringComparison.OrdinalIgnoreCase)));
                        if (element == null)
                            throw new InvalidOperationException("Không tìm thấy QS3D wall vừa capture cho source " + source.Handle + ".");

                        createdElements.Add(element);
                        element.Properties["ThicknessM"] = thicknessM.Value.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["HeightM"] = heightM.Value.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["BottomOffsetM"] = bottomOffsetM.Value.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["QS3D.PlanTo3D"] = "1";
                        element.MarkDirty(ElementDirtyFlags.Properties);

                        regenerated += regenerator.RegenerateDirtySubset(project, new[] { element.Id });

                        var built = source.Kind == SourceKind.Line
                            ? WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall)
                            : PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.ArchitecturalWall);
                        if (built != 1)
                            throw new InvalidOperationException(
                                "Native 3D builder cần tạo đúng một Solid3d cho source " + source.Handle + ", nhận được " + built + ".");
                        solids += built;
                    }

                    project.Touch();
                }
                catch (Exception operationError)
                {
                    RollbackBatch(document, project, rollback, createdElements, operationError);
                    throw;
                }

                FinalizeUi(document, createdElements, sources.Count, solids, regenerated);
            });
        }

        private static IReadOnlyList<ObjectId>? AcquireSelection(Document document)
        {
            var implied = document.Editor.SelectImplied();
            if (implied.Status == PromptStatus.OK && implied.Value != null)
            {
                var ids = implied.Value.GetObjectIds();
                if (ids.Length > 0) return ids.Distinct().ToList().AsReadOnly();
            }

            document.Editor.WriteMessage("\nChọn LINE hoặc open POLYLINE của mặt bằng 2D cần chuyển thành tường 3D: ");
            var selection = document.Editor.GetSelection();
            if (selection.Status == PromptStatus.Cancel) return null;
            if (selection.Status != PromptStatus.OK || selection.Value == null) return null;
            return selection.Value.GetObjectIds().Distinct().ToList().AsReadOnly();
        }

        private static IReadOnlyList<SourceCandidate> PreflightSources(Document document, IReadOnlyList<ObjectId> ids)
        {
            var result = new List<SourceCandidate>(ids.Count);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];

                foreach (var id in ids)
                {
                    if (id.IsNull || !id.IsValid)
                        throw new InvalidOperationException("Selection chứa CAD object không còn hợp lệ.");

                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Selection chứa entity không còn hợp lệ.");
                    if (!entity.OwnerId.Equals(modelSpaceId))
                        throw new InvalidOperationException("QS3DCONVERT2D chỉ nhận source ở Model Space: " + entity.Handle + ".");

                    if (entity is Line line)
                    {
                        var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, "2D plan LINE/dx");
                        var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, "2D plan LINE/dy");
                        var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, "2D plan LINE/dz");
                        if (CadGeometryGuard.Hypot(dx, dy, "2D plan LINE/length") <= 1e-6d)
                            throw new InvalidOperationException("LINE quá ngắn để chuyển thành wall: " + line.Handle + ".");
                        var deltaM = Math.Abs(CadGeometryGuard.ToMeters(document, dz, "2D plan LINE/delta Z"));
                        if (deltaM > PlanarityToleranceM)
                            throw new InvalidOperationException("LINE wall phải gần ngang |ΔZ| <= 0.005 m: " + line.Handle + ".");
                        result.Add(new SourceCandidate
                        {
                            Id = id,
                            Handle = line.Handle.ToString(),
                            Kind = SourceKind.Line,
                            GeometryFingerprint = BuildLineGeometryFingerprint(line)
                        });
                        continue;
                    }

                    if (entity is Polyline polyline)
                    {
                        if (polyline.Closed)
                            throw new InvalidOperationException(
                                "Closed POLYLINE không phải wall centerline. Hãy BREAK/tách thành open POLYLINE hoặc LINE trước khi QS3DCONVERT2D: " + polyline.Handle + ".");
                        if (polyline.NumberOfVertices < 2)
                            throw new InvalidOperationException("Open POLYLINE cần ít nhất 2 đỉnh: " + polyline.Handle + ".");
                        RequireWorldPlanNormal(polyline);
                        result.Add(new SourceCandidate
                        {
                            Id = id,
                            Handle = polyline.Handle.ToString(),
                            Kind = SourceKind.OpenPolyline,
                            GeometryFingerprint = BuildOpenPolylineGeometryFingerprint(polyline)
                        });
                        continue;
                    }

                    throw new InvalidOperationException(
                        "QS3DCONVERT2D chỉ hỗ trợ LINE/open POLYLINE; nhận " + entity.GetType().Name + " (" + entity.Handle + ").");
                }
                transaction.Commit();
            }

            return result.AsReadOnly();
        }

        private static void RequireSameSources(IReadOnlyList<SourceCandidate> before, IReadOnlyList<SourceCandidate> after)
        {
            if (before.Count != after.Count)
                throw new InvalidOperationException("Selection 2D -> 3D đã thay đổi trong lúc xác nhận. Hãy chạy lại lệnh.");

            for (var index = 0; index < before.Count; index++)
            {
                var left = before[index];
                var right = after[index];
                if (!left.Id.Equals(right.Id) ||
                    left.Kind != right.Kind ||
                    !string.Equals(left.Handle, right.Handle, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(left.GeometryFingerprint) ||
                    string.IsNullOrWhiteSpace(right.GeometryFingerprint) ||
                    !string.Equals(left.GeometryFingerprint, right.GeometryFingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("Source 2D -> 3D đã thay đổi trong lúc xác nhận. Hãy chạy lại lệnh.");
            }
        }

        private static string BuildLineGeometryFingerprint(Line line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            var start = line.StartPoint;
            var end = line.EndPoint;
            var normal = line.Normal;
            var canonical = new StringBuilder("QS3D_PLAN_SOURCE_V1|kind=LINE|start=");
            AppendPoint3d(canonical, start.X, start.Y, start.Z, "LINE start");
            canonical.Append("|end=");
            AppendPoint3d(canonical, end.X, end.Y, end.Z, "LINE end");
            canonical.Append("|normal=");
            AppendPoint3d(canonical, normal.X, normal.Y, normal.Z, "LINE normal");
            canonical.Append("|thickness=").Append(CanonicalGeometryNumber(line.Thickness, "LINE thickness"));
            return HashGeometrySnapshot(canonical);
        }

        private static string BuildOpenPolylineGeometryFingerprint(Polyline polyline)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            var normal = polyline.Normal;
            var canonical = new StringBuilder("QS3D_PLAN_SOURCE_V1|kind=OPEN_POLYLINE|closed=")
                .Append(polyline.Closed ? "1" : "0")
                .Append("|elevation=").Append(CanonicalGeometryNumber(polyline.Elevation, "POLYLINE elevation"))
                .Append("|normal=");
            AppendPoint3d(canonical, normal.X, normal.Y, normal.Z, "POLYLINE normal");
            canonical.Append("|vertices=").Append(polyline.NumberOfVertices.ToString(CultureInfo.InvariantCulture));

            for (var index = 0; index < polyline.NumberOfVertices; index++)
            {
                var point = polyline.GetPoint2dAt(index);
                var bulge = polyline.GetBulgeAt(index);
                canonical.Append('|').Append(index.ToString(CultureInfo.InvariantCulture)).Append(':')
                    .Append(CanonicalGeometryNumber(point.X, "POLYLINE vertex X")).Append(',')
                    .Append(CanonicalGeometryNumber(point.Y, "POLYLINE vertex Y")).Append(',')
                    .Append(CanonicalGeometryNumber(bulge, "POLYLINE bulge"));
            }

            return HashGeometrySnapshot(canonical);
        }

        private static void AppendPoint3d(StringBuilder canonical, double x, double y, double z, string label)
        {
            canonical.Append(CanonicalGeometryNumber(x, label + " X")).Append(',')
                .Append(CanonicalGeometryNumber(y, label + " Y")).Append(',')
                .Append(CanonicalGeometryNumber(z, label + " Z"));
        }

        private static string CanonicalGeometryNumber(double value, string label) =>
            CadGeometryGuard.Finite(value, label).ToString("R", CultureInfo.InvariantCulture);

        private static string HashGeometrySnapshot(StringBuilder canonical)
        {
            if (canonical == null) throw new ArgumentNullException(nameof(canonical));
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                var output = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) output.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static void RequireWorldPlanNormal(Polyline polyline)
        {
            var normal = polyline.Normal;
            var length = normal.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("POLYLINE có normal không hợp lệ: " + polyline.Handle + ".");
            var x = normal.X / length;
            var y = normal.Y / length;
            var z = normal.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("POLYLINE wall phải nằm trên mặt phẳng song song WCS XY: " + polyline.Handle + ".");
        }

        private static void RequireFreshSources(ProjectState project, IReadOnlyList<SourceCandidate> sources)
        {
            foreach (var source in sources)
            {
                var owner = project.Elements.FirstOrDefault(x =>
                    x.SourceHandles.Any(h => string.Equals(h, source.Handle, StringComparison.OrdinalIgnoreCase)));
                if (owner != null)
                    throw new InvalidOperationException(
                        "Source " + source.Handle + " đã thuộc QS3D element " + owner.Id + ". QS3DCONVERT2D chỉ dành cho CAD 2D chưa capture; dùng QS3DSETWALL/QS3DREFRESH cho source đã có semantic.");

                if (GeneratedHandleOwnershipPolicy.TryFindOwner(project, source.Handle, out var generatedOwner, out var generatedSlot))
                    throw new InvalidOperationException(
                        "Source " + source.Handle + " là generated CAD của " + generatedOwner!.Id + " (" + generatedSlot + "), không thể convert ngược thành semantic source.");
            }
        }

        private static void RollbackBatch(
            Document document,
            ProjectState project,
            ProjectStateSnapshot rollback,
            IReadOnlyList<ProjectElement> createdElements,
            Exception operationError)
        {
            var generatedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Exception? ownershipDiscoveryError = null;

            foreach (var element in createdElements.Distinct())
                foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element))
                    if (!string.IsNullOrWhiteSpace(entry.Key)) generatedHandles.Add(entry.Key.Trim());

            try
            {
                foreach (var element in createdElements.Distinct())
                    foreach (var handle in GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, element.Id, element.Category))
                        if (!string.IsNullOrWhiteSpace(handle)) generatedHandles.Add(handle.Trim());
            }
            catch (Exception ex)
            {
                ownershipDiscoveryError = ex;
            }

            Exception? cadCleanupError = null;
            Exception? restoreError = null;
            try { EraseGeneratedBatch(document, project, createdElements, generatedHandles); }
            catch (Exception ex) { cadCleanupError = ex; }
            try { rollback.Restore(project); }
            catch (Exception ex) { restoreError = ex; }
            try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
            catch { }

            if (ownershipDiscoveryError != null || cadCleanupError != null || restoreError != null)
            {
                var errors = new List<Exception> { operationError };
                if (ownershipDiscoveryError != null) errors.Add(ownershipDiscoveryError);
                if (cadCleanupError != null) errors.Add(cadCleanupError);
                if (restoreError != null) errors.Add(restoreError);
                throw new InvalidOperationException(
                    "2D -> 3D batch thất bại và rollback không hoàn tất đầy đủ.",
                    new AggregateException(errors));
            }
        }

        private static void EraseGeneratedBatch(
            Document document,
            ProjectState project,
            IReadOnlyList<ProjectElement> createdElements,
            IEnumerable<string> generatedHandles)
        {
            var normalized = new HashSet<string>(
                (generatedHandles ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (normalized.Count == 0) return;

            var ids = CadHandleService.Resolve(document, normalized);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    if (id.IsNull || !id.IsValid) continue;
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var owner = createdElements.FirstOrDefault(element =>
                        GeneratedGeometryService.HasMatchingOwnership(entity, project, element));
                    if (owner == null)
                        throw new InvalidOperationException(
                            "Rollback từ chối xóa generated handle " + id.Handle + " vì ownership không thuộc batch hiện tại.");
                    entity.Erase(true);
                }
                transaction.Commit();
            }

            var remaining = CadHandleService.GetLiveHandles(document, normalized);
            if (remaining.Count > 0)
                throw new InvalidOperationException(
                    "Rollback còn generated CAD chưa xóa: " + string.Join(", ", remaining.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) + ".");
        }

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(label + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double? PromptFiniteMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = true,
                AllowZero = true,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " phải là số hữu hạn.");
            return value;
        }

        private static double FamilyNumber(ProjectState project, string key, double fallback)
        {
            var value = FamilyFiniteNumber(project, key, fallback);
            if (!(value > 0d))
                throw new InvalidOperationException("Family ArchitecturalWall/" + key + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double FamilyFiniteNumber(ProjectState project, string key, double fallback)
        {
            var family = PreferredWallFamily(project);
            if (family == null || !family.Properties.TryGetValue(key, out var raw)) return fallback;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(
                    "Family '" + family.Name + "'/" + key + " không hợp lệ: '" + (raw ?? string.Empty) + "'.");
            return value;
        }

        private static ProjectFamily? PreferredWallFamily(ProjectState project)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == ElementCategory.ArchitecturalWall) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == ElementCategory.ArchitecturalWall);
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("QS3DCONVERT2D hiện chỉ hỗ trợ Model Space.");
                transaction.Commit();
            }

            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("QS3DCONVERT2D yêu cầu UCS có XY song song WCS XY; UCS nghiêng/3D chưa được hỗ trợ.");
        }

        private static void FinalizeUi(
            Document document,
            IReadOnlyList<ProjectElement> elements,
            int sourceCount,
            int solids,
            int regenerated)
        {
            var handles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
                foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element))
                    if (!string.IsNullOrWhiteSpace(entry.Key)) handles.Add(entry.Key.Trim());

            var status = "2D -> 3D: " + sourceCount + " wall source • " + solids + " solid • regenerate " + regenerated + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                CadHandleService.SelectIfAny(document, handles);
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D " + status + " UI sync warning: " + ex.Message); }
                catch { }
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " yêu cầu đúng DWG đã bắt đầu lệnh vẫn là bản vẽ active.");
        }

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); }
                catch { }
                try { PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); }
                catch { }
            }
        }
    }
}
