using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FloorLevelCommands
    {
        [CommandMethod("QS3DLEVELS", CommandFlags.Modal)]
        public void ShowFloorPicker()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                ExistingProjectMutationContext.TryGet(document, out _);
                var window = new FloorLevelWindow(document);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Level Picker: active floor + semantic floor assignment • khóa theo bản vẽ đang mở; CAD geometry không tự di chuyển.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DLEVELS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}