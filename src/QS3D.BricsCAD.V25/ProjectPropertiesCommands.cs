using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectPropertiesCommands
    {
        [CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]
        public void ShowProjectProperties()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var window = new ProjectPropertiesWindow();
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Thuộc tính dự án: surface BLT3D riêng, read-only placeholder; không mở Project Tools.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DPROJECTPROPERTIES lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
