using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomFinishHealthCommands
    {
        [CommandMethod("QS3DROOMFINISHHEALTH", CommandFlags.Modal)]
        public void ReviewRoomFinishHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    var blocked = "HT_Phòng Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.";
                    PaletteCoordinator.SetStatus(blocked);
                    document.Editor.WriteMessage("\nQS3D " + blocked);
                    return;
                }

                var issues = new RoomFinishHealthService().Inspect(project).ToList();
                var summary = new HealthSummary(issues);
                var status = "HT_Phòng Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);

                if (issues.Count == 0) return;
                ModelHealthWindowPresenter.Show(document, issues, issue =>
                {
                    if (string.IsNullOrWhiteSpace(issue.ElementId)) return;
                    if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)) return;
                    var handles = SourceHandleResolver.Resolve(currentProject, new[] { issue.ElementId });
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("HT_Phòng Health Locate " + issue.ElementId + " • " + count + " CAD object");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
            }
            catch (System.Exception)
            {
                var status = "QS3DROOMFINISHHEALTH lỗi: không thể hoàn tất health check.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }
    }
}
