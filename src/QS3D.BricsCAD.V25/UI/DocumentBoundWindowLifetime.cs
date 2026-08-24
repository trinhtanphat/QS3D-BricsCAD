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
            private readonly Document _lifecycleDocument;
            private readonly IntPtr _nativeDatabaseIdentity;
            private readonly object _documentAccessGate = new object();
            private bool _attached;
            private bool _projectAffinityBound;
            private int _invalidated;
            private int _documentCloseStarted;
            private string _projectId = string.Empty;

            public Registration(Window window, Document document)
            {
                _window = window;
                _lifecycleDocument = document;
                _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
            }

            public void Attach(Document document)
            {
                if (!MatchesNativeDatabase(document))
                    throw new InvalidOperationException("A modeless QS3D window cannot be rebound to a different BricsCAD document.");
                if (_attached) return;

                try
                {
                    ModelessHostQuiescenceCoordinator.EnsureInitialized();
                    BindProjectAffinityIfPresent();
                    _lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;
                    _lifecycleDocument.CloseAborted += OnDocumentCloseAborted;
                    BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                    ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;
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
                    Volatile.Write(ref _documentCloseStarted, 0);
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

            private bool TryResolveLiveDocument(out Document document)
            {
                document = null!;
                try
                {
                    foreach (Document candidate in BcadApplication.DocumentManager)
                    {
                        if (candidate == null || candidate.IsDisposed) continue;
                        if (!MatchesNativeDatabase(candidate)) continue;
                        document = candidate;
                        return true;
                    }
                }
                catch
                {
                    document = null!;
                    return false;
                }

                return false;
            }

            private bool HasAnotherLiveDocument()
            {
                try
                {
                    foreach (Document candidate in BcadApplication.DocumentManager)
                    {
                        if (candidate == null || candidate.IsDisposed) continue;
                        try
                        {
                            var database = candidate.Database;
                            if (database == null) continue;
                            var identity = database.UnmanagedObject;
                            if (identity != IntPtr.Zero && identity != _nativeDatabaseIdentity)
                                return true;
                        }
                        catch
                        {
                            // Ignore one unsafe candidate and keep looking. If enumeration cannot
                            // prove that another live document exists, final-teardown deferral wins.
                        }
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            }

            private void BindProjectAffinityIfPresent()
            {
                if (_projectAffinityBound) return;
                if (!TryResolveLiveDocument(out var document)) return;
                BindProjectAffinityIfPresent(document);
            }

            private void BindProjectAffinityIfPresent(Document document)
            {
                if (_projectAffinityBound) return;
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return;
                _projectId = project.ProjectId ?? string.Empty;
                _projectAffinityBound = true;
            }

            private bool EnsureProjectAffinity()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;

                var closeForProjectChange = false;
                lock (_documentAccessGate)
                {
                    // BeginDocumentClose and DocumentToBeDestroyed take this same gate before
                    // invalidation. The WPF path resolves a currently live managed wrapper from
                    // DocumentManager and never dereferences the wrapper retained for event lifetime.
                    if (Volatile.Read(ref _invalidated) != 0) return false;
                    if (!TryResolveLiveDocument(out var document))
                    {
                        closeForProjectChange = true;
                    }
                    else
                    {
                        try
                        {
                            if (!_projectAffinityBound)
                            {
                                BindProjectAffinityIfPresent(document);
                                return true;
                            }

                            if (ProjectContextCoordinator.TryGetReadOnly(document, out var project) &&
                                string.Equals(project.ProjectId ?? string.Empty, _projectId, StringComparison.OrdinalIgnoreCase))
                                return true;

                            closeForProjectChange = true;
                        }
                        catch
                        {
                            closeForProjectChange = true;
                        }
                    }
                }

                if (closeForProjectChange) CloseForProjectChange();
                return false;
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
                lock (_documentAccessGate)
                {
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }
                DetachDocumentLifecycleHandlersIfSafe();
                DetachDocumentManagerHandler();

                const string message = "QS3D project của cửa sổ modeless này đã thay đổi hoặc không còn được nạp. Cửa sổ đã đóng để tránh thao tác lên semantic state khác; hãy mở lại cửa sổ trong project hiện hành.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                TryCloseWindow();
            }

            private void OnHostQuiescenceAborted(object? sender, EventArgs e)
            {
                // The global coordinator already cleared host quiescence. If document teardown
                // invalidated this registration during the attempted quit, recovery is still
                // dispatcher-deferred so no WPF/native cleanup runs on the BricsCAD quit callback.
                if (Volatile.Read(ref _documentCloseStarted) == 0 ||
                    Volatile.Read(ref _invalidated) == 0)
                    return;

                TryRecoverAfterQuitAbort();
            }

            private void TryRecoverAfterQuitAbort()
            {
                try
                {
                    _window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // QuitAborted recovery is queued. A second quit can begin before this
                        // dispatcher turn, so re-check the global barrier before any native cleanup.
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        DetachDocumentLifecycleHandlersAfterAbort();
                        Detach();
                        TryCloseWindowOnDispatcher();
                    }));
                }
                catch
                {
                    // Keep the registration invalidated/fail-closed if the dispatcher is no longer
                    // available. No native lifecycle subscriptions are mutated from the quit callback.
                }
            }

            private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
            {
                // QuitWillStart is an earlier host boundary than document teardown. Do not even
                // enumerate DocumentManager once the host owns final native/WPF destruction.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                var abandonForHostShutdown = ModelessHostQuiescenceCoordinator.IsQuiescing;
                var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();
                lock (_documentAccessGate)
                {
                    Volatile.Write(ref _documentCloseStarted, 1);
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }

                // BeginDocumentClose is the earliest reliable per-document close boundary used by
                // this coordinator. During application quit BricsCAD owns modeless HWND teardown and
                // native reactor subscriptions; leave both untouched until exit or QuitAborted.
                if (abandonForHostShutdown) return;
                DetachDocumentManagerHandler();
                TryCloseWindow(deferForFinalDocument);
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                // During host teardown the event's managed Document wrapper may already front native
                // state being destroyed. Cross the global barrier before dereferencing e.Document.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                if (!MatchesNativeDatabase(e.Document)) return;
                var abandonForHostShutdown = ModelessHostQuiescenceCoordinator.IsQuiescing;
                var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();
                lock (_documentAccessGate)
                {
                    Volatile.Write(ref _documentCloseStarted, 1);
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }

                // BricsCAD may surface a different managed Document wrapper for the same native
                // database during destruction. Match the stable native database identity captured
                // at bind time so wrapper drift still closes this window, without using mutable paths.
                // If application quit is active, leave HWND/WPF and native reactor teardown to BricsCAD.
                if (abandonForHostShutdown) return;
                DetachDocumentManagerHandler();
                TryCloseWindow(deferForFinalDocument);
            }

            private void OnDocumentCloseAborted(object? sender, EventArgs e)
            {
                // Do not mutate native lifecycle subscriptions while application quit is active.
                // The global QuitAborted owner clears quiescence and schedules stale-window recovery.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                // A vetoed/aborted ordinary document close leaves the document live. The modeless
                // window remains fail-closed (or already closed), but this callback can safely release
                // the per-document lifecycle subscriptions preserved during native close processing.
                DetachDocumentLifecycleHandlersAfterAbort();
            }

            private void TryCloseWindow(bool deferOnDispatcher = false)
            {
                try
                {
                    if (_window.Dispatcher.CheckAccess())
                    {
                        if (deferOnDispatcher)
                        {
                            _window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));
                            return;
                        }

                        TryCloseWindowOnDispatcher();
                        return;
                    }

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
                    if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
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
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                try { BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; }
                catch { }
            }

            private void DetachDocumentLifecycleHandlersIfSafe()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                if (Volatile.Read(ref _documentCloseStarted) != 0) return;
                try { _lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose; }
                catch { }
                try { _lifecycleDocument.CloseAborted -= OnDocumentCloseAborted; }
                catch { }
            }

            private void DetachDocumentLifecycleHandlersAfterAbort()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                try { _lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose; }
                catch { }
                try { _lifecycleDocument.CloseAborted -= OnDocumentCloseAborted; }
                catch { }
            }

            private void OnWindowClosed(object? sender, EventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                Detach();
            }

            private void Detach()
            {
                if (!_attached) return;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                DetachDocumentLifecycleHandlersIfSafe();
                DetachDocumentManagerHandler();
                try { ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted; }
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
