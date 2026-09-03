using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Stable entry points for the common BLT-style workflow:
    /// choose a Family/Type once, then use either the primary Quick command or the existing
    /// prompt-bearing Advanced command for that Family. The dispatcher is read-only/non-creating;
    /// target commands retain ownership of source/semantic/native lifecycle and cancellation.
    /// </summary>
    public sealed class ActiveFamilyQuickDrawCommands
    {
        [CommandMethod("QS3DDRAWACTIVE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawActiveFamily() => DrawActiveFamilyCore(advanced: false, operation: "QS3DDRAWACTIVE");

        [CommandMethod("QS3DDRAWACTIVEADV", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawActiveFamilyAdvanced() => DrawActiveFamilyCore(advanced: true, operation: "QS3DDRAWACTIVEADV");

        [CommandMethod("QS3DDRAWACTIVEREPEAT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawActiveFamilyRepeated() =>
            DrawActiveFamilyCore(advanced: false, operation: "QS3DDRAWACTIVEREPEAT", repeated: true);

        internal static bool SupportsFamily(ProjectFamily family)
        {
            if (family == null) return false;
            if (SlabOpeningContract.IsSlabOpenFamily(family)) return true;
            switch (family.Category)
            {
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.Beam:
                case ElementCategory.Column:
                case ElementCategory.Slab:
                case ElementCategory.GlassWall:
                case ElementCategory.WallPier:
                case ElementCategory.StructuralWall:
                case ElementCategory.Foundation:
                case ElementCategory.Door:
                case ElementCategory.WallOpening:
                    return true;
                default:
                    return false;
            }
        }

        private static void DrawActiveFamilyCore(bool advanced, string operation, bool repeated = false)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, operation + ": bản vẽ chưa có QS3D project. Mở Workspace, tạo/chọn Family trước khi vẽ.");
                    return;
                }

                var family = ProjectFamilyActivationService.GetActive(project);
                if (family == null)
                {
                    Report(document, operation + ": chưa có Family active. Chọn Family/Type trong Workspace rồi chạy lại.");
                    return;
                }

                var expectedProjectId = project.ProjectId;
                var expectedChangeVersion = project.ChangeVersion;
                var expectedFamilyId = family.Id;
                var expectedCategory = family.Category;
                var expectedWindowRouting = family.Category == ElementCategory.WallOpening && IsWindowFamily(family);
                var expectedSlabOpenRouting = SlabOpeningContract.IsSlabOpenFamily(family);

                var dispatchFamily = RequireCurrentDispatchSnapshot(
                    document,
                    expectedProjectId,
                    expectedChangeVersion,
                    expectedFamilyId,
                    expectedCategory,
                    expectedWindowRouting,
                    expectedSlabOpenRouting,
                    operation);

                if (repeated)
                {
                    DispatchRepeated(document, dispatchFamily, expectedProjectId, expectedFamilyId, operation);
                }
                else
                {
                    using (DirectDrawProjectPreviewContext.BeginDispatchScope(document))
                        Dispatch(document, dispatchFamily, advanced, operation);
                }
            }
            catch (Exception)
            {
                Report(document, operation + ": không thể hoàn tất thao tác. Vui lòng thử lại.");
            }
        }

        private static ProjectFamily RequireCurrentDispatchSnapshot(
            Document document,
            string expectedProjectId,
            long expectedChangeVersion,
            string expectedFamilyId,
            ElementCategory expectedCategory,
            bool expectedWindowRouting,
            bool expectedSlabOpenRouting,
            string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(
                    operation + ": DWG active đã thay đổi trước khi dispatch. Hãy chạy lại lệnh trên bản vẽ hiện hành.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                throw new InvalidOperationException(
                    operation + ": QS3D project không còn khả dụng trước khi dispatch. Hãy Refresh Workspace rồi chạy lại.");

            if (!string.Equals(currentProject.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                currentProject.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException(
                    operation + ": QS3D project đã thay đổi sau khi đọc Active Family. Hãy chạy lại để dùng đúng Family hiện hành.");

            var currentFamily = ProjectFamilyActivationService.GetActive(currentProject);
            if (currentFamily == null)
                throw new InvalidOperationException(
                    operation + ": Active Family đã bị xóa/bỏ chọn trước khi dispatch. Hãy chọn lại Family/Type.");

            var currentWindowRouting = currentFamily.Category == ElementCategory.WallOpening && IsWindowFamily(currentFamily);
            var currentSlabOpenRouting = SlabOpeningContract.IsSlabOpenFamily(currentFamily);
            var routingChanged =
                !string.Equals(currentFamily.Id, expectedFamilyId, StringComparison.OrdinalIgnoreCase) ||
                currentFamily.Category != expectedCategory ||
                currentWindowRouting != expectedWindowRouting ||
                currentSlabOpenRouting != expectedSlabOpenRouting;
            if (routingChanged)
                throw new InvalidOperationException(
                    operation + ": Active Family/routing đã thay đổi trước khi dispatch. Hãy chạy lại lệnh.");

            return currentFamily;
        }

        private static void Dispatch(Document document, ProjectFamily family, bool advanced, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (family == null) throw new ArgumentNullException(nameof(family));
            if (!SupportsFamily(family))
            {
                Report(
                    document,
                    operation + ": Family '" + family.Name + "' thuộc " + family.Category +
                    " chưa có Direct Draw " + (advanced ? "Advanced" : "Quick") +
                    " an toàn. Dùng workflow chuyên biệt hiện có cho category này.");
                return;
            }

            if (SlabOpeningContract.IsSlabOpenFamily(family))
            {
                if (advanced) new DirectDrawSlabOpeningCommands().DrawSlabOpeningAdvanced();
                else new DirectDrawSlabOpeningCommands().DrawSlabOpening();
                return;
            }

            switch (family.Category)
            {
                case ElementCategory.ArchitecturalWall:
                    if (advanced) new DirectDrawCommands().DrawWallAdvanced();
                    else new DirectDrawCommands().DrawWall();
                    return;
                case ElementCategory.Beam:
                    if (advanced) new DirectDrawCommands().DrawBeamAdvanced();
                    else new DirectDrawCommands().DrawBeam();
                    return;
                case ElementCategory.Column:
                    if (advanced) new DirectDrawCommands().DrawColumnAdvanced();
                    else new DirectDrawCommands().DrawColumn();
                    return;
                case ElementCategory.Slab:
                    if (advanced) new DirectDrawCommands().DrawSlabAdvanced();
                    else new DirectDrawCommands().DrawSlab();
                    return;
                case ElementCategory.GlassWall:
                    if (advanced) new DirectDrawP1Commands().DrawGlassWallAdvanced();
                    else new DirectDrawP1Commands().DrawGlassWall();
                    return;
                case ElementCategory.WallPier:
                    if (advanced) new DirectDrawP1Commands().DrawWallPierAdvanced();
                    else new DirectDrawP1Commands().DrawWallPier();
                    return;
                case ElementCategory.StructuralWall:
                    if (advanced) new DirectDrawP1Commands().DrawStructuralWallAdvanced();
                    else new DirectDrawP1Commands().DrawStructuralWall();
                    return;
                case ElementCategory.Foundation:
                    if (SingleFootingContract.IsSingleFooting(family))
                    {
                        new SingleFootingCommands().DrawSingleFooting();
                        return;
                    }
                    if (advanced) new DirectDrawP1Commands().DrawFoundationAdvanced();
                    else new DirectDrawP1Commands().DrawFoundation();
                    return;
                case ElementCategory.Door:
                    if (advanced) new DirectDrawOpeningCommands().DrawDoorAdvanced();
                    else new DirectDrawOpeningCommands().DrawDoor();
                    return;
                case ElementCategory.WallOpening:
                    if (IsWindowFamily(family))
                    {
                        if (advanced) new DirectDrawWindowCommands().DrawWindowAdvanced();
                        else new DirectDrawWindowCommands().DrawWindow();
                        return;
                    }
                    if (advanced) new DirectDrawOpeningCommands().DrawWallOpeningAdvanced();
                    else new DirectDrawOpeningCommands().DrawWallOpening();
                    return;
                default:
                    throw new InvalidOperationException("Direct Draw support predicate and dispatcher are inconsistent for " + family.Category + ".");
            }
        }

        private static void DispatchRepeated(
            Document document,
            ProjectFamily family,
            string expectedProjectId,
            string expectedFamilyId,
            string operation)
        {
            if (family.Category != ElementCategory.ArchitecturalWall &&
                family.Category != ElementCategory.Beam)
            {
                Report(
                    document,
                    operation + ": chế độ vẽ liên tục hiện chỉ hỗ trợ Family Tường KT hoặc Dầm. " +
                    "Dùng Quick/Advanced workflow hiện có cho " + family.Category + ".");
                return;
            }

            new DirectDrawRepeatedCommands().DrawActiveFamilyRepeated(
                family.Category,
                expectedProjectId,
                expectedFamilyId);
        }

        private static bool IsWindowFamily(ProjectFamily family)
        {
            if (family.Properties.TryGetValue("OpeningUsage", out var usage) && !string.IsNullOrWhiteSpace(usage))
                return string.Equals(usage.Trim(), "Window", StringComparison.OrdinalIgnoreCase);

            return family.Properties.ContainsKey("WindowHeightM") ||
                   family.Properties.ContainsKey("WindowSillHeightM");
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
