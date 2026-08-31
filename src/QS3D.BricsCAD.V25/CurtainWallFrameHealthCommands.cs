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
    public sealed class CurtainWallFrameHealthCommands
    {
        [CommandMethod("QS3DCURTAINFRAMEHEALTH", CommandFlags.Modal)]
        public void CurtainFrameHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "Curtain Frame Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.";
                    PaletteCoordinator.SetStatus(blocked);
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                var handles = project.Elements.SelectMany(ParseHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var live = CadHandleService.GetLiveSolidHandles(document, handles);
                var issues = new List<ModelHealthIssue>(new GeneratedCurtainFrameHealthService().Inspect(project, live));
                issues.AddRange(CurtainWallFrameLiveStateService.Inspect(document, project));
                issues.AddRange(new GeneratedCurtainPanelHealthService().Inspect(project, live));
                issues.AddRange(CurtainWallPanelLiveStateService.Inspect(document, project));
                issues.AddRange(GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project));
                var summary = new HealthSummary(issues);
                var message = "Curtain Frame Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = CadHandleService.Select(document, ParseHandles(element));
                    PaletteCoordinator.SetStatus("Curtain Frame Health Định vị " + element.Id + " • " + count + " frame solid(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var message = "QS3DCURTAINFRAMEHEALTH lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }

        private static IEnumerable<string> ParseHandles(QS3D.Core.Domain.ProjectElement element)
        {
            var frames = element.Properties.TryGetValue("GeneratedCurtainFrameHandles", out var frameRaw) ? frameRaw : string.Empty;
            var panels = element.Properties.TryGetValue("GeneratedCurtainPanelHandles", out var panelRaw) ? panelRaw : string.Empty;
            return (frames + ";" + panels).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
