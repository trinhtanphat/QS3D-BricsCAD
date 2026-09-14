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
        private sealed class AttachGate
        {
            public bool IsAttaching;
        }

        private static readonly ConditionalWeakTable<Window, Registration> Registrations = new ConditionalWeakTable<Window, Registration>();
        private static readonly ConditionalWeakTable<Window, AttachGate> AttachGates = new ConditionalWeakTable<Window, AttachGate>();

        public static void Attach(Window window, Document document)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (document == null) throw new ArgumentNullException(nameof(document));

            var attachGate = AttachGates.GetValue(window, _ => new AttachGate());
            lock (attachGate)
            {
                if (attachGate.IsAttaching)
                    throw new InvalidOperationException("A modeless QS3D window attach is already in progress.");

                attachGate.IsAttaching = true;
                try
                {
                    var registration = Registrations.GetValue(window, key => new Registration(key, document));
                    if (registration.HasFailedInitialAttach)
                    {
                        registration.TryCompleteFailedInitialAttachCleanup();
                        if (!registration.CanRestartAfterFailedInitialAttach)
                            throw new InvalidOperationException("A previous modeless QS3D window attach failed and its native lifecycle cleanup is still pending.");

                        if (!Registrations.TryGetValue(window, out var failedRegistration) ||
                            !ReferenceEquals(failedRegistration, registration))
                            throw new InvalidOperationException("The modeless QS3D window registration changed while failed-attach cleanup was being finalized.");

                        Registrations.Remove(window);
                        registration = Registrations.GetValue(window, key => new Registration(key, document));
                    }

                    var wasAttached = registration.IsAttached;
                    try
                    {
                        registration.Attach(document);
                    }
                    catch
                    {
                        if (!wasAttached && registration.CanRestartAfterFailedInitialAttach &&
                            Registrations.TryGetValue(window, out var currentRegistration) &&
                            ReferenceEquals(currentRegistration, registration))
                        {
                            Registrations.Remove(window);
                        }
                        throw;
                    }
                }
                finally
                {
                    attachGate.IsAttaching = false;
                }
            }
        }

        private sealed class Registration
        {
            private readonly Window _window;
            private Document _lifecycleDocument;
            private readonly IntPtr _nativeDatabaseIdentity;
            private readonly object _documentAccessGate = new object();
            private IDisposable? _nativeLifecycleSubscription;
            private bool _attached;
            private bool _projectAffinityBound;
            private bool _initialAttachFailed;
            private int _invalidated;
            private int _documentCloseStarted;
            private int _windowClosedDuringQuiescence;
            private string _projectId = string.Empty;
            private string _drawingFingerprint = string.Empty;

            public bool IsAttached => _attached;
            public bool HasFailedInitialAttach => _initialAttachFailed;
            public bool CanRestartAfterFailedInitialAttach =>
                _initialAttachFailed &&
                !_attached &&
                _nativeLifecycleSubscription == null;

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
                if (_attached)
                {
                    lock (_documentAccessGate)
                    {
                        if (Volatile.Read(ref _invalidated) != 0)
                            throw new InvalidOperationException("An invalidated modeless QS3D window cannot move to another BricsCAD document wrapper.");
                        if (ReferenceEquals(document, _lifecycleDocument)) return;
                        if (!MatchesBoundDocumentAffinity(document))
                            throw new InvalidOperationException("A modeless QS3D window cannot move to a wrapper with different project/drawing affinity.");
                        DocumentBoundNativeLifecycleCoordinator.Rebind(
                            document,
                            _nativeDatabaseIdentity,
                            MatchesBoundDocumentAffinity);
                        _lifecycleDocument = document;
                        return;
                    }
                }

                try
                {
                    ModelessHostQuiescenceCoordinator.EnsureInitialized();
                    BindProjectAffinityIfPresent();
                    _nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(
                        _lifecycleDocument,
                        _nativeDatabaseIdentity,
                        MatchesBoundDocumentAffinity,
                        OnBeginDocumentClose,
                        OnDocumentCloseAborted,
                        OnDocumentToBeDestroyed);
                    ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;
                    _window.Activated += OnWindowActivated;
                    _window.PreviewMouseDown += OnPreviewMouseDown;
                    _window.PreviewKeyDown += OnPreviewKeyDown;
                    _window.Closed += OnWindowClosed;
                    _attached = true;
                    _initialAttachFailed = false;
                }
                catch
                {
                    _initialAttachFailed = true;
                    _attached = true;
                    Detach();
                    _projectAffinityBound = false;
                    Volatile.Write(ref _invalidated, 0);
                    Volatile.Write(ref _documentCloseStarted, 0);
                    Volatile.Write(ref _windowClosedDuringQuiescence, 0);
                    _projectId = string.Empty;
                    _drawingFingerprint = string.Empty;
                    throw;
                }
            }

            public void TryCompleteFailedInitialAttachCleanup()
            {
                if (!_initialAttachFailed) return;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                if (_attached) Detach();
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
                        if (!ReferenceEquals(candidate, _lifecycleDocument)) continue;
                        if (!MatchesNativeDatabase(candidate)) break;
                        document = candidate;
                        return true;
                    }

                    foreach (Document candidate in BcadApplication.DocumentManager)
                    {
                        if (candidate == null || candidate.IsDisposed) continue;
                        if (ReferenceEquals(candidate, _lifecycleDocument)) continue;
                        if (!MatchesNativeDatabase(candidate)) continue;
                        if (!MatchesBoundDocumentAffinity(candidate)) continue;
                        DocumentBoundNativeLifecycleCoordinator.Rebind(
                            candidate,
                            _nativeDatabaseIdentity,
                            MatchesBoundDocumentAffinity);
                        _lifecycleDocument = candidate;
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

            private bool MatchesBoundDocumentAffinity(Document candidate)
            {
                if (!_projectAffinityBound ||
                    string.IsNullOrWhiteSpace(_projectId) ||
                    string.IsNullOrWhiteSpace(_drawingFingerprint))
                    return false;

                try
                {
                    if (!ProjectContextCoordinator.TryGetReadOnly(candidate, out var project)) return false;
                    return string.Equals(project.ProjectId ?? string.Empty, _projectId, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(project.DrawingFingerprint ?? string.Empty, _drawingFingerprint, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
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
                        catch { }
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

                var projectId = project.ProjectId ?? string.Empty;
                var drawingFingerprint = project.DrawingFingerprint ?? string.Empty;
                if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(drawingFingerprint)) return;

                _projectId = projectId;
                _drawingFingerprint = drawingFingerprint;
                _projectAffinityBound = true;
            }

            private bool EnsureProjectAffinity()
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;

                var closeForProjectChange = false;
                lock (_documentAccessGate)
                {
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

                            if (MatchesBoundDocumentAffinity(document)) return true;
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
                if (Volatile.Read(ref _windowClosedDuringQuiescence) != 0)
                {
                    TryRecoverClosedWindowAfterQuitAbort();
                    return;
                }

                if (Volatile.Read(ref _documentCloseStarted) == 0 || Volatile.Read(ref _invalidated) == 0) return;
                TryRecoverAfterQuitAbort();
            }

            private void TryRecoverClosedWindowAfterQuitAbort()
            {
                try
                {
                    _window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        if (Volatile.Read(ref _windowClosedDuringQuiescence) == 0) return;
                        DetachDocumentLifecycleHandlersAfterAbort();
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        Detach();
                        if (!_attached) Interlocked.Exchange(ref _windowClosedDuringQuiescence, 0);
                    }));
                }
                catch { }
            }

            private void TryRecoverAfterQuitAbort()
            {
                try
                {
                    _window.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                        DetachDocumentLifecycleHandlersAfterAbort();
                        Detach();
                        TryCloseWindowOnDispatcher();
                    }));
                }
                catch { }
            }

            private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                var deferForFinalDocument = !HasAnotherLiveDocument();
                lock (_documentAccessGate)
                {
                    Volatile.Write(ref _documentCloseStarted, 1);
                    if (Interlocked.Exchange(ref _invalidated, 1) != 0) return;
                }
                TryCloseWindow(deferForFinalDocument);
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                // The shared coordinator has already matched this registration by current lifecycle
                // wrapper or a safe native-identity fallback. Do not reopen the event Document here.
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
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
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
                catch { }
            }

            private void TryCloseWindowOnDispatcher()
            {
                try
                {
                    if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                    _window.Close();
                }
                catch { }
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
                try { subscription.Dispose(); } catch { }
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
                try { ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted; } catch { }
                try { _window.Activated -= OnWindowActivated; } catch { }
                try { _window.PreviewMouseDown -= OnPreviewMouseDown; } catch { }
                try { _window.PreviewKeyDown -= OnPreviewKeyDown; } catch { }
                try { _window.Closed -= OnWindowClosed; } catch { }
                _attached = false;
            }
        }
    }
}
