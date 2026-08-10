using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GeometryExtensionsCommands
    {
        [CommandMethod("QS3DGEOMETRYEXT", CommandFlags.Modal)]
        public void ShowGeometryExtensions()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                Application.ShowModelessWindow(IntPtr.Zero, new GeometryExtensionsWindow(), true);
                PaletteCoordinator.SetStatus("Đã mở Geometry Extensions.");
            }
            catch (System.Exception ex)
            {
                var message = "QS3DGEOMETRYEXT lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
