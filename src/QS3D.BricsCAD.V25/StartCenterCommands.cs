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
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new StartCenterWindow();
                    _window.Closed += OnStartCenterClosed;
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
                var document = Application.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DSTART error: " + ex.Message);
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
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
            _documentActivatedSubscribed = false;
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

        private static void OnStartCenterClosed(object sender, EventArgs e)
        {
            UnsubscribeFromDocumentActivation();
            if (ReferenceEquals(sender, _window))
                _window = null;
        }
    }
}