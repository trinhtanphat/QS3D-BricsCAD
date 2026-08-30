using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RebarModeHealthCommands
    {
        [CommandMethod("QS3DREBARMODEHEALTH", CommandFlags.Modal)]
        public void RebarModeHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "Rebar Mode Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.";
                    PaletteCoordinator.SetStatus(blocked);
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                var issues = new GeneratedRebarModeHealthService().Inspect(project);
                var summary = new HealthSummary(issues);
                var message = "Rebar Mode Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var handles = element.Properties.TryGetValue("GeneratedRebarHandles", out var raw) && !string.IsNullOrWhiteSpace(raw)
                        ? raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray()
                        : element.SourceHandles.ToArray();
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("Rebar Mode Locate " + element.Id + " • " + count + " CAD object");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var message = "QS3DREBARMODEHEALTH lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
