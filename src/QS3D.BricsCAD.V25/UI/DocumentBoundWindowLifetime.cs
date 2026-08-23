using System;
using System.Runtime.CompilerServices;
using System.Threading;
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
            private readonly IntPtr _nativeDatabaseIdentity;
            private bool _attached;
            private bool _projectAffinityBound;
            private int _invalidated;
            private string _projectId = string.Empty;

            public Registration(Window window, Document document)
            {
                _window = window;
                _document = document;
                _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            }

            public void Attach(Document document)
            {
                if (!MatchesNativeDatabase(document))
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
                    Volatile.Write(ref _invalidated, 0);
                    _projectId = string.Empty;
                    throw;
                }
            }

            private static IntPtr GetNativeDatabaseIdentity(Document document)
            {
                var database = document.Database;
                if (database == null)
                    throw new InvalidOperationException("A modeless QS3D window requires a BricsCAD document database.");

                var identity = database.UnmanagedObject;
                if (identity == IntPtr.Zero)
                    throw new InvalidOperationException("A modeless QS3D window requires a live native BricsCAD database.");
                return identity;
            }

            private bool MatchesNativeDatabase(Document document)
            {
                if (document == null) return false;
                try
                {
                    var database = document.Database;
                    return database != null &&
                           database.UnmanagedObject != IntPtr.Zero &&
                           database.UnmanagedObject == _nativeDatabaseIdentity;
                }
                catch
                {
                    return false;
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
                if (Volatile.Read(ref _invalidated) != 0) return false;

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
                if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                DetachDocumentManagerHandler();

                const string message = "QS3D project của cửa sổ modeless này đã thay đổi hoặc không còn được nạp. Cửa sổ đã đóng để tránh thao tác lên semantic state khác; hãy mở lại cửa sổ trong project hiện hành.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                TryCloseWindow();
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                if (!MatchesNativeDatabase(e.Document)) return;
                if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;

                // BricsCAD may surface a different managed Document wrapper for the same native
                // database during destruction. Match the stable native database identity captured
                // at bind time so wrapper drift still closes this window, without using mutable paths.
                DetachDocumentManagerHandler();
                TryCloseWindow();
            }

            private void TryCloseWindow()
            {
                try
                {
                    if (_window.Dispatcher.CheckAccess())
                        TryCloseWindowOnDispatcher();
                    else
                        _window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));
                }
                catch
                {
                    // Fail closed: if scheduling/closing fails, _invalidated remains set and the
                    // still-attached mouse/key guards continue rejecting interaction.
                }
            }

            private void TryCloseWindowOnDispatcher()
            {
                try
                {
                    _window.Close();
                }
                catch
                {
                    // Keep the guards attached. A later user/host close can still raise Closed and
                    // detach normally, but stale QS3D interaction cannot resume in the meantime.
                }
            }

            private void DetachDocumentManagerHandler()
            {
                try { BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; }
                catch { }
            }

            private void OnWindowClosed(object? sender, EventArgs e) => Detach();

            private void Detach()
            {
                if (!_attached) return;
                DetachDocumentManagerHandler();
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
