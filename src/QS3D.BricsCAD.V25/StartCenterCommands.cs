using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StartCenterCommands
    {
        private static StartCenterWindow? _window;

        [CommandMethod("QS3DSTART", CommandFlags.Modal)]
        public void ShowStartCenter()
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new StartCenterWindow();
                    _window.Closed += (_, __) => _window = null;
                }

                if (!_window.IsVisible)
                    Application.ShowModelessWindow(IntPtr.Zero, _window, true);
                else
                {
                    _window.RefreshFromActiveDocument();
                    _window.Activate();
                }
            }
            catch (System.Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DSTART error: " + ex.Message);
            }
        }
    }
}