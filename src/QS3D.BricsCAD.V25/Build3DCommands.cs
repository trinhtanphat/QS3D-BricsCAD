using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Model;
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

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var selectedElements = project.Elements
                    .Where(x => SemanticReferenceHandles.MatchesSelection(x, handles))
                    .ToList();

                if (selectedElements.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: selection chưa thuộc cấu kiện semantic QS3D. Chạy QS3DWALL/QS3DBEAM/... trước rồi Vẽ/Cập nhật 3D.");
                    return;
                }

                var unsupported = selectedElements
                    .Where(x => !IsNativeBuildCategory(x.Category))
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

                document.Editor.SetImpliedSelection(sourceIds.ToArray());
                var sourceSnapshots = EntitySnapshotReader.ReadImpliedSelection(document);
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

                // Semantic validation/regeneration can fail on rules/dependencies. Run it before
                // committing any replacement Solid3d so those blockers cannot leave a partial CAD rebuild.
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var built = BuildCategory(document, project, category, sourceSnapshots);
                if (built <= 0)
                    throw new InvalidOperationException("Không tạo được solid từ source đang chọn. Tường KT cần LINE hoặc open POLYLINE; các cấu kiện khác phải đúng source profile được builder hỗ trợ.");

                project.Touch();
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();

                // Prefer selecting the newly generated result, like BLT. A subsequent QS3DBUILD3D
                // still resolves that generated selection back to the stable source handles above.
                var generatedHandles = selectedElements
                    .Select(x => x.Properties.TryGetValue("GeneratedSolidHandle", out var handle) ? handle : string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (generatedHandles.Count > 0) CadHandleService.Select(document, generatedHandles);
                else CadHandleService.Select(document, sourceHandles);

                var status = "Vẽ/Cập nhật 3D: " + built + " solid • " + selectedElements.Count + " semantic • " + category + " • regenerate " + regenerated + ".";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
                document.SendStringToExecute("QS3DVIEW3D ", true, false, false);
            }
            catch (Exception ex)
            {
                var message = "QS3DBUILD3D lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
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
            if (!IsWallCategory(category)) return true;

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
            if (sourceTypes.Count > 1)
            {
                error = "QS3DBUILD3D: không build chung LINE và open POLYLINE trong một lần vì hai builder có transaction riêng. Chọn một source type mỗi lần để giữ atomic/fail-closed.";
                return false;
            }
            return true;
        }

        private static int BuildCategory(
            Document document,
            ProjectState project,
            ElementCategory category,
            IReadOnlyCollection<EntitySnapshot> sourceSnapshots)
        {
            if (IsWallCategory(category))
            {
                var sourceType = sourceSnapshots
                    .Select(x => x.EntityType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .SingleOrDefault() ?? string.Empty;

                if (string.Equals(sourceType, "Line", StringComparison.OrdinalIgnoreCase))
                {
                    if (category == ElementCategory.WallPier)
                        return WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project);
                    return WallSolidBuilder.BuildSelectedLineWalls(document, project, category);
                }
                if (string.Equals(sourceType, "Polyline", StringComparison.OrdinalIgnoreCase))
                    return PolylineWallSolidBuilder.BuildSelected(document, project, category);

                throw new InvalidOperationException("Unsupported wall source type after validation: " + sourceType + ".");
            }

            return StructuralSolidBuilder.Supports(category)
                ? StructuralSolidBuilder.BuildSelected(document, project, category)
                : 0;
        }

        private static bool IsWallCategory(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        private static bool IsNativeBuildCategory(ElementCategory category) =>
            IsWallCategory(category) || StructuralSolidBuilder.Supports(category);

        private static void Write(Document document, string message)
        {
            PaletteCoordinator.SetStatus(message);
            document.Editor.WriteMessage("\nQS3D " + message);
        }
    }
}
