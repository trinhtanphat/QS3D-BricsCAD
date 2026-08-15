using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Builds/rebuilds native 3D geometry from already-captured QS3D source entities.
    /// This is the compatibility path used by the BLT-style Workspace flow:
    /// select reference -> capture semantic -> edit Family/Instance -> QS3DBUILD3D.
    /// </summary>
    public sealed class Build3DCommands
    {
        [CommandMethod("QS3DBUILD3D", CommandFlags.UsePickSet)]
        public void Build3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
#if DEBUG
                CadRuntimeGuard.EnsureSupportedV25();
#endif
                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: chưa có CAD reference. Chọn source hoặc solid QS3D đã tạo rồi chạy lại.");
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Build 3D");
                var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var selectedElements = project.Elements
                    .Where(x => SemanticReferenceHandles.MatchesSelection(x, handles))
                    .ToList();

                if (selectedElements.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: selection chưa thuộc cấu kiện semantic QS3D. Chạy QS3DWALL/QS3DBEAM/... trước rồi Vẽ/Cập nhật 3D.");
                    return;
                }

                var semanticSelectionAliases = new HashSet<string>(
                    selectedElements.SelectMany(SemanticReferenceHandles.GetSelectionAliases),
                    StringComparer.OrdinalIgnoreCase);
                var untrackedHandles = handles
                    .Where(x => !semanticSelectionAliases.Contains(x))
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (untrackedHandles.Count > 0)
                {
                    Write(document, "QS3DBUILD3D: selection có CAD object chưa thuộc source/boundary/generated-host của cấu kiện semantic (" +
                        string.Join(", ", untrackedHandles) + "). Đã dừng trước khi rebuild; capture hoặc bỏ các object này khỏi selection.");
                    return;
                }

                var unsupported = selectedElements
                    .Where(x => !NativeBuildCapability.Supports(x.Category))
                    .Select(x => x.Category)
                    .Distinct()
                    .OrderBy(x => x.ToString(), StringComparer.Ordinal)
                    .ToList();
                if (unsupported.Count > 0)
                {
                    Write(document, "QS3DBUILD3D: selection chứa category chưa hỗ trợ native 3D: " + string.Join(", ", unsupported) + ". Tách selection trước khi build.");
                    return;
                }

                var categories = selectedElements
                    .Select(x => x.Category)
                    .Distinct()
                    .ToList();

                if (categories.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: selection không có category hỗ trợ native 3D.");
                    return;
                }
                if (categories.Count > 1)
                {
                    Write(document, "QS3DBUILD3D: selection chứa nhiều category native (" + string.Join(", ", categories) + "). Chọn một category mỗi lần để giữ build atomic/fail-closed.");
                    return;
                }

                var sourceHandles = selectedElements
                    .SelectMany(x => x.SourceHandles)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (sourceHandles.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: cấu kiện semantic đang chọn không còn source handle để dựng lại 3D.");
                    return;
                }

                var sourceIds = CadHandleService.Resolve(document, sourceHandles);
                if (sourceIds.Count != sourceHandles.Count)
                {
                    Write(document, "QS3DBUILD3D: source CAD bị thiếu/stale (live " + sourceIds.Count + "/" + sourceHandles.Count + "). Không rebuild một phần; chạy Health/Locate và sửa source trước.");
                    return;
                }
                if (!AreAllModelSpaceEntities(document, sourceIds))
                {
                    Write(document, "QS3DBUILD3D: native generated geometry được quản lý trong Model Space; source đang nằm ngoài Model Space. Di chuyển/capture source trong Model Space trước khi rebuild.");
                    return;
                }

                var sourceSnapshots = EntitySnapshotReader.ReadHandles(document, sourceHandles);
                if (sourceSnapshots.Count != sourceHandles.Count)
                {
                    Write(document, "QS3DBUILD3D: không đọc đủ source CAD sau khi resolve semantic selection. Đã dừng trước khi thay solid.");
                    return;
                }

                var category = categories[0];
                if (!ValidateWallSourceBatch(selectedElements, sourceSnapshots, category, out var wallSourceError))
                {
                    Write(document, wallSourceError);
                    return;
                }

                var elementIds = selectedElements
                    .Select(x => x.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var regenerationScope = BuildRegenerationScope(project, selectedElements);
                var semanticRollback = ProjectStateSnapshot.Capture(project);
                var ownershipBefore = CaptureGeneratedSolidHandles(project, elementIds);
                int regenerated;
                int built;
                try
                {
                    // Semantic validation/regeneration can fail on rules/dependencies. Regenerate only
                    // the selected elements plus their transitive upstream dependencies before committing
                    // any replacement Solid3d. Unrelated dirty/downstream elements stay outside this build.
                    regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                        .RegenerateDirtySubset(project, regenerationScope);

                    var sourceType = NativeBuildCapability.IsWallCategory(category)
                        ? sourceSnapshots.Select(x => x.EntityType).Distinct(StringComparer.OrdinalIgnoreCase).Single()
                        : string.Empty;

                    // Native builders consume SelectImplied(). Handoff the already-validated live source IDs
                    // only at dispatch time so every preflight/regeneration failure leaves PICKFIRST untouched.
                    document.Editor.SetImpliedSelection(sourceIds.ToArray());
                    built = BuildCategory(document, project, category, sourceType);
                    if (built <= 0)
                        throw new InvalidOperationException("Không tạo được solid từ source đang chọn. Tường KT cần LINE hoặc open POLYLINE; các cấu kiện khác phải đúng source profile được builder hỗ trợ.");
                }
                catch (Exception operationError)
                {
                    if (GeneratedSolidHandlesMatch(project, ownershipBefore))
                    {
                        try
                        {
                            semanticRollback.Restore(project);
                        }
                        catch (Exception restoreError)
                        {
                            throw new InvalidOperationException(
                                "QS3DBUILD3D thất bại trước native ownership commit và semantic rollback cũng thất bại.",
                                new AggregateException(operationError, restoreError));
                        }
                        throw;
                    }

                    // A changed generated handle means a native builder may already have committed CAD +
                    // matching semantic ownership before a post-commit BricsCAD/UI operation failed.
                    // Rolling the project back here would create a worse CAD/semantic mismatch.
                    Report(document, "QS3DBUILD3D: native ownership đã thay đổi trước lỗi post-commit; giữ trạng thái đã commit để tránh lệch CAD/semantic. Chi tiết: " + operationError.Message);
                    return;
                }

                project.Touch();

                // At this point native CAD + semantic ownership already committed successfully.
                // Palette/selection/regen are convenience UI and must never turn a completed rebuild
                // into a false QS3DBUILD3D failure report or replace the user's current viewport.
                FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DBUILD3D lỗi: " + ex.Message);
            }
        }

        private static IReadOnlyList<string> BuildRegenerationScope(
            ProjectState project,
            IReadOnlyCollection<ProjectElement> selectedElements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedElements == null) throw new ArgumentNullException(nameof(selectedElements));

            var scope = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new Queue<ProjectElement>(selectedElements.Where(x => x != null));
            while (pending.Count > 0)
            {
                var element = pending.Dequeue();
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("QS3DBUILD3D: regeneration scope contains a semantic element with an empty ID.");
                if (!scope.Add(elementId)) continue;

                foreach (var rawDependencyId in element.DependsOn)
                {
                    var dependencyId = (rawDependencyId ?? string.Empty).Trim();
                    if (dependencyId.Length == 0)
                        throw new InvalidOperationException("QS3DBUILD3D: semantic element " + elementId + " contains an empty dependency ID.");
                    var dependency = project.FindElement(dependencyId);
                    if (dependency == null)
                        throw new InvalidOperationException(
                            "QS3DBUILD3D: semantic dependency " + dependencyId + " referenced by " + elementId + " is missing. Run Health/repair dependencies before native rebuild.");
                    pending.Enqueue(dependency);
                }
            }

            return scope
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        private static Dictionary<string, string> CaptureGeneratedSolidHandles(ProjectState project, IEnumerable<string> elementIds)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var elementId in elementIds)
            {
                var normalizedId = (elementId ?? string.Empty).Trim();
                if (normalizedId.Length == 0 || result.ContainsKey(normalizedId)) continue;
                var element = project.FindElement(normalizedId);
                result[normalizedId] = GeneratedSolidHandle(element);
            }
            return result;
        }

        private static bool GeneratedSolidHandlesMatch(ProjectState project, IReadOnlyDictionary<string, string> expected)
        {
            foreach (var pair in expected)
            {
                var element = project.FindElement(pair.Key);
                if (element == null) return false;
                if (!string.Equals(GeneratedSolidHandle(element), pair.Value, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private static string GeneratedSolidHandle(ProjectElement? element)
        {
            if (element == null) return string.Empty;
            return element.Properties.TryGetValue("GeneratedSolidHandle", out var handle)
                ? (handle ?? string.Empty).Trim()
                : string.Empty;
        }

        private static bool AreAllModelSpaceEntities(Document document, IReadOnlyCollection<ObjectId> ids)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased || !entity.OwnerId.Equals(modelSpaceId)) return false;
                }
                transaction.Commit();
            }
            return true;
        }

        private static bool ValidateWallSourceBatch(
            IReadOnlyCollection<ProjectElement> selectedElements,
            IReadOnlyCollection<EntitySnapshot> snapshots,
            ElementCategory category,
            out string error)
        {
            error = string.Empty;
            if (!NativeBuildCapability.IsWallCategory(category)) return true;

            var sourceHandles = new HashSet<string>(
                selectedElements.SelectMany(x => x.SourceHandles).Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);
            var sourceTypes = snapshots
                .Where(x => sourceHandles.Contains(x.Handle))
                .Select(x => x.EntityType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var unsupportedTypes = sourceTypes
                .Where(x => !string.Equals(x, "Line", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(x, "Polyline", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (unsupportedTypes.Count > 0)
            {
                error = "QS3DBUILD3D: Tường KT selection chứa source type chưa hỗ trợ native build: " + string.Join(", ", unsupportedTypes) + ".";
                return false;
            }
            if (sourceTypes.Count == 0)
            {
                error = "QS3DBUILD3D: không xác định được source type LINE/open POLYLINE cho wall selection. Đã dừng trước khi native build; chạy Health/Locate và kiểm tra source handle.";
                return false;
            }
            if (sourceTypes.Count > 1)
            {
                error = "QS3DBUILD3D: không build chung LINE và open POLYLINE trong một lần vì hai builder có transaction riêng. Chọn một source type mỗi lần để giữ atomic/fail-closed.";
                return false;
            }
            return true;
        }

        private static int BuildCategory(Document document, ProjectState project, ElementCategory category, string sourceType)
        {
            if (NativeBuildCapability.IsWallCategory(category))
            {
                if (string.Equals(sourceType, "Line", StringComparison.OrdinalIgnoreCase))
                {
                    return category == ElementCategory.WallPier
                        ? WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project)
                        : WallSolidBuilder.BuildSelectedLineWalls(document, project, category);
                }
                if (string.Equals(sourceType, "Polyline", StringComparison.OrdinalIgnoreCase))
                    return PolylineWallSolidBuilder.BuildSelected(document, project, category);
                return 0;
            }

            return StructuralSolidBuilder.Supports(category)
                ? StructuralSolidBuilder.BuildSelected(document, project, category)
                : 0;
        }

        private static void FinalizeUi(
            Document document,
            IReadOnlyCollection<string> elementIds,
            IReadOnlyCollection<string> sourceHandles,
            int built,
            int regenerated,
            ElementCategory category,
            ProjectState project)
        {
            var status = "Vẽ/Cập nhật 3D: " + built + " solid • " + elementIds.Count + " semantic • " + category + " • regenerate " + regenerated + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();

                // Resolve current project elements by id instead of retaining pre-build object references.
                // Builder rollback restores ProjectState from clones, so stale references must never be reused.
                var generatedHandles = elementIds
                    .Select(project.FindElement)
                    .Where(x => x != null)
                    .Select(GeneratedSolidHandle)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (generatedHandles.Count > 0) CadHandleService.Select(document, generatedHandles);
                else CadHandleService.Select(document, sourceHandles);

                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
            }
        }

        private static void Write(Document document, string message) => Report(document, message);

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
