using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class Rebar3DHubCommands
    {
        [CommandMethod("QS3DREBARHUB", CommandFlags.Modal)]
        public void ShowRebarHub()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var window = new Rebar3DHubWindow();
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3DREBARHUB lỗi: " + ex.Message);
                PaletteCoordinator.SetStatus("QS3DREBARHUB lỗi: " + ex.Message);
            }
        }
    }
}
