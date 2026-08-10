using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CurtainWallHubCommands
    {
        [CommandMethod("QS3DCURTAIN", CommandFlags.Modal)]
        public void ShowCurtainWallHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var window = new CurtainWallWindow(document);
                DocumentBoundWindowLifetime.Attach(window, document);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                PaletteCoordinator.SetStatus("Vách Kính Hub: Family • panel grid • schedule • workflow 3D.");
            }
            catch (System.Exception ex)
            {
                PaletteCoordinator.SetStatus("QS3DCURTAIN lỗi: " + ex.Message);
                document.Editor.WriteMessage("\nQS3DCURTAIN lỗi: " + ex.Message);
            }
        }
    }
}