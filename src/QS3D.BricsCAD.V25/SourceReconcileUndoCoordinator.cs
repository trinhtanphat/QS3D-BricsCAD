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
        private static readonly Dictionary<Document, ObserverRegistration> ObserverRegistrations =
            new Dictionary<Document, ObserverRegistration>();
        private static readonly Dictionary<Document, DocumentHistory> Histories =
            new Dictionary<Document, DocumentHistory>();
        private static bool _suppressUndoUntilStableCommand;

        private sealed class ObserverRegistration
        {
            public ObserverRegistration(
                CommandEventHandler commandWillStart,
                CommandEventHandler commandEnded,
                CommandEventHandler commandCancelled,
                CommandEventHandler commandFailed)
            {
                CommandWillStart = commandWillStart;
                CommandEnded = commandEnded;
                CommandCancelled = commandCancelled;
                CommandFailed = commandFailed;
            }

            public CommandEventHandler CommandWillStart { get; }
            public CommandEventHandler CommandEnded { get; }
            public CommandEventHandler CommandCancelled { get; }
            public CommandEventHandler CommandFailed { get; }
            public string? PendingCommand { get; set; }
            public int ActiveCommandDepth { get; set; }

            public void Subscribe(Document document)
            {
                var willStartSubscribed = false;
                var endedSubscribed = false;
                var cancelledSubscribed = false;
                try
                {
                    document.CommandWillStart += CommandWillStart;
                    willStartSubscribed = true;
                    document.CommandEnded += CommandEnded;
                    endedSubscribed = true;
                    document.CommandCancelled += CommandCancelled;
                    cancelledSubscribed = true;
                    document.CommandFailed += CommandFailed;
                }
                catch
                {
                    if (cancelledSubscribed)
                    {
                        try { document.CommandCancelled -= CommandCancelled; }
                        catch { }
                    }
                    if (endedSubscribed)
                    {
                        try { document.CommandEnded -= CommandEnded; }
                        catch { }
                    }
                    if (willStartSubscribed)
                    {
                        try { document.CommandWillStart -= CommandWillStart; }
                        catch { }
                    }
                    throw;
                }
            }

            public void Unsubscribe(Document document)
            {
                try { document.CommandWillStart -= CommandWillStart; }
                catch { }
                try { document.CommandEnded -= CommandEnded; }
                catch { }
                try { document.CommandCancelled -= CommandCancelled; }
                catch { }
                try { document.CommandFailed -= CommandFailed; }
                catch { }
            }
        }

        internal sealed class PendingTransition : IDisposable
        {
            private readonly Document _document;
            private readonly Database _database;
            private readonly Transaction _transaction;
            private readonly BlockTableRecord _modelSpace;
            private readonly DocumentHistory _history;
            private readonly string _previousRevision;
            private readonly string _nextRevision;
            private readonly HistoryEntry _beforeEntry;
            private readonly bool _rebase;
            private readonly bool _registeredHistory;
            private Dictionary<string, HistoryEntry>? _stagedEntries;
            private bool _staged;
            private bool _committed;
            private bool _disposed;

            internal PendingTransition(
                Document document,
                Database database,
                Transaction transaction,
                BlockTableRecord modelSpace,
                DocumentHistory history,
                string previousRevision,
                string nextRevision,
                HistoryEntry beforeEntry,
                bool rebase,
                bool registeredHistory)
            {
                _document = document;
                _database = database;
                _transaction = transaction;
                _modelSpace = modelSpace;
                _history = history;
                _previousRevision = previousRevision;
                _nextRevision = nextRevision;
                _beforeEntry = beforeEntry;
                _rebase = rebase;
                _registeredHistory = registeredHistory;
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

                    var stagedEntries = _rebase
                        ? new Dictionary<string, HistoryEntry>(StringComparer.Ordinal)
                        : new Dictionary<string, HistoryEntry>(_history.Entries, StringComparer.Ordinal);
                    if (_rebase) stagedEntries.Add(_previousRevision, _beforeEntry);
                    if (stagedEntries.Count >= MaxSnapshotsPerDocument)
                        throw new InvalidOperationException(
                            "Source Reconcile Undo history reached its safe in-session limit. Save, close and reopen the drawing before reconciling again.");
                    stagedEntries.Add(_nextRevision, new HistoryEntry(snapshot, ProjectRevisionStamp.Capture(project)));
                    _stagedEntries = stagedEntries;
                    _staged = true;
                }

                // Every managed allocation and history validation is complete
                // before touching the native marker. The staged dictionary is
                // private until ConfirmCommitted publishes it after CAD commit.
                using (var marker = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, _nextRevision)))
                {
                    EnsureRegApp(_database, _transaction);
                    _modelSpace.XData = marker;
                }
            }

            public void ConfirmCommitted()
            {
                // StageAfter performs every allocation/validation before the CAD
                // commit. Publish the already-built dictionary only after the
                // native marker and CAD changes have committed together.
                lock (Gate)
                {
                    if (_disposed || !_staged || _stagedEntries == null) return;
                    if (!Histories.TryGetValue(_document, out var current) || !ReferenceEquals(current, _history))
                    {
                        _history.MarkDesynchronized(DesynchronizationCause.CommitHistoryLost);
                        _committed = true;
                        return;
                    }
                    _history.Publish(_stagedEntries, _nextRevision);
                    _committed = true;
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_committed) return;

                lock (Gate)
                {
                    if (_registeredHistory &&
                        Histories.TryGetValue(_document, out var current) &&
                        ReferenceEquals(current, _history) &&
                        string.Equals(_history.CurrentRevision, _previousRevision, StringComparison.Ordinal))
                        Histories.Remove(_document);
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(PendingTransition));
            }
        }

        internal enum DesynchronizationCause
        {
            None,
            CommitHistoryLost,
            RestoreRecoveryFailed
        }

        internal sealed class DocumentHistory
        {
            public DocumentHistory(Document document, ProjectState project, string revision)
            {
                Document = document;
                Database = document.Database;
                Project = project;
                ProjectId = project.ProjectId;
                CurrentRevision = revision;
            }

            public Document Document { get; }
            public Database Database { get; }
            public ProjectState Project { get; }
            public string ProjectId { get; }
            public string CurrentRevision { get; set; }
            public bool Desynchronized => Cause != DesynchronizationCause.None;
            public DesynchronizationCause Cause { get; private set; }
            public Dictionary<string, HistoryEntry> Entries { get; private set; } =
                new Dictionary<string, HistoryEntry>(StringComparer.Ordinal);

            public void MarkDesynchronized(DesynchronizationCause cause)
            {
                if (cause == DesynchronizationCause.None)
                    throw new ArgumentOutOfRangeException(nameof(cause));
                Cause = cause;
            }

            public void Publish(Dictionary<string, HistoryEntry> entries, string revision)
            {
                Entries = entries;
                CurrentRevision = revision;
            }
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

        internal sealed class SanitizedDiagnosticSnapshot
        {
            private readonly string _nativeRevision;
            private readonly bool _markerValid;

            internal SanitizedDiagnosticSnapshot(
                string historyState,
                string entryClass,
                string desynchronizationCause,
                string nativeRevision,
                bool markerValid)
            {
                HistoryState = historyState;
                EntryClass = entryClass;
                DesynchronizationCause = desynchronizationCause;
                _nativeRevision = nativeRevision;
                _markerValid = markerValid;
            }

            public string HistoryState { get; }
            public string EntryClass { get; }
            public string DesynchronizationCause { get; }

            public string CompareMarkerTo(SanitizedDiagnosticSnapshot before)
            {
                if (before == null) throw new ArgumentNullException(nameof(before));
                if (!_markerValid || !before._markerValid) return "MISSING_OR_INVALID";
                return string.Equals(_nativeRevision, before._nativeRevision, StringComparison.Ordinal)
                    ? "UNCHANGED"
                    : "ADVANCED";
            }
        }

        /// <summary>
        /// Captures only bounded classifications plus a private marker token for
        /// the synthetic LOCAL-004 diagnostic lane. No native revision, project
        /// identifier, path, handle, entry count or semantic value is exposed.
        /// This is observational: it never attaches handlers, loads/creates a
        /// project, or changes Undo history state.
        /// </summary>
        internal static SanitizedDiagnosticSnapshot CaptureSanitizedState(
            Document document,
            ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));

            var nativeRevision = string.Empty;
            var markerValid = false;
            try
            {
                nativeRevision = ReadRevision(document);
                markerValid = !string.IsNullOrWhiteSpace(nativeRevision);
            }
            catch
            {
                // The opaque snapshot reports only MISSING_OR_INVALID and never
                // makes the malformed marker or exception text observable.
            }

            DocumentHistory? history;
            var entryClass = "ONE";
            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out history))
                    return new SanitizedDiagnosticSnapshot("NONE", "ONE", "NONE", nativeRevision, markerValid);

                entryClass = history.Entries.Count > 1 ? "MULTIPLE" : "ONE";
                if (history.Desynchronized)
                    return new SanitizedDiagnosticSnapshot(
                        ProjectSanitizedHistoryState(history.Cause),
                        entryClass,
                        ClassifyDesynchronizationCause(history.Cause),
                        nativeRevision,
                        markerValid);
                if (!IsSameNativeDrawing(history, document) ||
                    !ReferenceEquals(history.Project, project) ||
                    !string.Equals(history.ProjectId, project.ProjectId, StringComparison.Ordinal))
                    return new SanitizedDiagnosticSnapshot(
                        "NONE", entryClass, "HISTORY_AFFINITY_MISMATCH", nativeRevision, markerValid);
            }

            if (!ProjectContextCoordinator.TryGetCached(document, out var cached) || !ReferenceEquals(cached, project))
                return new SanitizedDiagnosticSnapshot(
                    "NONE", entryClass, "CACHE_PROJECT_MISMATCH", nativeRevision, markerValid);

            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out var currentHistory) || !ReferenceEquals(currentHistory, history))
                    return new SanitizedDiagnosticSnapshot("NONE", "ONE", "NONE", nativeRevision, markerValid);

                entryClass = history.Entries.Count > 1 ? "MULTIPLE" : "ONE";
                if (history.Desynchronized)
                    return new SanitizedDiagnosticSnapshot(
                        ProjectSanitizedHistoryState(history.Cause),
                        entryClass,
                        ClassifyDesynchronizationCause(history.Cause),
                        nativeRevision,
                        markerValid);
                if (!IsSameNativeDrawing(history, document) ||
                    !ReferenceEquals(history.Project, project) ||
                    !string.Equals(history.ProjectId, project.ProjectId, StringComparison.Ordinal))
                    return new SanitizedDiagnosticSnapshot(
                        "NONE", entryClass, "HISTORY_AFFINITY_MISMATCH", nativeRevision, markerValid);

                if (!markerValid || !string.Equals(nativeRevision, history.CurrentRevision, StringComparison.Ordinal))
                    return new SanitizedDiagnosticSnapshot(
                        "MARKER_MISMATCH", entryClass, "NONE", nativeRevision, markerValid);

                return new SanitizedDiagnosticSnapshot("SYNCED", entryClass, "NONE", nativeRevision, markerValid);
            }
        }

        private static string ClassifyDesynchronizationCause(DesynchronizationCause cause)
        {
            if (cause == DesynchronizationCause.CommitHistoryLost) return "COMMIT_HISTORY_LOST";
            if (cause == DesynchronizationCause.RestoreRecoveryFailed) return "RESTORE_RECOVERY_FAILED";
            return "NONE";
        }

        private static string ProjectSanitizedHistoryState(DesynchronizationCause cause)
        {
            // The unchanged LOCAL-004 runner already allowlists the coarse
            // history states but cannot consume a new output field. Reserve
            // DESYNCHRONIZED for the only cause that can poison the live
            // dictionary history. Every other cause means there is no usable
            // history for the observed document/project pair and projects to
            // the existing NONE classification. The private cause remains
            // available to source/static diagnostics without exposing values.
            return cause == DesynchronizationCause.RestoreRecoveryFailed
                ? "DESYNCHRONIZED"
                : "NONE";
        }

        public static void Attach(Document? document)
        {
            if (document == null) return;
            lock (Gate)
            {
                if (ObserverRegistrations.ContainsKey(document)) return;
                var registration = new ObserverRegistration(
                    (sender, args) => OnCommandWillStart(document, args),
                    (sender, args) => OnCommandEnded(document, args),
                    (sender, args) => OnCommandAborted(document),
                    (sender, args) => OnCommandAborted(document));
                ObserverRegistrations.Add(document, registration);
                try { registration.Subscribe(document); }
                catch
                {
                    ObserverRegistrations.Remove(document);
                    throw;
                }
            }
        }

        public static void Detach(Document? document)
        {
            if (document == null) return;
            ObserverRegistration? registration = null;
            lock (Gate)
            {
                if (ObserverRegistrations.TryGetValue(document, out registration))
                {
                    if (registration.ActiveCommandDepth > 0)
                        _suppressUndoUntilStableCommand = true;
                    registration.PendingCommand = null;
                    registration.ActiveCommandDepth = 0;
                    ObserverRegistrations.Remove(document);
                }
                Histories.Remove(document);
            }
            registration?.Unsubscribe(document);
        }

        public static void Stop()
        {
            Document[] documents;
            lock (Gate) documents = new List<Document>(ObserverRegistrations.Keys).ToArray();
            foreach (var document in documents) Detach(document);
            lock (Gate) _suppressUndoUntilStableCommand = false;
        }

        public static void Forget(Document? document)
        {
            if (document == null) return;
            lock (Gate)
            {
                Histories.Remove(document);
                if (ObserverRegistrations.TryGetValue(document, out var registration))
                    registration.PendingCommand = null;
            }
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
            HistoryEntry beforeEntry;
            var rebase = false;
            var registeredHistory = false;
            lock (Gate)
            {
                beforeEntry = new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project));
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
                    {
                        // An intervening semantic-only operation is not represented
                        // in BricsCAD's native Undo stack. Rebase the current native
                        // marker to the freshly captured pre-command project so Undo
                        // of this new reconcile preserves those intervening edits.
                        // Older native marker revisions deliberately become unknown
                        // and will fail closed instead of restoring a stale snapshot.
                        rebase = true;
                    }
                }

                if (!rebase && history.Entries.Count >= MaxSnapshotsPerDocument)
                    throw new InvalidOperationException(
                        "Source Reconcile Undo history reached its safe in-session limit. Save, close and reopen the drawing before reconciling again.");
            }

            var nextRevision = "SRU1:" + Guid.NewGuid().ToString("N");
            return new PendingTransition(
                document,
                document.Database,
                transaction,
                modelSpace,
                history,
                previousRevision,
                nextRevision,
                beforeEntry,
                rebase,
                registeredHistory);
        }

        private static void OnCommandWillStart(Document document, CommandEventArgs args)
        {
            var normalized = NormalizeNativeUndoRedo(args?.GlobalCommandName);
            lock (Gate)
            {
                if (!ObserverRegistrations.TryGetValue(document, out var registration)) return;

                var nested = HasActiveCommand();
                if (_suppressUndoUntilStableCommand &&
                    normalized == null &&
                    !nested &&
                    IsActiveDocument(document))
                {
                    // A document can be detached while its outer command is
                    // still executing (for example CloseAndDiscard from a
                    // command in that document). Its terminal callback is then
                    // intentionally unavailable. The next ordinary top-level
                    // command is the first stable boundary at which a later
                    // user Undo may be armed safely.
                    _suppressUndoUntilStableCommand = false;
                }
                var topLevel = !_suppressUndoUntilStableCommand && !nested;
                registration.ActiveCommandDepth++;

                // A second start before the first command reaches a terminal
                // event makes the event stream ambiguous. Invalidate the token
                // rather than allowing a nested/internal completion to consume it.
                if (registration.PendingCommand != null)
                {
                    registration.PendingCommand = null;
                    return;
                }
                if (normalized == null || !topLevel || !IsActiveDocument(document)) return;
                if (!Histories.TryGetValue(document, out var history) || history.Desynchronized) return;
                registration.PendingCommand = normalized;
            }
        }

        private static void OnCommandEnded(Document document, CommandEventArgs args)
        {
            // CommandEnded alone is not evidence of a deliberate native Undo.
            // BricsCAD can emit internal terminal events during failure cleanup
            // and document close/discard. Restore only a matching command that
            // started while this exact tracked document was active.
            if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;

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
                    // A command-end read can race native transaction cleanup.
                    // Without a readable marker transition there is no evidence
                    // that semantic state must be permanently poisoned. The next
                    // mutation still reads/compares the marker and fails closed.
                    throw new InvalidOperationException(
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
                        throw new InvalidOperationException(
                            "Source Reconcile native Undo reached a revision that is not available in this plugin session. Redo the native change or reload before further mutation.");
                    }
                }

                if (!ProjectContextCoordinator.TryGetCached(document, out var project) ||
                    !ReferenceEquals(project, history.Project) ||
                    !string.Equals(project.ProjectId, history.ProjectId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Source Reconcile native Undo cannot target a missing or replaced canonical project. Redo the native change or reload before further mutation.");

                try { ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Source Reconcile native Undo"); }
                catch (Exception backingStoreError)
                {
                    throw new InvalidOperationException(
                        "Source Reconcile native Undo was refused because the project backing store changed. Redo the native change or reload before continuing.",
                        backingStoreError);
                }
                if (!currentEntry.Stamp.Matches(project))
                    // Intervening semantic-only work (for example Build3D) makes
                    // the old snapshot unsafe to restore. Refuse this transition
                    // without a sticky desync: as long as the native marker stays
                    // different, BeginTransition still fails closed; if native
                    // state returns to CurrentRevision, the next reconcile can
                    // safely rebase from the canonical project it actually sees.
                    throw new InvalidOperationException(
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
                    try
                    {
                        restoreRollback.Restore(project);
                        if (!currentEntry.Stamp.Matches(project))
                            throw new InvalidOperationException(
                                "Recovered Source Reconcile semantic state does not match its current revision.");
                    }
                    catch (Exception rollbackError)
                    {
                        throw MarkDesynchronized(history,
                            "Source Reconcile semantic Undo restore and recovery both failed.",
                            new AggregateException(restoreError, rollbackError));
                    }
                    // Successful recovery restored the canonical project to its
                    // exact pre-observer snapshot. Keep CurrentRevision unchanged:
                    // the live marker mismatch still blocks later mutation, but
                    // returning the marker to CurrentRevision permits safe retry.
                    throw new InvalidOperationException(
                        "Source Reconcile semantic Undo restore failed and was recovered. Redo the native change or reload before continuing.",
                        restoreError);
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

        private static void OnCommandAborted(Document document)
        {
            lock (Gate)
            {
                if (ObserverRegistrations.TryGetValue(document, out var registration))
                {
                    registration.PendingCommand = null;
                    if (registration.ActiveCommandDepth > 0)
                        registration.ActiveCommandDepth--;
                }
            }
        }

        private static bool TryConsumeMatchingCommand(Document document, string? globalCommandName)
        {
            var normalized = NormalizeNativeUndoRedo(globalCommandName);
            lock (Gate)
            {
                if (!ObserverRegistrations.TryGetValue(document, out var registration)) return false;
                var pendingCommand = registration.PendingCommand;
                registration.PendingCommand = null;
                if (registration.ActiveCommandDepth <= 0) return false;
                registration.ActiveCommandDepth--;
                return normalized != null &&
                    pendingCommand != null &&
                    string.Equals(normalized, pendingCommand, StringComparison.Ordinal) &&
                    IsActiveDocument(document);
            }
        }

        private static bool HasActiveCommand()
        {
            foreach (var registration in ObserverRegistrations.Values)
            {
                if (registration.ActiveCommandDepth > 0) return true;
            }
            return false;
        }

        private static bool IsActiveDocument(Document document)
        {
            try
            {
                var active = Application.DocumentManager.MdiActiveDocument;
                return active != null && IsSameNativeDrawing(active, document);
            }
            catch { return false; }
        }

        private static bool IsSameNativeDrawing(DocumentHistory history, Document document)
        {
            if (ReferenceEquals(history.Document, document)) return true;
            try { return ReferenceEquals(history.Database, document.Database); }
            catch { return false; }
        }

        private static bool IsSameNativeDrawing(Document left, Document right)
        {
            if (ReferenceEquals(left, right)) return true;
            try { return ReferenceEquals(left.Database, right.Database); }
            catch { return false; }
        }

        private static string? NormalizeNativeUndoRedo(string? globalCommandName)
        {
            var normalized = (globalCommandName ?? string.Empty).Trim();
            if (normalized.Length == 0) return null;
            while (normalized.Length > 0 && (normalized[0] == '_' || normalized[0] == '.'))
                normalized = normalized.Substring(1);
            if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";
            if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";
            if (string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)) return "MREDO";
            return null;
        }

        private static InvalidOperationException MarkDesynchronized(
            DocumentHistory history,
            string message,
            Exception? inner = null)
        {
            lock (Gate) history.MarkDesynchronized(DesynchronizationCause.RestoreRecoveryFailed);
            return inner == null ? new InvalidOperationException(message) : new InvalidOperationException(message, inner);
        }

        private static void RequireCurrentHistory(
            Document document,
            DocumentHistory history,
            ProjectState project,
            string nativeRevision)
        {
            if (history.Desynchronized ||
                !IsSameNativeDrawing(history, document) ||
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
                var active = Application.DocumentManager.MdiActiveDocument;
                if (active != null && IsSameNativeDrawing(active, document))
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
