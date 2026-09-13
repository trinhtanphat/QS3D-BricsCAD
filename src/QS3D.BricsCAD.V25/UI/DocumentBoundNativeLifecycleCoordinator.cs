using System;
using System.Collections.Generic;
using System.Threading;
using Bricscad.ApplicationServices;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owns BricsCAD document/native lifecycle subscriptions for document-bound modeless windows.
    /// Native reactors retain only coordinator entries; per-window callbacks are weak from the
    /// native side so final host teardown cannot keep WPF windows rooted through reactor delegates.
    /// </summary>
    internal static class DocumentBoundNativeLifecycleCoordinator
    {
        internal delegate void BeginCloseCallback(object sender, DocumentBeginCloseEventArgs e);
        internal delegate void CloseAbortedCallback(object? sender, EventArgs e);
        internal delegate void DestroyedCallback(object sender, DocumentCollectionEventArgs e);

        private static readonly object Gate = new object();
        private static readonly Dictionary<IntPtr, Entry> Entries = new Dictionary<IntPtr, Entry>();
        private static int _documentManagerInitialized;

        internal static IDisposable Register(
            Document lifecycleDocument,
            IntPtr nativeDatabaseIdentity,
            Func<Document, bool> replacementAffinity,
            BeginCloseCallback beginClose,
            CloseAbortedCallback closeAborted,
            DestroyedCallback destroyed)
        {
            if (lifecycleDocument == null) throw new ArgumentNullException(nameof(lifecycleDocument));
            if (nativeDatabaseIdentity == IntPtr.Zero) throw new ArgumentOutOfRangeException(nameof(nativeDatabaseIdentity));
            if (replacementAffinity == null) throw new ArgumentNullException(nameof(replacementAffinity));
            if (beginClose == null) throw new ArgumentNullException(nameof(beginClose));
            if (closeAborted == null) throw new ArgumentNullException(nameof(closeAborted));
            if (destroyed == null) throw new ArgumentNullException(nameof(destroyed));
            if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                throw new InvalidOperationException("A document-bound modeless window cannot attach while BricsCAD is quitting.");

            EnsureDocumentManagerInitialized();

            Entry entry;
            var created = false;
            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out entry!))
                {
                    entry = new Entry(lifecycleDocument, nativeDatabaseIdentity);
                    Entries.Add(nativeDatabaseIdentity, entry);
                    try
                    {
                        entry.AttachNativeHandlers(lifecycleDocument);
                        created = true;
                    }
                    catch
                    {
                        if (entry.DetachNativeHandlersIfSafe())
                            Entries.Remove(nativeDatabaseIdentity);
                        throw;
                    }
                }
            }

            if (!created)
                entry.EnsureLifecycleDocument(lifecycleDocument, replacementAffinity);

            var callbacks = new Callbacks(beginClose, closeAborted, destroyed);
            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out var current) || !ReferenceEquals(current, entry))
                    throw new InvalidOperationException("The document-bound modeless lifecycle changed while registration was being validated.");
                if (entry.CloseStarted)
                    throw new InvalidOperationException("A closing document-bound modeless lifecycle cannot accept new callbacks.");
                entry.Add(callbacks);
                return new Subscription(entry, callbacks);
            }
        }

        internal static void Rebind(
            Document lifecycleDocument,
            IntPtr nativeDatabaseIdentity,
            Func<Document, bool> replacementAffinity)
        {
            if (lifecycleDocument == null) throw new ArgumentNullException(nameof(lifecycleDocument));
            if (nativeDatabaseIdentity == IntPtr.Zero) throw new ArgumentOutOfRangeException(nameof(nativeDatabaseIdentity));
            if (replacementAffinity == null) throw new ArgumentNullException(nameof(replacementAffinity));
            if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                throw new InvalidOperationException("A document-bound modeless lifecycle cannot move wrappers while BricsCAD is quitting.");

            Entry entry;
            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out entry!))
                    throw new InvalidOperationException("No document-bound modeless lifecycle is registered for the replacement wrapper.");
            }

            entry.EnsureLifecycleDocument(lifecycleDocument, replacementAffinity);
        }

        private static void EnsureDocumentManagerInitialized()
        {
            if (Interlocked.CompareExchange(ref _documentManagerInitialized, 1, 0) != 0) return;
            try
            {
                BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
            }
            catch
            {
                Volatile.Write(ref _documentManagerInitialized, 0);
                throw;
            }
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

            Document document;
            try
            {
                document = e.Document;
                if (document == null) return;
            }
            catch
            {
                return;
            }

            if (!TrySnapshotDestroyByLifecycleDocument(document, out var entry, out var callbacks))
            {
                IntPtr identity;
                try
                {
                    if (document.IsDisposed) return;
                    var database = document.Database;
                    if (database == null) return;
                    identity = database.UnmanagedObject;
                    if (identity == IntPtr.Zero) return;
                }
                catch
                {
                    return;
                }

                // A native pointer is only a fallback identity. After a proven managed-wrapper
                // rebind, a late destroy callback from the stale wrapper must not tear down the
                // current lifecycle merely because both wrappers still expose the same database.
                if (HasDifferentLiveLifecycleDocument(document, identity)) return;
                if (!TrySnapshotDestroyByNativeIdentity(identity, out entry, out callbacks)) return;
            }

            foreach (var callback in callbacks)
            {
                try { callback.Destroyed(sender, e); }
                catch { }
            }

            lock (Gate)
            {
                if (Entries.TryGetValue(entry.NativeDatabaseIdentity, out var current) && ReferenceEquals(current, entry))
                {
                    Entries.Remove(entry.NativeDatabaseIdentity);
                    entry.ClearCallbacks();
                }
            }
        }

        private static bool HasDifferentLiveLifecycleDocument(Document destroyingDocument, IntPtr nativeDatabaseIdentity)
        {
            Document lifecycleDocument;
            Entry entry;
            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out entry!)) return false;
                lifecycleDocument = entry.LifecycleDocument;
                if (ReferenceEquals(lifecycleDocument, destroyingDocument)) return false;
            }

            try
            {
                if (lifecycleDocument.IsDisposed) return false;
                var database = lifecycleDocument.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero || database.UnmanagedObject != nativeDatabaseIdentity)
                    return false;

                foreach (Document candidate in BcadApplication.DocumentManager)
                {
                    if (candidate == null || candidate.IsDisposed) continue;
                    if (!ReferenceEquals(candidate, lifecycleDocument)) continue;
                    lock (Gate)
                    {
                        if (Entries.TryGetValue(nativeDatabaseIdentity, out var current) &&
                            ReferenceEquals(current, entry) &&
                            ReferenceEquals(entry.LifecycleDocument, lifecycleDocument))
                        {
                            entry.ForgetPendingNativeDetachAfterDestroy(destroyingDocument);
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TrySnapshotDestroyByLifecycleDocument(Document document, out Entry entry, out List<Callbacks> callbacks)
        {
            lock (Gate)
            {
                foreach (var candidate in Entries.Values)
                {
                    if (!ReferenceEquals(candidate.LifecycleDocument, document)) continue;
                    candidate.MarkCloseStarted();
                    entry = candidate;
                    callbacks = candidate.SnapshotLiveCallbacks();
                    return true;
                }
            }
            entry = null!;
            callbacks = null!;
            return false;
        }

        private static bool TrySnapshotDestroyByNativeIdentity(IntPtr nativeDatabaseIdentity, out Entry entry, out List<Callbacks> callbacks)
        {
            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out entry!))
                {
                    callbacks = null!;
                    return false;
                }
                entry.MarkCloseStarted();
                callbacks = entry.SnapshotLiveCallbacks();
                return true;
            }
        }

        private static void Unregister(Entry entry, Callbacks callbacks)
        {
            lock (Gate)
            {
                entry.Remove(callbacks);
                if (entry.HasLiveCallbacks || entry.CloseStarted) return;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                if (!entry.DetachNativeHandlersIfSafe()) return;
                if (Entries.TryGetValue(entry.NativeDatabaseIdentity, out var current) && ReferenceEquals(current, entry))
                    Entries.Remove(entry.NativeDatabaseIdentity);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Entry? _entry;
            private Callbacks? _callbacks;
            public Subscription(Entry entry, Callbacks callbacks)
            {
                _entry = entry;
                _callbacks = callbacks;
            }
            public void Dispose()
            {
                var entry = Interlocked.Exchange(ref _entry, null);
                var callbacks = Interlocked.Exchange(ref _callbacks, null);
                if (entry == null || callbacks == null) return;
                Unregister(entry, callbacks);
            }
        }

        private sealed class Callbacks
        {
            public Callbacks(BeginCloseCallback beginClose, CloseAbortedCallback closeAborted, DestroyedCallback destroyed)
            {
                BeginClose = beginClose;
                CloseAborted = closeAborted;
                Destroyed = destroyed;
            }
            public BeginCloseCallback BeginClose { get; }
            public CloseAbortedCallback CloseAborted { get; }
            public DestroyedCallback Destroyed { get; }
        }

        private sealed class Entry
        {
            private Document _lifecycleDocument;
            private readonly List<Document> _pendingNativeDetachDocuments = new List<Document>();
            private readonly List<WeakReference<Callbacks>> _callbacks = new List<WeakReference<Callbacks>>();
            private bool _nativeHandlersAttached;

            public Entry(Document lifecycleDocument, IntPtr nativeDatabaseIdentity)
            {
                _lifecycleDocument = lifecycleDocument;
                NativeDatabaseIdentity = nativeDatabaseIdentity;
            }

            public Document LifecycleDocument => _lifecycleDocument;
            public IntPtr NativeDatabaseIdentity { get; }
            public bool CloseStarted { get; private set; }

            public bool HasLiveCallbacks
            {
                get
                {
                    PruneDeadCallbacks();
                    return _callbacks.Count != 0;
                }
            }

            public void EnsureLifecycleDocument(Document lifecycleDocument, Func<Document, bool> replacementAffinity)
            {
                Document previousDocument;
                lock (Gate)
                {
                    if (!Entries.TryGetValue(NativeDatabaseIdentity, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("The document-bound modeless lifecycle is no longer registered.");
                    if (!TryClearPendingNativeDetaches())
                        throw new InvalidOperationException("A previous modeless lifecycle handler rollback is still pending.");
                    EnsureCurrentNativeHandlersAttached();
                    if (ReferenceEquals(_lifecycleDocument, lifecycleDocument)) return;
                    if (CloseStarted)
                        throw new InvalidOperationException("A closing document-bound modeless lifecycle cannot move to another managed wrapper.");
                    if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                        throw new InvalidOperationException("A document-bound modeless lifecycle cannot move wrappers while BricsCAD is quitting.");
                    previousDocument = _lifecycleDocument;
                }

                if (!replacementAffinity(lifecycleDocument))
                    throw new InvalidOperationException("The replacement BricsCAD document wrapper does not match the bound project/drawing affinity.");

                lock (Gate)
                {
                    if (!Entries.TryGetValue(NativeDatabaseIdentity, out var current) || !ReferenceEquals(current, this))
                        throw new InvalidOperationException("The document-bound modeless lifecycle changed while wrapper affinity was being validated.");
                    if (!TryClearPendingNativeDetaches())
                        throw new InvalidOperationException("A previous modeless lifecycle handler rollback is still pending.");
                    EnsureCurrentNativeHandlersAttached();
                    if (ReferenceEquals(_lifecycleDocument, lifecycleDocument)) return;
                    if (!ReferenceEquals(_lifecycleDocument, previousDocument))
                        throw new InvalidOperationException("The document-bound modeless lifecycle moved to another wrapper while affinity was being validated.");
                    if (CloseStarted)
                        throw new InvalidOperationException("A closing document-bound modeless lifecycle cannot move to another managed wrapper.");
                    if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                        throw new InvalidOperationException("A document-bound modeless lifecycle cannot move wrappers while BricsCAD is quitting.");

                    AttachNativeHandlers(lifecycleDocument);
                    if (!TryDetachNativeHandlers(previousDocument))
                    {
                        if (!TryDetachNativeHandlers(lifecycleDocument))
                            RememberPendingNativeDetach(lifecycleDocument);
                        throw new InvalidOperationException("The obsolete BricsCAD document wrapper could not release its modeless lifecycle handlers.");
                    }
                    _lifecycleDocument = lifecycleDocument;
                    _nativeHandlersAttached = true;
                }
            }

            private void EnsureCurrentNativeHandlersAttached()
            {
                if (_nativeHandlersAttached) return;
                AttachNativeHandlers(_lifecycleDocument);
            }

            public void AttachNativeHandlers(Document lifecycleDocument)
            {
                if (!TryClearPendingNativeDetaches())
                    throw new InvalidOperationException("A previous modeless lifecycle handler rollback is still pending.");
                lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;
                try
                {
                    lifecycleDocument.CloseAborted += OnDocumentCloseAborted;
                }
                catch
                {
                    try { lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose; }
                    catch { RememberPendingNativeDetach(lifecycleDocument); }
                    throw;
                }
                _nativeHandlersAttached = true;
            }

            private bool TryDetachNativeHandlers(Document lifecycleDocument)
            {
                var beginDetached = false;
                try
                {
                    lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;
                    beginDetached = true;
                    lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;
                    ForgetPendingNativeDetach(lifecycleDocument);
                    return true;
                }
                catch
                {
                    if (beginDetached)
                    {
                        try { lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose; }
                        catch { RememberPendingNativeDetach(lifecycleDocument); }
                    }
                    else
                    {
                        RememberPendingNativeDetach(lifecycleDocument);
                    }
                    return false;
                }
            }

            private void RememberPendingNativeDetach(Document lifecycleDocument)
            {
                foreach (var pending in _pendingNativeDetachDocuments)
                {
                    if (ReferenceEquals(pending, lifecycleDocument)) return;
                }
                _pendingNativeDetachDocuments.Add(lifecycleDocument);
            }

            private void ForgetPendingNativeDetach(Document lifecycleDocument)
            {
                for (var index = _pendingNativeDetachDocuments.Count - 1; index >= 0; index--)
                {
                    if (ReferenceEquals(_pendingNativeDetachDocuments[index], lifecycleDocument))
                        _pendingNativeDetachDocuments.RemoveAt(index);
                }
            }

            public void ForgetPendingNativeDetachAfterDestroy(Document destroyedDocument)
            {
                ForgetPendingNativeDetach(destroyedDocument);
            }

            private bool TryClearPendingNativeDetaches()
            {
                if (_pendingNativeDetachDocuments.Count == 0) return true;
                var pendingDocuments = _pendingNativeDetachDocuments.ToArray();
                foreach (var pending in pendingDocuments)
                {
                    if (!TryDetachNativeHandlers(pending)) return false;
                    if (ReferenceEquals(pending, _lifecycleDocument)) _nativeHandlersAttached = false;
                }
                return _pendingNativeDetachDocuments.Count == 0;
            }

            public bool DetachNativeHandlersIfSafe()
            {
                if (CloseStarted) return false;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;
                if (!TryClearPendingNativeDetaches()) return false;
                if (!_nativeHandlersAttached) return true;
                var lifecycleDocument = _lifecycleDocument;
                if (!TryDetachNativeHandlers(lifecycleDocument)) return false;
                _nativeHandlersAttached = false;
                return true;
            }

            public void Add(Callbacks callbacks)
            {
                PruneDeadCallbacks();
                _callbacks.Add(new WeakReference<Callbacks>(callbacks));
            }
            public void Remove(Callbacks callbacks)
            {
                for (var index = _callbacks.Count - 1; index >= 0; index--)
                {
                    if (!_callbacks[index].TryGetTarget(out var candidate) || ReferenceEquals(candidate, callbacks))
                        _callbacks.RemoveAt(index);
                }
            }
            public void ClearCallbacks() => _callbacks.Clear();
            public void MarkCloseStarted() => CloseStarted = true;
            public List<Callbacks> SnapshotLiveCallbacks()
            {
                var live = new List<Callbacks>(_callbacks.Count);
                for (var index = _callbacks.Count - 1; index >= 0; index--)
                {
                    if (_callbacks[index].TryGetTarget(out var callback)) live.Add(callback);
                    else _callbacks.RemoveAt(index);
                }
                live.Reverse();
                return live;
            }
            private void PruneDeadCallbacks()
            {
                for (var index = _callbacks.Count - 1; index >= 0; index--)
                {
                    if (!_callbacks[index].TryGetTarget(out _)) _callbacks.RemoveAt(index);
                }
            }

            private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                List<Callbacks> callbacks;
                lock (Gate)
                {
                    if (!ReferenceEquals(sender, _lifecycleDocument)) return;
                    CloseStarted = true;
                    callbacks = SnapshotLiveCallbacks();
                }
                foreach (var callback in callbacks)
                {
                    try { callback.BeginClose(sender, e); }
                    catch { }
                }
            }

            private void OnDocumentCloseAborted(object? sender, EventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;
                List<Callbacks> callbacks;
                lock (Gate)
                {
                    if (!ReferenceEquals(sender, _lifecycleDocument)) return;
                    CloseStarted = false;
                    callbacks = SnapshotLiveCallbacks();
                }
                foreach (var callback in callbacks)
                {
                    try { callback.CloseAborted(sender, e); }
                    catch { }
                }
            }
        }
    }
}
