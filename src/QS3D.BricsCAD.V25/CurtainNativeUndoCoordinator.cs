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
    /// Couples one committed QS3DCURTAIN3D native generation with the matching
    /// canonical semantic owner snapshot. BricsCAD restores the Model Space
    /// marker with host/frame/panel CAD; the command-end observer then restores
    /// only the known snapshot for the exact cached project and document.
    /// </summary>
    internal static class CurtainNativeUndoCoordinator
    {
        private const string RegAppName = "QS3D_CURTAIN_UNDO";
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
            private readonly HistoryEntry _beforeEntry;
            private readonly bool _rebase;
            private readonly bool _registeredHistory;
            private Dictionary<string, HistoryEntry>? _stagedEntries;
            private bool _markerStaged;
            private bool _nativeCommitted;
            private bool _confirmed;
            private bool _disposed;

            internal PendingTransition(
                Document document,
                DocumentHistory history,
                string previousRevision,
                string nextRevision,
                HistoryEntry beforeEntry,
                bool rebase,
                bool registeredHistory)
            {
                _document = document;
                _history = history;
                _previousRevision = previousRevision;
                _nextRevision = nextRevision;
                _beforeEntry = beforeEntry;
                _rebase = rebase;
                _registeredHistory = registeredHistory;
            }

            public void StageMarker(Database database, Transaction transaction, ProjectState project)
            {
                if (database == null) throw new ArgumentNullException(nameof(database));
                if (transaction == null) throw new ArgumentNullException(nameof(transaction));
                if (project == null) throw new ArgumentNullException(nameof(project));

                lock (Gate)
                {
                    ThrowIfDisposed();
                    if (_markerStaged) throw new InvalidOperationException("Curtain Undo transition marker is already staged.");
                    RequireCurrentHistory(_document, _history, project, _previousRevision);

                    var stagedEntries = _rebase
                        ? new Dictionary<string, HistoryEntry>(StringComparer.Ordinal)
                        : new Dictionary<string, HistoryEntry>(_history.Entries, StringComparer.Ordinal);
                    if (_rebase) stagedEntries.Add(_previousRevision, _beforeEntry);
                    if (stagedEntries.Count >= MaxSnapshotsPerDocument)
                        throw new InvalidOperationException(
                            "Curtain Undo history reached its safe in-session limit. Save, close and reopen the drawing before rebuilding Curtain output.");
                    _stagedEntries = stagedEntries;
                    _markerStaged = true;
                }

                var modelSpace = OpenModelSpace(database, transaction, OpenMode.ForWrite);
                using (var marker = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, _nextRevision)))
                {
                    EnsureRegApp(database, transaction);
                    modelSpace.XData = marker;
                }
            }

            public void MarkNativeCommitted()
            {
                _nativeCommitted = true;
            }

            public void ConfirmCommitted(ProjectState project, ProjectStateSnapshot afterSnapshot)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                if (afterSnapshot == null) throw new ArgumentNullException(nameof(afterSnapshot));
                var afterEntry = new HistoryEntry(afterSnapshot, ProjectRevisionStamp.Capture(project));

                lock (Gate)
                {
                    ThrowIfDisposed();
                    if (_confirmed) return;
                    if (!_markerStaged || !_nativeCommitted || _stagedEntries == null)
                        throw new InvalidOperationException("Curtain Undo transition cannot publish before its native marker commits.");
                    RequireCurrentHistory(_document, _history, project, _previousRevision);
                    _stagedEntries.Add(_nextRevision, afterEntry);
                    _history.Publish(_stagedEntries, _nextRevision);
                    _confirmed = true;
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_confirmed) return;

                lock (Gate)
                {
                    if (!Histories.TryGetValue(_document, out var current) || !ReferenceEquals(current, _history)) return;
                    if (_nativeCommitted)
                    {
                        // Native state contains the new marker but no matching
                        // semantic snapshot was published. Never guess/rebase it.
                        _history.Desynchronized = true;
                        return;
                    }
                    if (_registeredHistory &&
                        string.Equals(_history.CurrentRevision, _previousRevision, StringComparison.Ordinal))
                        Histories.Remove(_document);
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(PendingTransition));
            }
        }

        private sealed class DocumentHistory
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
            public Dictionary<string, HistoryEntry> Entries { get; private set; } =
                new Dictionary<string, HistoryEntry>(StringComparer.Ordinal);

            public void Publish(Dictionary<string, HistoryEntry> entries, string revision)
            {
                Entries = entries;
                CurrentRevision = revision;
            }
        }

        private sealed class HistoryEntry
        {
            public HistoryEntry(ProjectStateSnapshot snapshot, ProjectRevisionStamp stamp)
            {
                Snapshot = snapshot;
                Stamp = stamp;
            }

            public ProjectStateSnapshot Snapshot { get; }
            public ProjectRevisionStamp Stamp { get; }
        }

        private readonly struct ProjectRevisionStamp
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
                CommandEventHandler handler = (sender, args) => OnCommandEnded(document, args);
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
                if (CommandEndedHandlers.TryGetValue(document, out handler)) CommandEndedHandlers.Remove(document);
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
            ProjectState project,
            ProjectStateSnapshot beforeSnapshot)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (beforeSnapshot == null) throw new ArgumentNullException(nameof(beforeSnapshot));

            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Curtain Undo registration");
            Attach(document);
            var previousRevision = ReadRevision(document);
            var beforeEntry = new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project));
            DocumentHistory history;
            var rebase = false;
            var registeredHistory = false;

            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out history))
                {
                    history = new DocumentHistory(document, project, previousRevision);
                    history.Entries.Add(previousRevision, beforeEntry);
                    Histories.Add(document, history);
                    registeredHistory = true;
                }
                else
                {
                    RequireCurrentHistory(document, history, project, previousRevision);
                    if (!history.Entries.TryGetValue(previousRevision, out var currentEntry) || !currentEntry.Stamp.Matches(project))
                        rebase = true;
                }

                if (!rebase && history.Entries.Count >= MaxSnapshotsPerDocument)
                    throw new InvalidOperationException(
                        "Curtain Undo history reached its safe in-session limit. Save, close and reopen the drawing before rebuilding Curtain output.");
            }

            return new PendingTransition(
                document,
                history,
                previousRevision,
                "CRU1:" + Guid.NewGuid().ToString("N"),
                beforeEntry,
                rebase,
                registeredHistory);
        }

        private static void OnCommandEnded(Document document, CommandEventArgs args)
        {
            if (!IsNativeUndoRedo(args?.GlobalCommandName)) return;
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
                    throw new InvalidOperationException(
                        "Curtain native Undo marker could not be read. Reload the project before further Curtain mutation.",
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
                            "Curtain native Undo reached an unavailable in-session semantic revision. Reload before further Curtain mutation.");
                    }
                }

                if (!ProjectContextCoordinator.TryGetCached(document, out var project) ||
                    !ReferenceEquals(project, history.Project) ||
                    !string.Equals(project.ProjectId, history.ProjectId, StringComparison.Ordinal))
                    throw MarkDesynchronized(history,
                        "Curtain native Undo cannot target a missing or replaced canonical project. Reload before continuing.");

                try { ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Curtain native Undo"); }
                catch (Exception backingStoreError)
                {
                    throw MarkDesynchronized(
                        history,
                        "Curtain native Undo was refused because the project backing store changed. Reload before continuing.",
                        backingStoreError);
                }

                if (!currentEntry.Stamp.Matches(project))
                    throw new InvalidOperationException(
                        "Curtain native Undo was refused because semantic state changed outside tracked native history. Redo the native change or reload before continuing.");

                var restoreRollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    targetEntry.Snapshot.Restore(project);
                    if (!targetEntry.Stamp.Matches(project))
                        throw new InvalidOperationException("Restored Curtain semantic state does not match its recorded revision.");
                }
                catch (Exception restoreError)
                {
                    try { restoreRollback.Restore(project); }
                    catch (Exception rollbackError)
                    {
                        throw MarkDesynchronized(
                            history,
                            "Curtain semantic Undo restore and recovery both failed.",
                            new AggregateException(restoreError, rollbackError));
                    }
                    throw MarkDesynchronized(history, "Curtain semantic Undo restore failed.", restoreError);
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
                Report(document, "QS3D Curtain Undo sync warning: " + error.Message);
            }
        }

        private static bool IsNativeUndoRedo(string? globalCommandName)
        {
            var normalized = (globalCommandName ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            while (normalized.Length > 0 && (normalized[0] == '_' || normalized[0] == '.'))
                normalized = normalized.Substring(1);
            return string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase);
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
                    "Curtain Undo history is not synchronized with this document/project. Reload before rebuilding Curtain output.");
        }

        private static string ReadRevision(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var revision = ReadRevision(OpenModelSpace(document.Database, transaction, OpenMode.ForRead));
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
                    throw new InvalidOperationException("Curtain native Undo marker is malformed.");
                var revision = Convert.ToString(values[2].Value, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(revision) || !revision.StartsWith("CRU1:", StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain native Undo revision is malformed.");
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
                Report(document, "QS3D Curtain semantic state synchronized with native Undo/Redo.");
            }
            catch (Exception error)
            {
                Report(document, "QS3D Curtain Undo restored; UI sync warning: " + error.Message);
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
