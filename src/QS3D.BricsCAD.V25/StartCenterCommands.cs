using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class StartCenterCommands
    {
        private static StartCenterWindow? _window;
        private static bool _documentActivatedSubscribed;

        [CommandMethod("QS3DSTART", CommandFlags.Modal)]
        public void ShowStartCenter()
        {
            StartCenterWindow? createdWindow = null;

            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    createdWindow = new BltStartCenterWindow();
                    _window = createdWindow;
                    createdWindow.Closed += OnStartCenterClosed;
                    SubscribeToDocumentActivation();
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
                if (createdWindow != null)
                    ReleaseStartCenterWindow(createdWindow);

                try
                {
                    var document = Application.DocumentManager.MdiActiveDocument;
                    document?.Editor.WriteMessage("\nQS3DSTART error: " + ex.Message);
                }
                catch (System.Exception)
                {
                    // Never let optional Start Center diagnostics escape the command failure boundary.
                }
            }
        }

        private static void SubscribeToDocumentActivation()
        {
            if (_documentActivatedSubscribed) return;
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
            _documentActivatedSubscribed = true;
        }

        private static void UnsubscribeFromDocumentActivation()
        {
            if (!_documentActivatedSubscribed) return;

            try
            {
                Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
                _documentActivatedSubscribed = false;
            }
            catch (System.Exception)
            {
                // Keep the flag true so later cleanup can retry without creating a duplicate subscription.
            }
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            var window = _window;
            if (window == null || !window.IsLoaded) return;

            try
            {
                window.RefreshFromActiveDocument();
            }
            catch (System.Exception ex)
            {
                try
                {
                    e.Document?.Editor.WriteMessage("\nQS3DSTART refresh warning: " + ex.Message);
                }
                catch (System.Exception)
                {
                    // Never let optional Start Center diagnostics escape into BricsCAD document activation.
                }
            }
        }

        private static void ReleaseStartCenterWindow(StartCenterWindow window)
        {
            if (!ReferenceEquals(window, _window)) return;

            window.Closed -= OnStartCenterClosed;
            UnsubscribeFromDocumentActivation();
            _window = null;
        }

        private static void OnStartCenterClosed(object sender, EventArgs e)
        {
            if (sender is StartCenterWindow window)
                ReleaseStartCenterWindow(window);
        }
    }
}
