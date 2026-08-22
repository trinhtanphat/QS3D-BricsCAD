using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BeamStirrupCommands
    {
        [CommandMethod("QS3DBEAMSTIRRUP3D", CommandFlags.UsePickSet)]
        public void BuildBeamStirrupsWorkspaceAlias() => BuildBeamStirrups();

        [CommandMethod("QS3DREBARSTIRRUP3D", CommandFlags.UsePickSet)]
        public void BuildBeamStirrups()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Beam Stirrup 3D: chọn Beam semantic LINE có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8).");
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Beam Stirrup 3D");
                var result = BeamStirrupSolidBuilder.BuildSelected(document, project);
                var message = result.Stirrups == 0
                    ? "Beam Stirrup 3D: chọn Beam semantic LINE có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8)."
                    : "Beam Stirrup 3D: đã tạo/cập nhật " + result.Stirrups + " đai trên " + result.Elements + " dầm.";
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DREBARSTIRRUP3D lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DBEAMSTIRRUPHEALTH", CommandFlags.Modal)]
        public void BeamStirrupHealthWorkspaceAlias() => BeamStirrupHealth();

        [CommandMethod("QS3DREBARSTIRRUPHEALTH", CommandFlags.Modal)]
        public void BeamStirrupHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Beam Stirrup Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
                    return;
                }

                var handles = new List<string>();
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedBeamStirrupHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    handles.AddRange(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0));
                }
                var live = CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase));
                var issues = new GeneratedBeamStirrupHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Beam Stirrup Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                foreach (var issue in issues.Take(50))
                    document.Editor.WriteMessage("\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) document.Editor.WriteMessage("\n  … health output truncated.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DREBARSTIRRUPHEALTH lỗi: " + ex.Message);
            }
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + message + " UI sync warning: " + ex.Message);
            }
        }

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
