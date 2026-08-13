using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Bridges Source Reconcile's in-memory semantic transaction to BricsCAD's
    /// native Undo stack. A small revision marker is written to Model Space in
    /// the same native transaction as generated-output invalidation. BricsCAD
    /// therefore restores that marker together with the CAD entities, and the
    /// command-end observer restores the matching in-session semantic snapshot.
    /// </summary>
    internal static class SourceReconcileUndoCoordinator
    {
        private const string RegAppName = "QS3D_SRC_SYNC_UNDO";
        private const string MarkerVersion = "1";
        private const int MaxSnapshotsPerDocument = 128;
        private static readonly object Gate = new object();
        private static readonly Dictionary<Document, CommandEventHandler> CommandEndedHandlers =
            new Dictionary<Document, CommandEventHandler>();
        private static readonly Dictionary<Document, DocumentHistory> Histories =
            new Dictionary<Document, DocumentHistory>();

        internal sealed class PendingTransition : IDisposable
        {
            private readonly Document _document;
            private readonly DocumentHistory _history;
            private readonly string _previousRevision;
            private readonly string _nextRevision;
            private HistoryEntry? _previousNextEntry;
            private bool _staged;
            private bool _committed;
            private bool _disposed;

            internal PendingTransition(
                Document document,
                DocumentHistory history,
                string previousRevision,
                string nextRevision)
            {
                _document = document;
                _history = history;
                _previousRevision = previousRevision;
                _nextRevision = nextRevision;
            }

            public void StageAfter(ProjectState project, ProjectStateSnapshot snapshot)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

                lock (Gate)
                {
                    ThrowIfDisposed();
                    if (_staged) throw new InvalidOperationException("Source Reconcile Undo transition is already staged.");
                    RequireCurrentHistory(_document, _history, project, _previousRevision);

                    _history.Entries.TryGetValue(_nextRevision, out _previousNextEntry);
                    _history.Entries[_nextRevision] = new HistoryEntry(snapshot, ProjectRevisionStamp.Capture(project));
                    _history.CurrentRevision = _nextRevision;
                    _staged = true;
                }
            }

            public void ConfirmCommitted()
            {
                // StageAfter performs every allocation/validation before the CAD
                // commit. This post-commit acknowledgement is intentionally a
                // no-allocation state flip so a valid native commit cannot be
                // reported as failed by Undo bookkeeping.
                _committed = true;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (!_staged || _committed) return;

                lock (Gate)
                {
                    if (!Histories.TryGetValue(_document, out var current) || !ReferenceEquals(current, _history)) return;
                    if (_previousNextEntry == null) _history.Entries.Remove(_nextRevision);
                    else _history.Entries[_nextRevision] = _previousNextEntry;
                    _history.CurrentRevision = _previousRevision;
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(PendingTransition));
            }
        }

        internal sealed class DocumentHistory
        {
            public DocumentHistory(Document document, ProjectState project, string revision)
            {
                Document = document;
                Project = project;
                ProjectId = project.ProjectId;
                CurrentRevision = revision;
            }

            public Document Document { get; }
            public ProjectState Project { get; }
            public string ProjectId { get; }
            public string CurrentRevision { get; set; }
            public bool Desynchronized { get; set; }
            public Dictionary<string, HistoryEntry> Entries { get; } =
                new Dictionary<string, HistoryEntry>(StringComparer.Ordinal);
        }

        internal sealed class HistoryEntry
        {
            public HistoryEntry(ProjectStateSnapshot snapshot, ProjectRevisionStamp stamp)
            {
                Snapshot = snapshot;
                Stamp = stamp;
            }

            public ProjectStateSnapshot Snapshot { get; }
            public ProjectRevisionStamp Stamp { get; }
        }

        internal readonly struct ProjectRevisionStamp
        {
            private ProjectRevisionStamp(string projectId, long changeVersion, long updatedUtcTicks)
            {
                ProjectId = projectId;
                ChangeVersion = changeVersion;
                UpdatedUtcTicks = updatedUtcTicks;
            }

            public string ProjectId { get; }
            public long ChangeVersion { get; }
            public long UpdatedUtcTicks { get; }

            public static ProjectRevisionStamp Capture(ProjectState project)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                return new ProjectRevisionStamp(project.ProjectId, project.ChangeVersion, project.UpdatedUtc.Ticks);
            }

            public bool Matches(ProjectState project)
            {
                return project != null &&
                    string.Equals(ProjectId, project.ProjectId, StringComparison.Ordinal) &&
                    ChangeVersion == project.ChangeVersion &&
                    UpdatedUtcTicks == project.UpdatedUtc.Ticks;
            }
        }

        public static void Attach(Document? document)
        {
            if (document == null) return;
            lock (Gate)
            {
                if (CommandEndedHandlers.ContainsKey(document)) return;
                CommandEventHandler handler = (sender, args) => OnCommandEnded(document);
                document.CommandEnded += handler;
                CommandEndedHandlers.Add(document, handler);
            }
        }

        public static void Detach(Document? document)
        {
            if (document == null) return;
            CommandEventHandler? handler = null;
            lock (Gate)
            {
                if (CommandEndedHandlers.TryGetValue(document, out handler))
                    CommandEndedHandlers.Remove(document);
                Histories.Remove(document);
            }
            if (handler != null)
            {
                try { document.CommandEnded -= handler; }
                catch { }
            }
        }

        public static void Stop()
        {
            Document[] documents;
            lock (Gate) documents = new List<Document>(CommandEndedHandlers.Keys).ToArray();
            foreach (var document in documents) Detach(document);
        }

        public static void Forget(Document? document)
        {
            if (document == null) return;
            lock (Gate) Histories.Remove(document);
        }

        public static PendingTransition BeginTransition(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectStateSnapshot beforeSnapshot)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (beforeSnapshot == null) throw new ArgumentNullException(nameof(beforeSnapshot));

            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Source Reconcile Undo registration");
            Attach(document);

            var modelSpace = OpenModelSpace(document.Database, transaction, OpenMode.ForWrite);
            var previousRevision = ReadRevision(modelSpace);
            DocumentHistory history;
            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out history))
                {
                    history = new DocumentHistory(document, project, previousRevision);
                    history.Entries.Add(previousRevision, new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project)));
                    Histories.Add(document, history);
                }
                else
                {
                    RequireCurrentHistory(document, history, project, previousRevision);
                    if (!history.Entries.TryGetValue(previousRevision, out var currentEntry) || !currentEntry.Stamp.Matches(project))
                    {
                        // An intervening semantic-only operation is not represented
                        // in BricsCAD's native Undo stack. Rebase the current native
                        // marker to the freshly captured pre-command project so Undo
                        // of this new reconcile preserves those intervening edits.
                        // Older native marker revisions deliberately become unknown
                        // and will fail closed instead of restoring a stale snapshot.
                        history.Entries.Clear();
                        history.Entries.Add(previousRevision, new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project)));
                    }
                }

                if (history.Entries.Count >= MaxSnapshotsPerDocument)
                    throw new InvalidOperationException(
                        "Source Reconcile Undo history reached its safe in-session limit. Save, close and reopen the drawing before reconciling again.");
            }

            var nextRevision = "SRU1:" + Guid.NewGuid().ToString("N");
            EnsureRegApp(document.Database, transaction);
            using (var marker = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, nextRevision)))
                modelSpace.XData = marker;

            return new PendingTransition(document, history, previousRevision, nextRevision);
        }

        private static void OnCommandEnded(Document document)
        {
            DocumentHistory history;
            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out history) || history.Desynchronized) return;
            }

            try
            {
                string nativeRevision;
                try { nativeRevision = ReadRevision(document); }
                catch (Exception markerError)
                {
                    throw MarkDesynchronized(
                        history,
                        "Source Reconcile native Undo marker could not be read. Reload the project before further mutation.",
                        markerError);
                }
                HistoryEntry targetEntry;
                HistoryEntry currentEntry;
                lock (Gate)
                {
                    if (!Histories.TryGetValue(document, out var currentHistory) || !ReferenceEquals(currentHistory, history)) return;
                    if (string.Equals(nativeRevision, history.CurrentRevision, StringComparison.Ordinal)) return;
                    if (!history.Entries.TryGetValue(nativeRevision, out targetEntry) ||
                        !history.Entries.TryGetValue(history.CurrentRevision, out currentEntry))
                    {
                        history.Desynchronized = true;
                        throw new InvalidOperationException(
                            "Source Reconcile native Undo reached a revision that is not available in this plugin session. Reload the project before further mutation.");
                    }
                }

                if (!ProjectContextCoordinator.TryGetCached(document, out var project) ||
                    !ReferenceEquals(project, history.Project) ||
                    !string.Equals(project.ProjectId, history.ProjectId, StringComparison.Ordinal))
                    throw MarkDesynchronized(history,
                        "Source Reconcile native Undo cannot target a missing or replaced canonical project. Reload the project before further mutation.");

                try { ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Source Reconcile native Undo"); }
                catch (Exception backingStoreError)
                {
                    throw MarkDesynchronized(
                        history,
                        "Source Reconcile native Undo was refused because the project backing store changed. Reload before continuing.",
                        backingStoreError);
                }
                if (!currentEntry.Stamp.Matches(project))
                    throw MarkDesynchronized(history,
                        "Source Reconcile native Undo was refused because semantic state changed outside its tracked native history. Redo the native change or reload before continuing.");

                var restoreRollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    targetEntry.Snapshot.Restore(project);
                    if (!targetEntry.Stamp.Matches(project))
                        throw new InvalidOperationException("Restored Source Reconcile semantic state does not match its recorded revision.");
                }
                catch (Exception restoreError)
                {
                    try { restoreRollback.Restore(project); }
                    catch (Exception rollbackError)
                    {
                        throw MarkDesynchronized(history,
                            "Source Reconcile semantic Undo restore and recovery both failed.",
                            new AggregateException(restoreError, rollbackError));
                    }
                    throw MarkDesynchronized(history, "Source Reconcile semantic Undo restore failed.", restoreError);
                }

                lock (Gate)
                {
                    if (Histories.TryGetValue(document, out var currentHistory) && ReferenceEquals(currentHistory, history))
                        history.CurrentRevision = nativeRevision;
                }
                RefreshAfterRestore(document);
            }
            catch (Exception error)
            {
                Report(document, "QS3D Source Reconcile Undo sync warning: " + error.Message);
            }
        }

        private static InvalidOperationException MarkDesynchronized(
            DocumentHistory history,
            string message,
            Exception? inner = null)
        {
            lock (Gate) history.Desynchronized = true;
            return inner == null ? new InvalidOperationException(message) : new InvalidOperationException(message, inner);
        }

        private static void RequireCurrentHistory(
            Document document,
            DocumentHistory history,
            ProjectState project,
            string nativeRevision)
        {
            if (history.Desynchronized ||
                !ReferenceEquals(history.Document, document) ||
                !ReferenceEquals(history.Project, project) ||
                !string.Equals(history.ProjectId, project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(history.CurrentRevision, nativeRevision, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Source Reconcile Undo history is not synchronized with this document/project. Reload before reconciling again.");
        }

        private static string ReadRevision(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var modelSpace = OpenModelSpace(document.Database, transaction, OpenMode.ForRead);
                var revision = ReadRevision(modelSpace);
                transaction.Commit();
                return revision;
            }
        }

        private static string ReadRevision(BlockTableRecord modelSpace)
        {
            using (var marker = modelSpace.GetXDataForApplication(RegAppName))
            {
                if (marker == null) return string.Empty;
                var values = marker.AsArray();
                if (values.Length != 3 ||
                    !string.Equals(Convert.ToString(values[0].Value, CultureInfo.InvariantCulture), RegAppName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Convert.ToString(values[1].Value, CultureInfo.InvariantCulture), MarkerVersion, StringComparison.Ordinal))
                    throw new InvalidOperationException("Source Reconcile native Undo marker is malformed.");
                var revision = Convert.ToString(values[2].Value, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(revision) || !revision.StartsWith("SRU1:", StringComparison.Ordinal))
                    throw new InvalidOperationException("Source Reconcile native Undo revision is malformed.");
                return revision;
            }
        }

        private static BlockTableRecord OpenModelSpace(Database database, Transaction transaction, OpenMode mode)
        {
            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], mode);
        }

        private static void EnsureRegApp(Database database, Transaction transaction)
        {
            var table = (RegAppTable)transaction.GetObject(database.RegAppTableId, OpenMode.ForRead);
            if (table.Has(RegAppName)) return;
            table.UpgradeOpen();
            var record = new RegAppTableRecord { Name = RegAppName };
            table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
        }

        private static void RefreshAfterRestore(Document document)
        {
            try
            {
                if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                {
                    PaletteCoordinator.RefreshProject();
                    document.Editor.Regen();
                }
                Report(document, "QS3D Source Reconcile semantic state synchronized with native Undo/Redo.");
            }
            catch (Exception error)
            {
                Report(document, "QS3D Source Reconcile Undo restored; UI sync warning: " + error.Message);
            }
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); }
            catch { }
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
        }
    }
}
