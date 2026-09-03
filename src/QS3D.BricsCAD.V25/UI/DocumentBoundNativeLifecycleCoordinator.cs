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
            BeginCloseCallback beginClose,
            CloseAbortedCallback closeAborted,
            DestroyedCallback destroyed)
        {
            if (lifecycleDocument == null) throw new ArgumentNullException(nameof(lifecycleDocument));
            if (nativeDatabaseIdentity == IntPtr.Zero) throw new ArgumentOutOfRangeException(nameof(nativeDatabaseIdentity));
            if (beginClose == null) throw new ArgumentNullException(nameof(beginClose));
            if (closeAborted == null) throw new ArgumentNullException(nameof(closeAborted));
            if (destroyed == null) throw new ArgumentNullException(nameof(destroyed));
            if (ModelessHostQuiescenceCoordinator.IsQuiescing)
                throw new InvalidOperationException("A document-bound modeless window cannot attach while BricsCAD is quitting.");

            EnsureDocumentManagerInitialized();

            lock (Gate)
            {
                if (!Entries.TryGetValue(nativeDatabaseIdentity, out var entry))
                {
                    entry = new Entry(lifecycleDocument, nativeDatabaseIdentity);
                    entry.AttachNativeHandlers();
                    Entries.Add(nativeDatabaseIdentity, entry);
                }

                var callbacks = new Callbacks(beginClose, closeAborted, destroyed);
                entry.Add(callbacks);
                return new Subscription(entry, callbacks);
            }
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
            // The managed Document wrapper may already front native state being dismantled. Never
            // dereference it after the global quit boundary; the host owns final destruction then.
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

            // Managed reference identity is safe even after the wrapper reports IsDisposed. Try the
            // exact lifecycle wrapper first so a normal destroy event cannot strand its process-global
            // entry merely because native teardown advanced before DocumentToBeDestroyed was raised.
            if (!TrySnapshotDestroyByLifecycleDocument(document, out var entry, out var callbacks))
            {
                IntPtr identity;
                try
                {
                    // Wrapper drift is still supported, but only a live alternate wrapper may be
                    // dereferenced to recover the stable native database identity.
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

        private static bool TrySnapshotDestroyByLifecycleDocument(
            Document document,
            out Entry entry,
            out List<Callbacks> callbacks)
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

        private static bool TrySnapshotDestroyByNativeIdentity(
            IntPtr nativeDatabaseIdentity,
            out Entry entry,
            out List<Callbacks> callbacks)
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

                if (Entries.TryGetValue(entry.NativeDatabaseIdentity, out var current) && ReferenceEquals(current, entry))
                    Entries.Remove(entry.NativeDatabaseIdentity);
                entry.DetachNativeHandlersIfSafe();
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
            private readonly Document _lifecycleDocument;
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

            public void AttachNativeHandlers()
            {
                if (_nativeHandlersAttached) return;
                var lifecycleDocument = _lifecycleDocument;
                lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;
                try
                {
                    lifecycleDocument.CloseAborted += OnDocumentCloseAborted;
                }
                catch
                {
                    try { lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose; }
                    catch { }
                    throw;
                }
                _nativeHandlersAttached = true;
            }

            public void DetachNativeHandlersIfSafe()
            {
                if (!_nativeHandlersAttached || CloseStarted) return;
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                var lifecycleDocument = _lifecycleDocument;
                try { lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose; }
                catch { }
                try { lifecycleDocument.CloseAborted -= OnDocumentCloseAborted; }
                catch { }
                _nativeHandlersAttached = false;
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
                    if (_callbacks[index].TryGetTarget(out var callback))
                        live.Add(callback);
                    else
                        _callbacks.RemoveAt(index);
                }
                live.Reverse();
                return live;
            }

            private void PruneDeadCallbacks()
            {
                for (var index = _callbacks.Count - 1; index >= 0; index--)
                {
                    if (!_callbacks[index].TryGetTarget(out _))
                        _callbacks.RemoveAt(index);
                }
            }

            private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)
            {
                if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;

                List<Callbacks> callbacks;
                lock (Gate)
                {
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
