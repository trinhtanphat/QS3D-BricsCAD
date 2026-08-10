using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectToolsCommands
    {
        [CommandMethod("QS3DPROJECTTOOLS", CommandFlags.Modal)]
        public void ShowProjectTools()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new ProjectToolsWindow(document), true);
                PaletteCoordinator.SetStatus("Project Tools: tầng • vật liệu • template • module • health • khóa theo bản vẽ.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DPROJECTTOOLS lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
