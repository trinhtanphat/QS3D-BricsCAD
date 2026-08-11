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

                var commandLabel = Dispatch(document, family);
                if (!string.IsNullOrWhiteSpace(commandLabel))
                    PaletteCoordinator.SetStatus("Vẽ Nhanh • " + family.Name + " • " + commandLabel);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DDRAWACTIVE lỗi: " + ex.Message);
            }
        }

        private static string Dispatch(Document document, ProjectFamily family)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (family == null) throw new ArgumentNullException(nameof(family));

            switch (family.Category)
            {
                case ElementCategory.ArchitecturalWall:
                    new DirectDrawCommands().DrawWall();
                    return "Tường";
                case ElementCategory.Beam:
                    new DirectDrawCommands().DrawBeam();
                    return "Dầm";
                case ElementCategory.Column:
                    new DirectDrawCommands().DrawColumn();
                    return "Cột";
                case ElementCategory.Slab:
                    new DirectDrawCommands().DrawSlab();
                    return "Sàn";
                case ElementCategory.GlassWall:
                    new DirectDrawP1Commands().DrawGlassWall();
                    return "Vách Kính";
                case ElementCategory.WallPier:
                    new DirectDrawP1Commands().DrawWallPier();
                    return "Trụ Tường";
                case ElementCategory.StructuralWall:
                    new DirectDrawP1Commands().DrawStructuralWall();
                    return "Vách BTCT";
                case ElementCategory.Foundation:
                    new DirectDrawP1Commands().DrawFoundation();
                    return "Móng";
                case ElementCategory.Door:
                    new DirectDrawOpeningCommands().DrawDoor();
                    return "Cửa";
                case ElementCategory.WallOpening:
                    if (IsWindowFamily(family))
                    {
                        new DirectDrawWindowCommands().DrawWindow();
                        return "Cửa Sổ";
                    }
                    new DirectDrawOpeningCommands().DrawWallOpening();
                    return "Lỗ Mở";
                default:
                    Report(
                        document,
                        "QS3DDRAWACTIVE: Family '" + family.Name + "' thuộc " + family.Category +
                        " chưa có Direct Draw quick an toàn. Dùng workflow chuyên biệt hiện có cho category này.");
                    return string.Empty;
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
