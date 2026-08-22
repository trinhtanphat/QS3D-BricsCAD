using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class FloorLevelCommands
    {
<<<<<<< HEAD
        [CommandMethod("QS3DFLOORS", CommandFlags.Modal)]
        public void ShowFloorLevelPicker()
=======
        [CommandMethod("QS3DLEVELS", CommandFlags.Modal)]
        public void ShowFloorPicker()
>>>>>>> origin/main
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new FloorLevelWindow(), true);
<<<<<<< HEAD
                PaletteCoordinator.SetStatus("Tầng / Level Picker: active floor, inspect và assign semantic selection.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DFLOORS lỗi: " + ex.Message;
=======
                PaletteCoordinator.SetStatus("Level Picker: active floor + semantic floor assignment; CAD geometry không tự di chuyển.");
            }
            catch (Exception ex)
            {
                var message = "QS3DLEVELS lỗi: " + ex.Message;
>>>>>>> origin/main
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
