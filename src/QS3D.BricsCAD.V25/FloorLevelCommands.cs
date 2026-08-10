using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FloorLevelCommands
    {
        [CommandMethod("QS3DFLOORS", CommandFlags.Modal)]
        public void ShowFloorLevelPicker()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new FloorLevelWindow(), true);
                PaletteCoordinator.SetStatus("Tầng / Level Picker: active floor, inspect và assign semantic selection.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFLOORS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
