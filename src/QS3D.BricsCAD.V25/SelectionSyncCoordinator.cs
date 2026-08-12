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
        private static readonly HashSet<Document> Refreshing = new HashSet<Document>();
        private static readonly Dictionary<Document, DispatcherTimer> Pending = new Dictionary<Document, DispatcherTimer>();
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(80d);

        public static void Attach(Document? document)
        {
            if (document == null || Attached.Contains(document)) return;
            var subscribed = false;
            try
            {
                document.ImpliedSelectionChanged += OnImpliedSelectionChanged;
                subscribed = true;
                if (!Attached.Add(document))
                {
                    document.ImpliedSelectionChanged -= OnImpliedSelectionChanged;
                    return;
                }
                Refresh(document);
            }
            catch
            {
                if (subscribed)
                {
                    try { document.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }
                    catch { }
                }
                RemovePending(document);
                Refreshing.Remove(document);
                Attached.Remove(document);
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
            Attached.Remove(document);
        }

        public static void DetachByName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            foreach (var document in Attached.Where(x => string.Equals(x.Name, fileName, StringComparison.OrdinalIgnoreCase)).ToArray()) Detach(document);
        }

        public static void Refresh(Document? document)
        {
            if (document == null || !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            if (!PaletteCoordinator.IsWorkspaceVisible) return;
            StopPending(document);
            if (!Refreshing.Add(document)) return;
            try { PaletteCoordinator.SetInspection(EntitySnapshotReader.ReadImpliedSelection(document)); }
            catch (Exception ex) { PaletteCoordinator.SetStatus("Selection sync lỗi: " + ex.Message); }
            finally { Refreshing.Remove(document); }
        }

        public static void Stop()
        {
            foreach (var document in Attached.ToArray()) Detach(document);
            foreach (var timer in Pending.Values.ToArray()) timer.Stop();
            Pending.Clear();
            Refreshing.Clear();
        }

        private static void OnImpliedSelectionChanged(object sender, EventArgs e)
        {
            var document = sender as Document ?? Application.DocumentManager.MdiActiveDocument;
            if (document == null || !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)) return;
            ScheduleRefresh(document);
        }

        private static void ScheduleRefresh(Document document)
        {
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
