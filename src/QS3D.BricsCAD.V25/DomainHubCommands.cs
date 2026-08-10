using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class DomainHubCommands
    {
        private static DomainHubWindow? _window;

        [CommandMethod("QS3DDOMAIN", CommandFlags.Modal)]
        public void ShowDomainHub()
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new DomainHubWindow();
                    _window.Closed += (_, __) => _window = null;
                }
                if (!_window.IsVisible) Application.ShowModelessWindow(IntPtr.Zero, _window, true);
                else _window.Activate();
            }
            catch (Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DDOMAIN error: " + ex.Message);
            }
        }
    }
}
