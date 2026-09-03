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
            private IDisposable? _nativeLifecycleSubscription;
            private bool _attached;
            private bool _projectAffinityBound;
            private int _invalidated;
            private int _documentCloseStarted;
            private int _windowClosedDuringQuiescence;
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
                    _nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(
                        _lifecycleDocument,
                        _nativeDatabaseIdentity,
                        OnBeginDocumentClose,
                        OnDocumentCloseAborted,
                        OnDocumentToBeDestroyed);
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
                    Volatile.Write(ref _windowClosedDuringQuiescence, 0);
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
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                lock (_documentAccessGate)
                {
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }
                DetachDocumentLifecycleHandlersIfSafe();

                const string message = "QS3D project của cửa sổ modeless này đã thay đổi hoặc không còn được nạp. Cửa sổ đã đóng để tránh thao tác lên semantic state khác; hãy mở lại cửa sổ trong project hiện hành.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                TryCloseWindow();
            }

            private void OnHostQuiescenceAborted(object? sender, EventArgs e)
            {
                // Closed is allowed to run while host quiescence owns native teardown, but its
                // normal Detach path must not mutate native subscriptions until QuitAborted clears
                // the barrier. Recover that already-closed registration even if the shared native
                // lifecycle coordinator correctly suppressed every document callback during quit.
                if (Volatile.Read(ref _windowClosedDuringQuiescence) != 0)
                {
                    TryRecoverClosedWindowAfterQuitAbort();
                    return;
                }

                // The global coordinator already cleared host quiescence. If document teardown
                // invalidated this registration during the attempted quit, recovery is still
                // dispatcher-deferred so no WPF/native cleanup runs on the BricsCAD quit callback.
                if (Volatile.Read(ref _documentCloseStarted) == 0 ||
                    Volatile.Read(ref _invalidated) == 0)
                    return;

                TryRecoverAfterQuitAbort();
            }

            private void TryRecoverClosedWindowAfterQuitAbort()
            {
                try
                {
                    _window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // QuitAborted recovery is queued. A second quit can begin before or during
                        // this dispatcher turn, so keep the deferred marker armed until managed
                        // detach has actually completed.
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        if (Volatile.Read(ref _windowClosedDuringQuiescence) == 0) return;
                        DetachDocumentLifecycleHandlersAfterAbort();
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        Detach();
                        if (!_attached)
                            Interlocked.Exchange(ref _windowClosedDuringQuiescence, 0);
                    }));
                }
                catch
                {
                    // Keep the registration fail-closed and the deferred marker armed if the
                    // dispatcher is unavailable. No native lifecycle cleanup runs from this callback.
                }
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
                // The shared native lifecycle coordinator crosses the host-quiescence barrier before
                // dispatching this managed callback. Re-check defensively before DocumentManager access.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                var deferForFinalDocument = !HasAnotherLiveDocument();
                lock (_documentAccessGate)
                {
                    Volatile.Write(ref _documentCloseStarted, 1);
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }

                // Preserve the proven synchronous close path while another native document exists;
                // final/only-document close remains dispatcher-deferred.
                TryCloseWindow(deferForFinalDocument);
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                // The shared coordinator has already matched this registration to the destroying
                // document by managed lifecycle reference or by the safe live-wrapper native fallback.
                // Do not reopen the event's managed Document here: native teardown may advance between
                // affinity proof and callback dispatch, turning a proven match into a false negative.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                var deferForFinalDocument = !HasAnotherLiveDocument();
                lock (_documentAccessGate)
                {
                    Volatile.Write(ref _documentCloseStarted, 1);
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }

                TryCloseWindow(deferForFinalDocument);
            }

            private void OnDocumentCloseAborted(object? sender, EventArgs e)
            {
                // Do not mutate native lifecycle subscriptions while application quit is active.
                // The global QuitAborted owner clears quiescence and schedules stale-window recovery.
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                // A vetoed/aborted ordinary document close leaves the document live. The modeless
                // window remains fail-closed (or already closed), but this callback can safely release
                // its managed registration from the shared native lifecycle coordinator.
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

            private void DetachDocumentLifecycleHandlersIfSafe()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                if (Volatile.Read(ref _documentCloseStarted) != 0) return;
                DetachNativeLifecycleSubscription();
            }

            private void DetachDocumentLifecycleHandlersAfterAbort()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                DetachNativeLifecycleSubscription();
            }

            private void DetachNativeLifecycleSubscription()
            {
                var subscription = Interlocked.Exchange(ref _nativeLifecycleSubscription, null);
                if (subscription == null) return;
                try { subscription.Dispose(); }
                catch { }
            }

            private void OnWindowClosed(object? sender, EventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                {
                    Volatile.Write(ref _windowClosedDuringQuiescence, 1);
                    return;
                }
                Detach();
            }

            private void Detach()
            {
                if (!_attached) return;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                DetachDocumentLifecycleHandlersIfSafe();
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
