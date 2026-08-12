using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
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
            private bool _projectAffinityBound;
            private string _projectId = string.Empty;

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

                try
                {
                    BindProjectAffinityIfPresent();
                    BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                    _window.Activated += OnWindowActivated;
                    _window.PreviewMouseDown += OnPreviewMouseDown;
                    _window.PreviewKeyDown += OnPreviewKeyDown;
                    _window.Closed += OnWindowClosed;
                    _attached = true;
                }
                catch
                {
                    // Detach owns best-effort removal of every handler. Mark the partial attempt as
                    // attached only long enough to make that cleanup path authoritative.
                    _attached = true;
                    Detach();
                    _projectAffinityBound = false;
                    _projectId = string.Empty;
                    throw;
                }
            }

            private void BindProjectAffinityIfPresent()
            {
                if (_projectAffinityBound) return;
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project)) return;
                _projectId = project.ProjectId ?? string.Empty;
                _projectAffinityBound = true;
            }

            private bool EnsureProjectAffinity()
            {
                try
                {
                    if (!_projectAffinityBound)
                    {
                        BindProjectAffinityIfPresent();
                        return true;
                    }

                    if (ProjectContextCoordinator.TryGetReadOnly(_document, out var project) &&
                        string.Equals(project.ProjectId ?? string.Empty, _projectId, StringComparison.OrdinalIgnoreCase))
                        return true;

                    CloseForProjectChange();
                    return false;
                }
                catch
                {
                    CloseForProjectChange();
                    return false;
                }
            }

            private void OnWindowActivated(object? sender, EventArgs e) => EnsureProjectAffinity();

            private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
            {
                if (!EnsureProjectAffinity()) e.Handled = true;
            }

            private void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (!EnsureProjectAffinity()) e.Handled = true;
            }

            private void CloseForProjectChange()
            {
                Detach();
                const string message = "QS3D project của cửa sổ modeless này đã thay đổi hoặc không còn được nạp. Cửa sổ đã đóng để tránh thao tác lên semantic state khác; hãy mở lại cửa sổ trong project hiện hành.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                try
                {
                    if (_window.Dispatcher.CheckAccess())
                        _window.Close();
                    else
                        _window.Dispatcher.BeginInvoke(new Action(_window.Close));
                }
                catch
                {
                    // Fail closed: stale modeless UI must never stay actionable merely because cleanup failed.
                }
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
                try { _window.Activated -= OnWindowActivated; }
                catch { }
                try { _window.PreviewMouseDown -= OnPreviewMouseDown; }
                catch { }
                try { _window.PreviewKeyDown -= OnPreviewKeyDown; }
                catch { }
                try { _window.Closed -= OnWindowClosed; }
                catch { }
                _attached = false;
            }
        }
    }
}
