using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class TktVariantCommands
    {
        [CommandMethod("QS3DGLASSWALL", CommandFlags.UsePickSet)]
        public void CaptureGlassWall() => Capture(ElementCategory.GlassWall, "Vách Kính");

        [CommandMethod("QS3DWALLPIER", CommandFlags.UsePickSet)]
        public void CaptureWallPier() => Capture(ElementCategory.WallPier, "Trụ Tường");

        private static void Capture(ElementCategory category, string label)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var family = project.Families.FirstOrDefault(x => x.Category == category);
                if (family == null)
                {
                    family = new ProjectFamily(Guid.NewGuid().ToString("N"), label, category);
                    project.Families.Add(family);
                }

                EnsureDefault(family, "HeightM", "3.6");
                EnsureDefault(family, "AxisLeftOffsetM", "0");
                EnsureDefault(family, "AxisRightOffsetM", "0");
                if (category == ElementCategory.GlassWall)
                {
                    EnsureDefault(family, "ThicknessM", "0.012");
                    EnsureDefault(family, "Material", "Kính");
                    EnsureDefault(family, "CurtainMaxPanelWidthM", "1.2");
                    EnsureDefault(family, "CurtainMaxPanelHeightM", "1.5");
                    EnsureDefault(family, "CurtainPerimeterFrameWidthM", "0.05");
                    EnsureDefault(family, "CurtainMullionWidthM", "0.05");
                    EnsureDefault(family, "CurtainTransomWidthM", "0.05");
                    EnsureDefault(family, "CurtainFrameMaterial", "Nhôm");
                }
                else
                {
                    EnsureDefault(family, "ThicknessM", "0.2");
                    EnsureDefault(family, "Material", "Gạch");
                    EnsureDefault(family, "WallPierProfileMode", "Rectangular");
                    EnsureDefault(family, "WallPierChamferM", "0.02");
                }

                project.Metadata["ActiveFamilyId"] = family.Id;
                project.Touch();
                var count = SemanticCaptureService.Capture(document, category);
                PaletteCoordinator.RefreshProject();
                var status = label + ": đã ghi " + count + " cấu kiện semantic.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (System.Exception ex)
            {
                var status = label + " lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
        }

        private static void EnsureDefault(ProjectFamily family, string key, string value)
        {
            if (!family.Properties.ContainsKey(key)) family.Properties[key] = value;
        }
    }
}
