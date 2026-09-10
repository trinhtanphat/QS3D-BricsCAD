using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;

namespace QS3D.BricsCAD.V25
{
    internal static class SelectionSyncCoordinator
    {
        private sealed class NativeSelectionSubscription
        {
            internal NativeSelectionSubscription(Document document, object token, EventHandler handler)
            {
                Document = document;
                Token = token;
                Handler = handler;
            }

            internal readonly Document Document;
            internal readonly object Token;
            internal readonly EventHandler Handler;
            internal bool MayBeSubscribed;
            private bool DetachRequested;
            private bool DetachInProgress;
        }

        private static readonly HashSet<Document> Attached = new HashSet<Document>();
        private static readonly Dictionary<Document, object> AttachmentTokens = new Dictionary<Document, object>();
        private static readonly Dictionary<Document, EventHandler> AttachmentHandlers = new Dictionary<Document, EventHandler>();
        private static readonly Dictionary<object, NativeSelectionSubscription> NativeSubscriptions = new Dictionary<object, NativeSelectionSubscription>();
        private static readonly Dictionary<Document, object> Refreshing = new Dictionary<Document, object>();
        private static readonly Dictionary<Document, DispatcherTimer> Pending = new Dictionary<Document, DispatcherTimer>();
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(80d);

        public static void Attach(Document? document)
        {
            RetryPendingDetaches();
            if (document == null || Attached.Contains(document)) return;

            var attachmentToken = new object();
            EventHandler attachmentHandler = (_, __) => OnImpliedSelectionChanged(document, attachmentToken);
            var subscription = new NativeSelectionSubscription(document, attachmentToken, attachmentHandler);
            try
            {
                if (!Attached.Add(document)) return;
                AttachmentTokens[document] = attachmentToken;
                AttachmentHandlers[document] = attachmentHandler;
                NativeSubscriptions[attachmentToken] = subscription;

                // Publish native ownership before crossing the add accessor. BricsCAD can fail after
                // partially registering a managed delegate; conservative ownership lets rollback or a
                // retained callback compensate without ever granting that generation UI authority again.
                subscription.MayBeSubscribed = true;
                document.ImpliedSelectionChanged += attachmentHandler;
                if (!IsCurrentAttachment(document, attachmentToken))
                {
                    RequestDetach(subscription);
                    return;
                }
                Refresh(document, attachmentToken);
            }
            catch
            {
                RollbackAttachment(document, attachmentToken, attachmentHandler, subscription);
                throw;
            }
        }

        public static void Detach(Document? document)
        {
            RetryPendingDetaches();
            if (document == null || !Attached.Contains(document)) return;

            AttachmentTokens.TryGetValue(document, out var attachmentToken);
            AttachmentHandlers.TryGetValue(document, out var attachmentHandler);
            NativeSelectionSubscription? subscription = null;
            if (attachmentToken != null)
                NativeSubscriptions.TryGetValue(attachmentToken, out subscription);

            // Revoke active modeless/UI authority before crossing the fallible native remove boundary.
            // Native ownership is generation-keyed separately, so a failed detach cannot be forgotten
            // and cannot overwrite a later reattachment of the same Document wrapper.
            RemovePending(document);
            Refreshing.Remove(document);
            AttachmentHandlers.Remove(document);
            AttachmentTokens.Remove(document);
            Attached.Remove(document);

            if (subscription != null)
                RequestDetach(subscription);
            else if (attachmentHandler != null)
            {
                // Legacy/inconsistent ownership is best-effort only. Normal generations always have
                // a NativeSelectionSubscription before native add is attempted.
                try { document.ImpliedSelectionChanged -= attachmentHandler; }
                catch { }
            }
        }

        public static void DetachByName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            foreach (var document in Attached.Where(x => string.Equals(x.Name, fileName, StringComparison.OrdinalIgnoreCase)).ToArray()) Detach(document);
        }

        // Explicit lifecycle/UI callers do not own a historical generation token. Capture the current
        // attachment once at this boundary, then delegate all native/modeless work to the exact-token
        // overload. Queued callbacks never use this overload and therefore cannot recapture a newer
        // generation after detach -> reattach of the same native Document wrapper.
        public static void Refresh(Document? document)
        {
            if (document == null ||
                !AttachmentTokens.TryGetValue(document, out var attachmentToken)) return;
            Refresh(document, attachmentToken);
        }

