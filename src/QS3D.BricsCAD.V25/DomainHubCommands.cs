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
                var published = _window;
                if (published != null)
                {
                    if (published.IsLoaded)
                    {
                        try { published.Activate(); } catch { }
                        return;
                    }

                    ReleasePublishedWindow(published);
                }

                var window = new DomainHubWindow();
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
            }
            catch (System.Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DDOMAIN error: " + ex.Message);
            }
        }

        private static void ReleasePublishedWindow(DomainHubWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
        }
    }
}
