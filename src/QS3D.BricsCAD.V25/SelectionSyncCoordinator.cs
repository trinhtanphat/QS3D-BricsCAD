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
        private static readonly Dictionary<Document, object> Refreshing = new Dictionary<Document, object>();
        private static readonly Dictionary<Document, DispatcherTimer> Pending = new Dictionary<Document, DispatcherTimer>();
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(80d);

        public static void Attach(Document? document)
        {
            if (document == null || Attached.Contains(document)) return;
            var attachmentToken = new object();
            var subscribed = false;
            try
            {
                if (!Attached.Add(document)) return;
                AttachmentTokens[document] = attachmentToken;

                // Publish exact generation ownership before entering the native subscription boundary.
                // If the host pumps detach/reattach callbacks while adding the handler, stale outer
                // cleanup must not gain authority over a newer generation of the same Document wrapper.
                subscribed = true;
                document.ImpliedSelectionChanged += OnImpliedSelectionChanged;
                if (!IsCurrentAttachment(document, attachmentToken)) return;
                Refresh(document);
            }
            catch
            {
                RollbackAttachment(document, subscribed, attachmentToken);
                throw;
            }
        }

        public static void Detach(Document? document)
        {
            if (document == null || !Attached.Contains(document)) return;
            try { document.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }
            catch { }
            RemovePending(document);
            Refreshing.Remove(document);
            AttachmentTokens.Remove(document);
            Attached.Remove(document);
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
            AttachmentTokens.Clear();
        }

        private static void RollbackAttachment(Document document, bool subscribed, object attachmentToken)
        {
            if (AttachmentTokens.TryGetValue(document, out var currentToken) &&
                !ReferenceEquals(currentToken, attachmentToken))
            {
                return;
            }

            if (subscribed)
            {
                try { document.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }
                catch { }
            }
            RemovePending(document);
            ReleaseRefresh(document, attachmentToken);
            AttachmentTokens.Remove(document);
            Attached.Remove(document);
        }

        private static void ReleaseRefresh(Document document, object attachmentToken)
        {
            if (!Refreshing.TryGetValue(document, out var currentToken) ||
                !ReferenceEquals(currentToken, attachmentToken)) return;
            Refreshing.Remove(document);
        }

        private static void OnImpliedSelectionChanged(object sender, EventArgs e)
        {
            var document = sender as Document ?? Application.DocumentManager.MdiActiveDocument;
            if (document == null ||
                !Attached.Contains(document) ||
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
