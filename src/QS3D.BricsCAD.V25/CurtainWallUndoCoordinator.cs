using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Keeps the bounded semantic ownership state written by QS3DCURTAIN3D on the
    /// same in-session Undo/Redo revision as its native host/frame/panel geometry.
    /// The revision marker is written in the command's outer native transaction.
    /// </summary>
    internal static class CurtainWallUndoCoordinator
    {
        private const string RegAppName = "QS3D_CURTAIN_UNDO";
        private const string MarkerVersion = "1";
        private const string RevisionPrefix = "CWU1:";
        private const int MaxTransitionsPerDocument = 128;
        private const string FrameLiveFingerprintKey = "GeneratedCurtainFrameLiveFingerprint";
        private const string PanelLiveFingerprintKey = "GeneratedCurtainPanelLiveFingerprint";

        private static readonly object Gate = new object();
        private static readonly Dictionary<Document, CommandEventHandler> CommandEndedHandlers =
            new Dictionary<Document, CommandEventHandler>();
        private static readonly Dictionary<Document, DocumentHistory> Histories =
            new Dictionary<Document, DocumentHistory>();

        internal sealed class OwnerStateSnapshot
        {
            private readonly Dictionary<string, OwnerState> _owners;

            private OwnerStateSnapshot(Dictionary<string, OwnerState> owners)
            {
                _owners = owners ?? throw new ArgumentNullException(nameof(owners));
            }

            public int Count => _owners.Count;
            public IReadOnlyList<string> OwnerIds => _owners.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();

            public static OwnerStateSnapshot CaptureSelectedOwners(
                Document document,
                ProjectState project,
                IReadOnlyList<ObjectId> sourceIds)
            {
                if (document == null) throw new ArgumentNullException(nameof(document));
                if (project == null) throw new ArgumentNullException(nameof(project));
                if (sourceIds == null) throw new ArgumentNullException(nameof(sourceIds));
                if (sourceIds.Count == 0)
                    return new OwnerStateSnapshot(new Dictionary<string, OwnerState>(StringComparer.OrdinalIgnoreCase));

                var ownerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (var id in sourceIds)
                    {
                        var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (source == null || source.IsErased)
                            throw new InvalidOperationException("Curtain Undo registration encountered a missing selected source.");
                        var handle = source.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == ElementCategory.GlassWall &&
                                        x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count != 1)
                            throw new InvalidOperationException(
                                "Curtain Undo registration requires exactly one semantic GlassWall owner for each selected source.");
                        if (!ownerIds.Add(matches[0].Id))
                            throw new InvalidOperationException("Curtain Undo registration encountered a duplicate semantic owner.");
                    }
                    transaction.Commit();
                }
                return Capture(project, ownerIds);
            }

            public static OwnerStateSnapshot Capture(ProjectState project, IEnumerable<string> ownerIds)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                if (ownerIds == null) throw new ArgumentNullException(nameof(ownerIds));
                var owners = new Dictionary<string, OwnerState>(StringComparer.OrdinalIgnoreCase);
                foreach (var rawId in ownerIds)
                {
                    var id = (rawId ?? string.Empty).Trim();
                    if (id.Length == 0 || owners.ContainsKey(id))
                        throw new InvalidOperationException("Curtain Undo owner set contains a missing or duplicate id.");
                    var element = project.FindElement(id);
                    if (element == null || element.Category != ElementCategory.GlassWall)
                        throw new InvalidOperationException("Curtain Undo owner is missing or is no longer a GlassWall: " + id + ".");
                    owners.Add(id, OwnerState.Capture(element));
                }
                return new OwnerStateSnapshot(owners);
            }

            public bool HasSameOwnerSet(OwnerStateSnapshot other)
            {
                if (other == null || other._owners.Count != _owners.Count) return false;
                return _owners.Keys.All(other._owners.ContainsKey);
            }

            public bool CoreMatches(ProjectState project)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                foreach (var pair in _owners)
                {
                    var element = project.FindElement(pair.Key);
                    if (element == null || element.Category != ElementCategory.GlassWall) return false;
                    if (!pair.Value.CoreMatches(element)) return false;
                }
                return true;
            }

            public void Restore(ProjectState project)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                var targets = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in _owners.Keys)
                {
                    var element = project.FindElement(id);
                    if (element == null || element.Category != ElementCategory.GlassWall)
                        throw new InvalidOperationException("Curtain Undo cannot restore missing/non-GlassWall owner " + id + ".");
                    targets.Add(id, element);
                }

                foreach (var pair in _owners)
                    pair.Value.Restore(targets[pair.Key]);
                if (_owners.Count > 0) project.Touch();
            }
        }

        private sealed class OwnerState
        {
            private OwnerState(IReadOnlyList<string> sourceHandles, Dictionary<string, string> properties)
            {
                SourceHandles = sourceHandles;
                Properties = properties;
            }

            public IReadOnlyList<string> SourceHandles { get; }
            public Dictionary<string, string> Properties { get; }

            public static OwnerState Capture(ProjectElement element)
            {
                var handles = element.SourceHandles.Select(x => x ?? string.Empty).ToList().AsReadOnly();
                var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in element.Properties)
                    if (IsTrackedProperty(pair.Key)) properties.Add(pair.Key, pair.Value ?? string.Empty);
                return new OwnerState(handles, properties);
            }

            public bool CoreMatches(ProjectElement element)
            {
                if (element.SourceHandles.Count != SourceHandles.Count) return false;
                for (var index = 0; index < SourceHandles.Count; index++)
                    if (!string.Equals(SourceHandles[index], element.SourceHandles[index], StringComparison.OrdinalIgnoreCase))
                        return false;

                var current = element.Properties
                    .Where(x => IsTrackedCoreProperty(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                var expected = Properties
                    .Where(x => IsTrackedCoreProperty(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                if (current.Count != expected.Count) return false;
                foreach (var pair in expected)
                    if (!current.TryGetValue(pair.Key, out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal))
                        return false;
                return true;
            }

            public void Restore(ProjectElement element)
            {
                element.SourceHandles.Clear();
                foreach (var handle in SourceHandles) element.SourceHandles.Add(handle);

                foreach (var key in element.Properties.Keys.Where(IsTrackedProperty).ToList())
                    element.Properties.Remove(key);
                foreach (var pair in Properties) element.Properties[pair.Key] = pair.Value;
            }
        }

        private sealed class TransitionEntry
        {
            public TransitionEntry(
                string previousRevision,
                string nextRevision,
                OwnerStateSnapshot before,
                OwnerStateSnapshot after)
            {
                PreviousRevision = previousRevision;
                NextRevision = nextRevision;
                Before = before;
                After = after;
            }

            public string PreviousRevision { get; }
            public string NextRevision { get; }
            public OwnerStateSnapshot Before { get; set; }
            public OwnerStateSnapshot After { get; set; }
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
            public Dictionary<string, TransitionEntry> Transitions { get; } =
                new Dictionary<string, TransitionEntry>(StringComparer.Ordinal);
        }

        internal sealed class PendingTransition : IDisposable
        {
            private readonly Document _document;
            private readonly DocumentHistory _history;
            private readonly string _previousRevision;
            private readonly string _nextRevision;
            private readonly OwnerStateSnapshot _before;
            private readonly bool _registeredHistory;
            private TransitionEntry? _staged;
            private bool _committed;
            private bool _disposed;

            internal PendingTransition(
                Document document,
                DocumentHistory history,
                string previousRevision,
                string nextRevision,
                OwnerStateSnapshot before,
                bool registeredHistory)
            {
                _document = document;
                _history = history;
                _previousRevision = previousRevision;
                _nextRevision = nextRevision;
                _before = before;
                _registeredHistory = registeredHistory;
            }

            public void StageAfter(ProjectState project, Transaction transaction, OwnerStateSnapshot after)
            {
                if (project == null) throw new ArgumentNullException(nameof(project));
                if (transaction == null) throw new ArgumentNullException(nameof(transaction));
                if (after == null) throw new ArgumentNullException(nameof(after));
                ProjectContextCoordinator.RequireBackingStoreUnchanged(_document, project, "Curtain Undo staging");

                lock (Gate)
                {
                    ThrowIfDisposed();
                    if (_staged != null) throw new InvalidOperationException("Curtain Undo transition is already staged.");
                    RequireCurrentHistory(_document, _history, project, _previousRevision);
                    if (!_before.HasSameOwnerSet(after))
                        throw new InvalidOperationException("Curtain Undo before/after owner sets differ.");
                    if (!after.CoreMatches(project))
                        throw new InvalidOperationException("Curtain Undo after-state no longer matches the canonical project before native commit.");
                    if (_history.Transitions.Count >= MaxTransitionsPerDocument)
                        throw new InvalidOperationException(
                            "Curtain Undo history reached its safe in-session limit. Save, close and reopen the drawing before building Curtain 3D again.");
                    _staged = new TransitionEntry(_previousRevision, _nextRevision, _before, after);
                }

                var modelSpace = OpenModelSpace(_document.Database, transaction, OpenMode.ForWrite);
                var currentNativeRevision = ReadRevision(modelSpace);
                if (!string.Equals(currentNativeRevision, _previousRevision, StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain native revision changed before the outer command transaction could stage Undo metadata.");

                EnsureRegApp(_document.Database, transaction);
                using (var marker = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, MarkerVersion),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, _nextRevision)))
                    modelSpace.XData = marker;
            }

            public void ConfirmCommitted()
            {
                lock (Gate)
                {
                    if (_disposed || _staged == null) return;
                    if (!Histories.TryGetValue(_document, out var current) || !ReferenceEquals(current, _history))
                    {
                        _history.Desynchronized = true;
                        _committed = true;
                        return;
                    }
                    _history.Transitions[_nextRevision] = _staged;
                    _history.CurrentRevision = _nextRevision;
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
            ProjectState project,
            OwnerStateSnapshot before)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (before.Count == 0) throw new InvalidOperationException("Curtain Undo transition requires at least one semantic owner.");

            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Curtain Undo registration");
            if (!before.CoreMatches(project))
                throw new InvalidOperationException("Curtain Undo before-state changed before registration.");
            Attach(document);

            var previousRevision = ReadRevision(document);
            DocumentHistory history;
            var registeredHistory = false;
            lock (Gate)
            {
                if (!Histories.TryGetValue(document, out history) ||
                    !ReferenceEquals(history.Project, project) ||
                    !string.Equals(history.ProjectId, project.ProjectId, StringComparison.Ordinal))
                {
                    history = new DocumentHistory(document, project, previousRevision);
                    Histories[document] = history;
                    registeredHistory = true;
                }
                else
                {
                    RequireCurrentHistory(document, history, project, previousRevision);
                    if (history.Transitions.Count >= MaxTransitionsPerDocument)
                        throw new InvalidOperationException(
                            "Curtain Undo history reached its safe in-session limit. Save, close and reopen the drawing before building Curtain 3D again.");
                }
            }

            var nextRevision = RevisionPrefix + Guid.NewGuid().ToString("N");
            return new PendingTransition(
                document,
                history,
                previousRevision,
                nextRevision,
                before,
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
                var nativeRevision = ReadRevision(document);
                TransitionEntry transition;
                bool undo;
                lock (Gate)
                {
                    if (!Histories.TryGetValue(document, out var current) || !ReferenceEquals(current, history)) return;
                    if (string.Equals(nativeRevision, history.CurrentRevision, StringComparison.Ordinal)) return;

                    if (history.Transitions.TryGetValue(history.CurrentRevision, out transition) &&
                        string.Equals(transition.PreviousRevision, nativeRevision, StringComparison.Ordinal))
                    {
                        undo = true;
                    }
                    else if (history.Transitions.TryGetValue(nativeRevision, out transition) &&
                             string.Equals(transition.PreviousRevision, history.CurrentRevision, StringComparison.Ordinal))
                    {
                        undo = false;
                    }
                    else
                    {
                        history.Desynchronized = true;
                        throw new InvalidOperationException(
                            "Curtain native Undo/Redo reached a revision not available in this plugin session. Reload the project before further Curtain mutation.");
                    }
                }

                if (!ProjectContextCoordinator.TryGetCached(document, out var project) ||
                    !ReferenceEquals(project, history.Project) ||
                    !string.Equals(project.ProjectId, history.ProjectId, StringComparison.Ordinal))
                    throw MarkDesynchronized(history,
                        "Curtain native Undo/Redo cannot target a missing or replaced canonical project. Reload before continuing.");

                try { ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, "Curtain native Undo/Redo"); }
                catch (Exception backingStoreError)
                {
                    throw MarkDesynchronized(
                        history,
                        "Curtain native Undo/Redo was refused because the project backing store changed. Reload before continuing.",
                        backingStoreError);
                }

                var currentExpected = undo ? transition.After : transition.Before;
                if (!currentExpected.CoreMatches(project))
                    throw MarkDesynchronized(history,
                        "Curtain native Undo/Redo was refused because generated owner metadata changed outside the tracked Curtain history. Reload and reconcile before further mutation.");

                // Live fingerprint stamps are intentionally post-commit warnings. They are excluded
                // from CoreMatches, then captured here so Redo restores the actual committed stamp
                // rather than the pre-stamp staging snapshot.
                var currentFull = OwnerStateSnapshot.Capture(project, currentExpected.OwnerIds);
                if (undo) transition.After = currentFull;
                else transition.Before = currentFull;

                var target = undo ? transition.Before : transition.After;
                var restoreRollback = OwnerStateSnapshot.Capture(project, target.OwnerIds);
                try
                {
                    target.Restore(project);
                    if (!target.CoreMatches(project))
                        throw new InvalidOperationException("Restored Curtain semantic owner state does not match its native revision.");
                }
                catch (Exception restoreError)
                {
                    try { restoreRollback.Restore(project); }
                    catch (Exception rollbackError)
                    {
                        throw MarkDesynchronized(history,
                            "Curtain semantic Undo/Redo restore and recovery both failed.",
                            new AggregateException(restoreError, rollbackError));
                    }
                    throw MarkDesynchronized(history, "Curtain semantic Undo/Redo restore failed.", restoreError);
                }

                lock (Gate)
                {
                    if (Histories.TryGetValue(document, out var current) && ReferenceEquals(current, history))
                        history.CurrentRevision = nativeRevision;
                }
                RefreshAfterRestore(document);
            }
            catch (Exception error)
            {
                Report(document, "QS3D Curtain Undo sync warning: " + error.Message);
            }
        }

        private static bool IsTrackedProperty(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.StartsWith("GeneratedSolid", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("GeneratedCurtainFrame", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("GeneratedCurtainPanel", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("QS3D.GeneratedSolid.", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("QS3D.GeneratedCurtainFrame.", StringComparison.OrdinalIgnoreCase) ||
                   key.StartsWith("QS3D.GeneratedCurtainPanel.", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, ProjectElement.GeneratedGeometryStateKey, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, ProjectElement.GeneratedGeometryStaleReasonKey, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrackedCoreProperty(string? key) =>
            IsTrackedProperty(key) &&
            !string.Equals(key, FrameLiveFingerprintKey, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(key, PanelLiveFingerprintKey, StringComparison.OrdinalIgnoreCase);

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
                    "Curtain Undo history is not synchronized with this document/project. Reload before building Curtain 3D again.");
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
                    throw new InvalidOperationException("Curtain native Undo marker is malformed.");
                var revision = Convert.ToString(values[2].Value, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(revision) || !revision.StartsWith(RevisionPrefix, StringComparison.Ordinal))
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
                Report(document, "QS3D Curtain semantic owner state synchronized with native Undo/Redo.");
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
