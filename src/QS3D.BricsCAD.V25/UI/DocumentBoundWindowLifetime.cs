using System;
using System.Runtime.CompilerServices;
using System.Windows;
using Bricscad.ApplicationServices;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    internal static class DocumentBoundWindowLifetime
    {
        private static readonly ConditionalWeakTable<Window, Registration> Registrations = new ConditionalWeakTable<Window, Registration>();

        public static void Attach(Window window, Document document)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (document == null) throw new ArgumentNullException(nameof(document));
            var registration = Registrations.GetValue(window, key => new Registration(key, document));
            registration.Attach(document);
        }

        private sealed class Registration
        {
            private readonly Window _window;
            private readonly Document _document;
            private bool _attached;

            public Registration(Window window, Document document)
            {
                _window = window;
                _document = document;
            }

            public void Attach(Document document)
            {
                if (!ReferenceEquals(document, _document))
                    throw new InvalidOperationException("A modeless QS3D window cannot be rebound to a different BricsCAD document.");
                if (_attached) return;
                BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                _window.Closed += OnWindowClosed;
                _attached = true;
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                if (!ReferenceEquals(e.Document, _document)) return;
                Detach();
                try
                {
                    if (_window.Dispatcher.CheckAccess())
                        _window.Close();
                    else
                        _window.Dispatcher.BeginInvoke(new Action(_window.Close));
                }
                catch
                {
                    // The document is already tearing down. Never let modeless UI cleanup block DWG close.
                }
            }

            private void OnWindowClosed(object? sender, EventArgs e) => Detach();

            private void Detach()
            {
                if (!_attached) return;
                try { BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; }
                catch { }
                try { _window.Closed -= OnWindowClosed; }
                catch { }
                _attached = false;
            }
        }
    }
}
