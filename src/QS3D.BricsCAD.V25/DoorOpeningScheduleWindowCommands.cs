using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DoorOpeningScheduleWindowCommands
    {
        [CommandMethod("QS3DDOORSCHEDULE", CommandFlags.Modal)]
        public void ShowDoorOpeningSchedule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new DoorOpeningScheduleWindow(document), true);
                PaletteCoordinator.SetStatus("Door/Opening Schedule: group • host provenance • XLSX • khóa theo project của bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DDOORSCHEDULE lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
