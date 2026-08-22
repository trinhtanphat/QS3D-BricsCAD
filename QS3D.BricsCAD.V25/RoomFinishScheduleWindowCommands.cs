using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class RoomFinishScheduleWindowCommands
    {
        [CommandMethod("QS3DFINISHSCHEDULE", CommandFlags.Modal)]
        public void ShowRoomFinishSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new RoomFinishScheduleWindow(document), true);
                PaletteCoordinator.SetStatus("HT_Phòng Schedule: review • filter • XLSX • khóa theo project của bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFINISHSCHEDULE lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
