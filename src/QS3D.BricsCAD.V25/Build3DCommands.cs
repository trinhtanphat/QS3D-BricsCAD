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
                    Write(document, "QS3DBUILD3D: chưa có CAD reference. Chọn LINE/open POLYLINE hoặc source đã capture rồi chạy lại.");
                    return;
                }

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var selectedElements = project.Elements
                    .Where(x => x.SourceHandles.Any(handles.Contains))
                    .ToList();

                if (selectedElements.Count == 0)
                {
                    Write(document, "QS3DBUILD3D: selection chưa được capture semantic. Chạy QS3DWALL/QS3DBEAM/... trước rồi Vẽ/Cập nhật 3D.");
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

                var category = categories[0];
                if (!ValidateWallSourceBatch(selectedElements, snapshots, category, out var wallSourceError))
                {
                    Write(document, wallSourceError);
                    return;
                }

                // Semantic validation/regeneration can fail on rules/dependencies. Run it before
                // committing any replacement Solid3d so those blockers cannot leave a partial CAD rebuild.
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                var built = BuildCategory(document, project, category);
                if (built <= 0)
                    throw new InvalidOperationException("Không tạo được solid từ source đang chọn. Tường KT cần LINE hoặc open POLYLINE; các cấu kiện khác phải đúng source profile được builder hỗ trợ.");

                project.Touch();
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();

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

        private static int BuildCategory(Document document, ProjectState project, ElementCategory category)
        {
            if (IsWallCategory(category))
            {
                var count = WallSolidBuilder.BuildSelectedLineWalls(document, project, category);
                return count + PolylineWallSolidBuilder.BuildSelected(document, project, category);
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