        public static void Refresh(Document? document, object attachmentToken)
        {
            if (document == null ||
                !IsCurrentAttachment(document, attachmentToken) ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            if (!PaletteCoordinator.IsWorkspaceVisible) return;
            RemovePending(document);
            if (Refreshing.ContainsKey(document)) return;
            Refreshing[document] = attachmentToken;
            try
            {
                // Palette creation and native selection capture may pump modeless/document callbacks.
                // Preserve the attachment identity captured at entry so detach -> reattach of the same
                // Document wrapper cannot give this older invocation authority over the new attachment.
                PaletteCoordinator.EnsureCreated();
                var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);
                if (!IsCurrentAttachment(document, attachmentToken) ||
                    !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
                PaletteCoordinator.SetInspection(snapshots);
            }
            catch (Exception)
            {
                if (IsCurrentAttachment(document, attachmentToken))
                {
                    SelectionSyncStatusPublisher.SetStatusForDocument(document, "Selection sync lỗi. Vui lòng thử lại.");
                }
            }
            finally { ReleaseRefresh(document, attachmentToken); }
        }

        public static void Stop()
        {
            foreach (var document in Attached.ToArray()) Detach(document);
            RetryPendingDetaches();
            foreach (var timer in Pending.Values.ToArray()) timer.Stop();
            Pending.Clear();
            Refreshing.Clear();
            AttachmentHandlers.Clear();
            AttachmentTokens.Clear();
            Attached.Clear();
            // Do not clear NativeSubscriptions here. A native remove that is temporarily rejected
            // still owns its exact delegate/document pair; a retained callback can safely retry later.
        }

        private static void RollbackAttachment(
            Document document,
            object attachmentToken,
            EventHandler attachmentHandler,
            NativeSelectionSubscription subscription)
        {
            var ownsCurrentToken =
                AttachmentTokens.TryGetValue(document, out var currentToken) &&
                ReferenceEquals(currentToken, attachmentToken);
            var ownsCurrentHandler =
                AttachmentHandlers.TryGetValue(document, out var currentHandler) &&
                ReferenceEquals(currentHandler, attachmentHandler);

            // Roll back active authority only for this exact generation. A reentrant replacement may
            // already own the Document and must not be torn down by an older add/refresh failure.
            if (ownsCurrentToken && ownsCurrentHandler)
            {
                RemovePending(document);
                ReleaseRefresh(document, attachmentToken);
                AttachmentHandlers.Remove(document);
                AttachmentTokens.Remove(document);
                Attached.Remove(document);
            }

            RequestDetach(subscription);
        }

        private static void ReleaseRefresh(Document document, object attachmentToken)
        {
            if (!Refreshing.TryGetValue(document, out var currentToken) ||
                !ReferenceEquals(currentToken, attachmentToken)) return;
            Refreshing.Remove(document);
        }

        private static void OnImpliedSelectionChanged(Document document, object attachmentToken)
        {
            if (NativeSubscriptions.TryGetValue(attachmentToken, out var subscription) &&
                subscription.DetachRequested)
            {
                TryDetachSubscription(subscription);
                return;
            }

            if (!IsCurrentAttachment(document, attachmentToken) ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            ScheduleRefresh(document, attachmentToken);
        }

        private static void ScheduleRefresh(Document document, object attachmentToken)
        {
            if (!IsCurrentAttachment(document, attachmentToken))
            {
                RemovePending(document);
                return;
            }

            if (!PaletteCoordinator.IsWorkspaceVisible)
            {
                StopPending(document);
                return;
            }

            if (!Pending.TryGetValue(document, out var timer))
            {
                timer = new DispatcherTimer { Interval = RefreshDelay };
                timer.Tick += (_, __) =>
                {
                    timer.Stop();
                    if (!Pending.TryGetValue(document, out var current) ||
                        !ReferenceEquals(current, timer))
                    {
                        return;
                    }
                    Pending.Remove(document);
                    if (!IsCurrentAttachment(document, attachmentToken)) return;
                    Refresh(document, attachmentToken);
                };
                Pending[document] = timer;
            }
            else
            {
                timer.Stop();
            }
            timer.Start();
        }

        private static void RequestDetach(NativeSelectionSubscription subscription)
        {
            subscription.DetachRequested = true;
            TryDetachSubscription(subscription);
        }

        private static void TryDetachSubscription(NativeSelectionSubscription subscription)
        {
            if (!subscription.MayBeSubscribed || subscription.DetachInProgress) return;

            subscription.DetachInProgress = true;
            try
            {
                subscription.Document.ImpliedSelectionChanged -= subscription.Handler;
                subscription.MayBeSubscribed = false;
                if (NativeSubscriptions.TryGetValue(subscription.Token, out var current) &&
                    ReferenceEquals(current, subscription))
                {
                    NativeSubscriptions.Remove(subscription.Token);
                }
            }
            catch
            {
                // Native document teardown can reject removal temporarily. Keep exact generation
                // ownership published; a later lifecycle pass or retained callback will retry.
            }
            finally
            {
                subscription.DetachInProgress = false;
            }
        }

        private static void RetryPendingDetaches()
        {
            foreach (var subscription in NativeSubscriptions.Values.Where(x => x.DetachRequested).ToArray())
                TryDetachSubscription(subscription);
        }

        private static bool IsCurrentAttachment(Document document, object attachmentToken)
        {
            return Attached.Contains(document) &&
                   AttachmentTokens.TryGetValue(document, out var currentToken) &&
                   ReferenceEquals(currentToken, attachmentToken);
        }

        private static void StopPending(Document document)
        {
            if (!Pending.TryGetValue(document, out var timer)) return;
            timer.Stop();
        }

        private static void RemovePending(Document document)
        {
            if (!Pending.TryGetValue(document, out var timer)) return;
            timer.Stop();
            Pending.Remove(document);
        }
    }
}
