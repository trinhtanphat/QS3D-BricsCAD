using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeneratedHandleOwnershipHealthCommands
    {
        [CommandMethod("QS3DHANDLEHEALTH", CommandFlags.Modal)]
        public void ReviewHandleOwnership()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Handle Ownership Health: BLOCKED • chưa có QS3D project state/sidecar; health check không tạo project mới.");
                    return;
                }

                var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
                var summary = new HealthSummary(issues);
                var message = "Handle Ownership Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                Report(document, message);
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var element = currentProject.FindElement(issue.ElementId);
                    if (element == null) return;
                    var count = Cad.CadHandleService.Select(document, Services.SemanticReferenceHandles.Get(element));
                    PaletteCoordinator.SetStatus("Handle Health Định vị " + element.Id + " • " + count + " semantic source/generated reference(s)");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                Report(document, "QS3DHANDLEHEALTH lỗi: không thể hoàn tất health check.");
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }
    }
}
