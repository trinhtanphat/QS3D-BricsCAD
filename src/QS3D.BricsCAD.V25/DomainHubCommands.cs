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
            DomainHubWindow? candidate = null;
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

                candidate = new DomainHubWindow();
                var window = candidate;
                window.Closed += (_, __) => ReleasePublishedWindow(window);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
                if (!window.IsLoaded) return;

                _window = window;
                candidate = null;
            }
            catch (System.Exception ex)
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DDOMAIN error: " + ex.Message);
            }
            finally
            {
                if (candidate != null) TryCloseUnpublishedWindow(candidate);
            }
        }

        private static void ReleasePublishedWindow(DomainHubWindow window)
        {
            if (!ReferenceEquals(_window, window)) return;
            _window = null;
        }

        private static void TryCloseUnpublishedWindow(DomainHubWindow window)
        {
            if (ReferenceEquals(_window, window)) return;
            try { window.Close(); } catch (System.Exception) { }
        }
    }
}
