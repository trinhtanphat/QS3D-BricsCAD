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
        [CommandMethod("QS3DDRAWACTIVE", CommandFlags.Modal)]
        public void DrawActiveFamily() => DrawActiveFamilyCore(advanced: false, operation: "QS3DDRAWACTIVE");

        [CommandMethod("QS3DDRAWACTIVEADV", CommandFlags.Modal)]
        public void DrawActiveFamilyAdvanced() => DrawActiveFamilyCore(advanced: true, operation: "QS3DDRAWACTIVEADV");

        private static void DrawActiveFamilyCore(bool advanced, string operation)
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

                Dispatch(document, family, advanced, operation);
            }
            catch (Exception ex)
            {
                Report(document, operation + " lỗi: " + ex.Message);
            }
        }

        private static void Dispatch(Document document, ProjectFamily family, bool advanced, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (family == null) throw new ArgumentNullException(nameof(family));

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
                    Report(
                        document,
                        operation + ": Family '" + family.Name + "' thuộc " + family.Category +
                        " chưa có Direct Draw " + (advanced ? "Advanced" : "Quick") +
                        " an toàn. Dùng workflow chuyên biệt hiện có cho category này.");
                    return;
            }
        }

        private static bool IsWindowFamily(ProjectFamily family)
        {
            if (family.Properties.TryGetValue("OpeningUsage", out var usage) && !string.IsNullOrWhiteSpace(usage))
                return string.Equals(usage.Trim(), "Window", StringComparison.OrdinalIgnoreCase);

            // Window uses WallOpening as the canonical semantic category. These dedicated Family
            // keys are therefore a deterministic compatibility signal when legacy Family data does
            // not yet carry OpeningUsage=Window explicitly.
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
