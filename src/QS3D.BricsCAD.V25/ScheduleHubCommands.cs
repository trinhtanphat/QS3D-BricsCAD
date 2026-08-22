using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ScheduleHubCommands
    {
        [CommandMethod("QS3DSCHEDULES", CommandFlags.Modal)]
        public void ShowScheduleHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new ScheduleHubWindow(document), true);
                PaletteCoordinator.SetStatus("Schedule Hub: BQ • vật liệu • curtain • cửa/lỗ • cốt thép • khóa theo bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DSCHEDULES lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
