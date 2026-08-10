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
                var project = ProjectContextCoordinator.GetOrCreate(document);
                var issues = new RoomFinishHealthService().Inspect(project).ToList();
                var summary = new HealthSummary(issues);
                var status = "HT_Phòng Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);

                if (issues.Count == 0) return;
                var window = new ModelHealthWindow(issues, issue =>
                {
                    if (string.IsNullOrWhiteSpace(issue.ElementId)) return;
                    var handles = SourceHandleResolver.Resolve(project, new[] { issue.ElementId });
                    var count = CadHandleService.Select(document, handles);
                    PaletteCoordinator.SetStatus("HT_Phòng Health Locate " + issue.ElementId + " • " + count + " CAD object");
                    if (count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            }
            catch (System.Exception ex)
            {
                var status = "QS3DROOMFINISHHEALTH lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\n" + status);
            }
        }
    }
}
