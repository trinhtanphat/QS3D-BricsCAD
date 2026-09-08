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
        private static readonly HashSet<Document> Attached = new HashSet<Document>();
        private static readonly Dictionary<Document, object> AttachmentTokens = new Dictionary<Document, object>();
        private static readonly Dictionary<Document, EventHandler> AttachmentHandlers = new Dictionary<Document, EventHandler>();
        private static readonly Dictionary<Document, object> Refreshing = new Dictionary<Document, object>();
        private static readonly Dictionary<Document, DispatcherTimer> Pending = new Dictionary<Document, DispatcherTimer>();
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(80d);

        public static void Attach(Document? document)
        {
            if (document == null || Attached.Contains(document)) return;
            var attachmentToken = new object();
            EventHandler attachmentHandler = (_, __) => OnImpliedSelectionChanged(document, attachmentToken);
            var subscribed = false;
            try
            {
                if (!Attached.Add(document)) return;
                AttachmentTokens[document] = attachmentToken;
                AttachmentHandlers[document] = attachmentHandler;

                // Publish exact generation ownership before entering the native subscription boundary.
                // The generation-specific delegate captures document + token so stale callbacks cannot
                // act for a later attachment that happens to reuse the same native Document wrapper.
                subscribed = true;
                document.ImpliedSelectionChanged += attachmentHandler;
                if (!IsCurrentAttachment(document, attachmentToken))
                {
                    try { document.ImpliedSelectionChanged -= attachmentHandler; }
                    catch { }
                    return;
                }
                Refresh(document);
            }
            catch
            {
                RollbackAttachment(document, subscribed, attachmentToken, attachmentHandler);
                throw;
            }
        }

        public static void Detach(Document? document)
        {
            if (document == null || !Attached.Contains(document)) return;
            AttachmentHandlers.TryGetValue(document, out var attachmentHandler);

            // Unpublish current ownership before crossing the native unsubscribe boundary. A nested
            // reattach may now publish a new generation, while this teardown retains only its exact
            // generation-specific handler and therefore cannot unsubscribe the replacement.
            RemovePending(document);
            Refreshing.Remove(document);
            AttachmentHandlers.Remove(document);
            AttachmentTokens.Remove(document);
            Attached.Remove(document);

            if (attachmentHandler != null)
            {
                try { document.ImpliedSelectionChanged -= attachmentHandler; }
                catch { }
            }
        }

        public static void DetachByName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            foreach (var document in Attached.Where(x => string.Equals(x.Name, fileName, StringComparison.OrdinalIgnoreCase)).ToArray()) Detach(document);
        }

        public static void Refresh(Document? document)
        {
            if (document == null ||
                !Attached.Contains(document) ||
                !AttachmentTokens.TryGetValue(document, out var attachmentToken) ||
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
            foreach (var timer in Pending.Values.ToArray()) timer.Stop();
            Pending.Clear();
            Refreshing.Clear();
            AttachmentHandlers.Clear();
            AttachmentTokens.Clear();
        }

        private static void RollbackAttachment(Document document, bool subscribed, object attachmentToken, EventHandler attachmentHandler)
        {
            if (AttachmentTokens.TryGetValue(document, out var currentToken) &&
                !ReferenceEquals(currentToken, attachmentToken))
            {
                return;
            }
            if (AttachmentHandlers.TryGetValue(document, out var currentHandler) &&
                !ReferenceEquals(currentHandler, attachmentHandler))
            {
                return;
            }

            RemovePending(document);
            ReleaseRefresh(document, attachmentToken);
            AttachmentHandlers.Remove(document);
            AttachmentTokens.Remove(document);
            Attached.Remove(document);

            if (subscribed)
            {
                try { document.ImpliedSelectionChanged -= attachmentHandler; }
                catch { }
            }
        }

        private static void ReleaseRefresh(Document document, object attachmentToken)
        {
            if (!Refreshing.TryGetValue(document, out var currentToken) ||
                !ReferenceEquals(currentToken, attachmentToken)) return;
            Refreshing.Remove(document);
        }

        private static void OnImpliedSelectionChanged(Document document, object attachmentToken)
        {
            if (!IsCurrentAttachment(document, attachmentToken) ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            ScheduleRefresh(document);
        }

        private static void ScheduleRefresh(Document document)
        {
            if (!Attached.Contains(document))
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
                    if (!Attached.Contains(document)) return;
                    Refresh(document);
                };
                Pending[document] = timer;
            }
            else
            {
                timer.Stop();
            }
            timer.Start();
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
