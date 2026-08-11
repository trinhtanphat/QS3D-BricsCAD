using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// One stable entry point for the common BLT-style workflow:
    /// choose a Family/Type once, then draw using the primary quick command for that Family.
    /// The dispatcher is read-only/non-creating; each target command keeps its own guarded
    /// source -> semantic -> native lifecycle and cancellation boundary.
    /// </summary>
    public sealed class ActiveFamilyQuickDrawCommands
    {
        [CommandMethod("QS3DDRAWACTIVE", CommandFlags.Modal)]
        public void DrawActiveFamily()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "QS3DDRAWACTIVE: bản vẽ chưa có QS3D project. Mở Workspace, tạo/chọn Family trước khi Vẽ Nhanh.");
                    return;
                }

                var family = ProjectFamilyActivationService.GetActive(project);
                if (family == null)
                {
                    Report(document, "QS3DDRAWACTIVE: chưa có Family active. Chọn Family/Type trong Workspace rồi chạy Vẽ Nhanh.");
                    return;
                }

                Dispatch(document, family);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DDRAWACTIVE lỗi: " + ex.Message);
            }
        }

        private static void Dispatch(Document document, ProjectFamily family)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (family == null) throw new ArgumentNullException(nameof(family));

            switch (family.Category)
            {
                case ElementCategory.ArchitecturalWall:
                    new DirectDrawCommands().DrawWall();
                    return;
                case ElementCategory.Beam:
                    new DirectDrawCommands().DrawBeam();
                    return;
                case ElementCategory.Column:
                    new DirectDrawCommands().DrawColumn();
                    return;
                case ElementCategory.Slab:
                    new DirectDrawCommands().DrawSlab();
                    return;
                case ElementCategory.GlassWall:
                    new DirectDrawP1Commands().DrawGlassWall();
                    return;
                case ElementCategory.WallPier:
                    new DirectDrawP1Commands().DrawWallPier();
                    return;
                case ElementCategory.StructuralWall:
                    new DirectDrawP1Commands().DrawStructuralWall();
                    return;
                case ElementCategory.Foundation:
                    new DirectDrawP1Commands().DrawFoundation();
                    return;
                case ElementCategory.Door:
                    new DirectDrawOpeningCommands().DrawDoor();
                    return;
                case ElementCategory.WallOpening:
                    if (IsWindowFamily(family))
                    {
                        new DirectDrawWindowCommands().DrawWindow();
                        return;
                    }
                    new DirectDrawOpeningCommands().DrawWallOpening();
                    return;
                default:
                    Report(
                        document,
                        "QS3DDRAWACTIVE: Family '" + family.Name + "' thuộc " + family.Category +
                        " chưa có Direct Draw quick an toàn. Dùng workflow chuyên biệt hiện có cho category này.");
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
