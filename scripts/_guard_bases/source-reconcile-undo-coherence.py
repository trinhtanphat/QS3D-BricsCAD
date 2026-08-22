#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
LIFECYCLE = ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs"
PROJECTS = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
RUNBOOK = ROOT / "docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Source Reconcile Undo surface: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


coordinator = read(COORDINATOR)
service = read(SERVICE)
lifecycle = read(LIFECYCLE)
projects = read(PROJECTS)
runbook = read(RUNBOOK)
inbox = read(INBOX)

for token in (
    'RegAppName = "QS3D_SRC_SYNC_UNDO"',
    'MarkerVersion = "1"',
    "Dictionary<Document, ObserverRegistration>",
    "Dictionary<Document, DocumentHistory>",
    "public Document SubscribedDocument { get; }",
    "ReferenceEquals(existing.SubscribedDocument, document)",
    "existing.Unsubscribe();",
    "registration.Subscribe();",
    "registration?.Unsubscribe();",
    "document.CommandWillStart += CommandWillStart",
    "document.CommandEnded += CommandEnded",
    "document.CommandCancelled += CommandCancelled",
    "document.CommandFailed += CommandFailed",
    "document.CommandWillStart -= CommandWillStart",
    "document.CommandEnded -= CommandEnded",
    "document.CommandCancelled -= CommandCancelled",
    "document.CommandFailed -= CommandFailed",
    "OnCommandWillStart(document, args)",
    "OnCommandEnded(document, args)",
    "SynchronizeToNativeRevision(document)",
    "OnCommandAborted(document)",
    "TryConsumeMatchingCommand(document, args?.GlobalCommandName)",
    "NormalizeNativeUndoRedo(args?.GlobalCommandName)",
    "IsActiveDocument(document)",
    "IsCurrentHistory(document, history)",
    "EqualityComparer<Document>.Default.Equals(left, right)",
    "active != null && IsSameDocumentKey(active, document)",
    "registration.PendingCommand = null",
    "ObserverRegistrations.TryGetValue(document, out var registration)",
    'string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)',
    'string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)',
    'string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)',
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "ProjectContextCoordinator.TryGetCached(document, out var project)",
    "ReferenceEquals(project, history.Project)",
    "currentEntry.Stamp.Matches(project)",
    "var restoreRollback = ProjectStateSnapshot.Capture(project);",
    "targetEntry.Snapshot.Restore(project);",
    "restoreRollback.Restore(project);",
    "markerCarrier.GetXDataForApplication(RegAppName)",
    "_markerCarrier.DisableUndoRecording(false);",
    "_markerCarrier.UpgradeOpen();",
    "markerCarrier.XData = marker",
    "OpenMarkerCarrier(document.Database, transaction, OpenMode.ForRead)",
    "transaction.GetObject(modelSpace.BlockBeginId, mode)",
    "MaxSnapshotsPerDocument = 128",
    "history.MarkDesynchronized(DesynchronizationCause.RestoreRecoveryFailed)",
    "_history.MarkDesynchronized(DesynchronizationCause.CommitHistoryLost)",
    "new Dictionary<string, HistoryEntry>(_history.Entries, StringComparer.Ordinal)",
    "_history.Publish(_stagedEntries, _nextRevision);",
    "_registeredHistory",
    "Histories.Remove(_document);",
    "EnsureRegApp(_database, _transaction);",
    "internal sealed class SanitizedDiagnosticSnapshot",
    "internal static SanitizedDiagnosticSnapshot CaptureSanitizedState(",
    "public string HistoryState { get; }",
    "public string EntryClass { get; }",
    "public string DesynchronizationCause { get; }",
    "ProjectSanitizedHistoryState(history.Cause)",
    "public string CompareMarkerTo(SanitizedDiagnosticSnapshot before)",
    'return "MISSING_OR_INVALID";',
    '? "UNCHANGED"',
    ': "ADVANCED";',
):
    if token not in coordinator:
        errors.append("Undo coordinator missing contract: " + token)

attach_start = coordinator.find("public static void Attach(")
detach_start = coordinator.find("public static void Detach(", attach_start)
stop_start = coordinator.find("public static void Stop(", detach_start)
forget_start = coordinator.find("public static void Forget(", stop_start)
attach_body = coordinator[attach_start:detach_start]
detach_body = coordinator[detach_start:stop_start]
stop_body = coordinator[stop_start:forget_start]
if min(attach_start, detach_start, stop_start, forget_start) < 0:
    errors.append("Undo observer lifecycle boundaries are missing")
else:
    same_wrapper = attach_body.find("if (ReferenceEquals(existing.SubscribedDocument, document)) return;")
    unsubscribe_old = attach_body.find("existing.Unsubscribe();", same_wrapper)
    remove_old = attach_body.find("ObserverRegistrations.Remove(document);", unsubscribe_old)
    bind_current = attach_body.find("new ObserverRegistration(\n                    document,", remove_old)
    subscribe_current = attach_body.find("registration.Subscribe();", bind_current)
    if min(same_wrapper, unsubscribe_old, remove_old, bind_current, subscribe_current) < 0 or not (
        same_wrapper < unsubscribe_old < remove_old < bind_current < subscribe_current
    ):
        errors.append("equality-equivalent observer wrappers must unsubscribe/rebind to the exact current instance before transition registration")
    if "ObserverRegistrations.ContainsKey(document)" in attach_body:
        errors.append("equality-only observer lookup must not retain a stale wrapper subscription")
    if "registration?.Unsubscribe();" not in detach_body:
        errors.append("Detach must unsubscribe the exact wrapper recorded by the resolved registration")
    if ".ConvertAll(x => x.SubscribedDocument)" not in stop_body:
        errors.append("Stop must detach the exact subscribed wrapper instances")

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate(",
    "ProjectContextCoordinator.TryGetReadOnly(",
    "Application.DocumentManager.MdiActiveDocument =",
    "SendStringToExecute",
    "_history.CurrentRevision = _nextRevision",
    "history.Entries.Clear();",
    'string.Equals(normalized, "U", StringComparison.OrdinalIgnoreCase)',
    "public Document Document { get; }",
    "public Database Database { get; }",
    "IsSameNativeDrawing(",
    "ActiveCommandDepth",
    "HasActiveCommand()",
    "_suppressUndoUntilStableCommand",
):
    if forbidden in coordinator:
        errors.append("Undo observer must not load/create/switch/drive a project or command: " + forbidden)

prepare = service.find("GeneratedDependentGeometryInvalidator.Prepare")
units = service.find("CadUnitService.TryGetPolicy", prepare)
refresh = service.find("RefreshSourceDerivedState(project", prepare)
metadata = service.find("invalidation.CommitMetadata()", refresh)
after = service.find("var afterSnapshot = ProjectStateSnapshot.Capture(project);", metadata)
begin = service.find("SourceReconcileUndoCoordinator.BeginTransition(", after)
stage = service.find("undoTransition.StageAfter(project, afterSnapshot);", begin)
commit = service.find("transaction.Commit();", stage)
confirm = service.find("undoTransition.ConfirmCommitted();", commit)
committed = service.find("cadCommitted = true;", confirm)
restore = service.find("rollback.Restore(project);", committed)
positions = (prepare, units, refresh, metadata, after, begin, stage, commit, confirm, committed, restore)
if any(position < 0 for position in positions) or list(positions) != sorted(positions):
    errors.append(
        "Source Reconcile must finish invalidation/unit/refresh work -> capture Redo -> begin/stage marker -> native commit -> publish history, while preserving pre-commit semantic rollback"
    )

snapshot = service.find("var rollback = ProjectStateSnapshot.Capture(project);")
stamp = service.find(
    "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
    snapshot,
)
transaction = service.find("document.Database.TransactionManager.StartTransaction()", stamp)
if min(snapshot, stamp, transaction, begin) < 0 or not snapshot < stamp < transaction < begin:
    errors.append(
        "Source Reconcile must pair its rollback snapshot with a pre-mutation revision stamp before the native transaction and delay the Undo transition until all reconcile work is ready"
    )
begin_call = service[begin:stage]
if "rollback,\n                        rollbackStamp))" not in begin_call:
    errors.append("Source Reconcile must pass the captured pre-mutation stamp with its rollback snapshot")
if "if (!cadCommitted)" not in service or "new AggregateException(operationError, restoreError)" not in service:
    errors.append("Source Reconcile command-failure semantic rollback contract drifted")

diagnostic_start = coordinator.find("internal sealed class SanitizedDiagnosticSnapshot")
attach_start = coordinator.find("public static void Attach(", diagnostic_start)
diagnostic = coordinator[diagnostic_start:attach_start] if diagnostic_start >= 0 and attach_start > diagnostic_start else ""
if not diagnostic:
    errors.append("sanitized Source Reconcile diagnostic accessor is missing")
else:
    for token in (
        '"NONE"', '"SYNCED"', '"MARKER_MISMATCH"', '"DESYNCHRONIZED"',
        '"ONE"', '"MULTIPLE"', '"ADVANCED"', '"UNCHANGED"', '"MISSING_OR_INVALID"',
        '"COMMIT_HISTORY_LOST"', '"RESTORE_RECOVERY_FAILED"',
        '"HISTORY_AFFINITY_MISMATCH"', '"CACHE_PROJECT_MISMATCH"',
        "private readonly string _nativeRevision;",
        "ProjectContextCoordinator.TryGetCached(document, out var cached)",
        "ReferenceEquals(cached, project)",
        "ReadRevision(document)",
    ):
        if token not in diagnostic:
            errors.append("sanitized diagnostic accessor missing contract: " + token)
    for forbidden in (
        "public string NativeRevision",
        "public string ProjectId",
        "public int EntryCount",
        "return _nativeRevision",
        "return history.ProjectId",
        "return history.CurrentRevision",
        "Attach(document)",
        "Histories.Add(",
        "Histories.Remove(",
        "CurrentRevision =",
    ):
        if forbidden in diagnostic:
            errors.append("sanitized diagnostic accessor exposes raw state or mutates coordination: " + forbidden)
    if diagnostic.count("ProjectSanitizedHistoryState(history.Cause)") != 2:
        errors.append("persistent history cause must be projected consistently before and after cache validation")
    if diagnostic.count("!ReferenceEquals(history.Project, project)") != 2:
        errors.append("sanitized history affinity must retain exact canonical project identity")
    if "if (!IsCurrentHistory(document, history))" not in diagnostic:
        errors.append("sanitized history must remain the dictionary's current entry across cache validation")
    if "history.Document" in diagnostic or "history.Database" in diagnostic:
        errors.append("sanitized history must not impose wrapper/native identity stricter than dictionary lookup")
    if '"NONE", entryClass, "HISTORY_AFFINITY_MISMATCH"' not in diagnostic:
        errors.append("history affinity mismatch must project to existing NONE state without hiding its private sanitized cause")
    if '"NONE", entryClass, "CACHE_PROJECT_MISMATCH"' not in diagnostic:
        errors.append("cache project mismatch must project to existing NONE state without hiding its private sanitized cause")
    projection_start = diagnostic.find("private static string ProjectSanitizedHistoryState(")
    projection_body = diagnostic[projection_start:] if projection_start >= 0 else ""
    if (
        "cause == DesynchronizationCause.RestoreRecoveryFailed" not in projection_body
        or '? "DESYNCHRONIZED"' not in projection_body
        or ': "NONE";' not in projection_body
    ):
        errors.append("DESYNCHRONIZED projection must uniquely identify live restore/recovery failure")

stage_start = coordinator.find("public void StageAfter(")
confirm_start = coordinator.find("public void ConfirmCommitted()", stage_start)
dispose_start = coordinator.find("public void Dispose()", confirm_start)
begin_start = coordinator.find("public static PendingTransition BeginTransition(")
will_start = coordinator.find("private static void OnCommandWillStart(", begin_start)
ended_start = coordinator.find("private static void OnCommandEnded(", will_start)
sync_start = coordinator.find("private static void SynchronizeToNativeRevision(", ended_start)
aborted_start = coordinator.find("private static void OnCommandAborted(", sync_start)
consume_start = coordinator.find("private static bool TryConsumeMatchingCommand(", aborted_start)
active_start = coordinator.find("private static bool IsActiveDocument(", consume_start)
current_history_start = coordinator.find("private static bool IsCurrentHistory(", active_start)
same_document_start = coordinator.find("private static bool IsSameDocumentKey(", current_history_start)
normalize_start = coordinator.find("private static string? NormalizeNativeUndoRedo(", active_start)
mark_start = coordinator.find("private static InvalidOperationException MarkDesynchronized(", normalize_start)
stage_body = coordinator[stage_start:confirm_start]
confirm_body = coordinator[confirm_start:dispose_start]
begin_body = coordinator[begin_start:will_start]
will_body = coordinator[will_start:ended_start]
ended_body = coordinator[ended_start:sync_start]
sync_body = coordinator[sync_start:aborted_start]
aborted_body = coordinator[aborted_start:consume_start]
consume_body = coordinator[consume_start:active_start]
filter_body = coordinator[normalize_start:mark_start]
if min(
    stage_start, confirm_start, dispose_start, begin_start, will_start,
    ended_start, sync_start, aborted_start, consume_start, active_start, current_history_start,
    same_document_start, normalize_start, mark_start,
) < 0:
    errors.append("Undo coordinator transition method boundaries are missing")
else:
    if (
        "ProjectStateSnapshot beforeSnapshot,\n            ProjectRevisionStamp beforeStamp)" not in begin_body
        or "beforeEntry = new HistoryEntry(beforeSnapshot, beforeStamp);" not in begin_body
    ):
        errors.append("the before history entry must pair the rollback snapshot with its captured pre-mutation stamp")
    if "new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project))" in begin_body:
        errors.append("BeginTransition must not pair the before snapshot with a live post-mutation project stamp")
    marker_carrier_read = begin_body.find(
        "OpenMarkerCarrier(document.Database, transaction, OpenMode.ForRead)"
    )
    previous_revision = begin_body.find("var previousRevision = ReadRevision(markerCarrier);", marker_carrier_read)
    if min(marker_carrier_read, previous_revision) < 0 or "OpenMode.ForWrite" in begin_body:
        errors.append("BeginTransition must keep the existing ModelSpace BlockBegin marker carrier read-only while capturing its prior revision")
    if "markerCarrier.XData = marker" not in stage_body or "_stagedEntries = stagedEntries;" not in stage_body:
        errors.append("native marker must be written only after the private shadow history is fully staged")
    enable_undo = stage_body.find("_markerCarrier.DisableUndoRecording(false);")
    upgrade_write = stage_body.find("_markerCarrier.UpgradeOpen();", enable_undo)
    marker_write = stage_body.find("_markerCarrier.XData = marker;", upgrade_write)
    enable_end = enable_undo + len("_markerCarrier.DisableUndoRecording(false);")
    upgrade_end = upgrade_write + len("_markerCarrier.UpgradeOpen();")
    if (
        min(enable_undo, upgrade_write, marker_write) < 0
        or stage_body[enable_end:upgrade_write].strip()
        or stage_body[upgrade_end:marker_write].strip()
    ):
        errors.append("the read-only ModelSpace BlockBegin carrier must enable native Undo recording, upgrade open, then assign revision XData")
    if (
        coordinator.count(".DisableUndoRecording(") != 1
        or coordinator.count("_markerCarrier.UpgradeOpen(") != 1
        or "DisableUndoRecording" in begin_body
        or "_markerCarrier.UpgradeOpen" in begin_body
    ):
        errors.append("explicit Undo recording and write upgrade must remain isolated to the staged BlockBegin revision marker write")
    if (
        coordinator.count("modelSpace.BlockBeginId") != 1
        or "new BlockBegin" in coordinator
        or "StartUndoRecord" in coordinator
        or "_modelSpace" in coordinator
    ):
        errors.append("the revision carrier must be the existing geometry-free ModelSpace BlockBegin without creating an entity or a database undo record")
    if "markerCarrier.XData = marker" in begin_body or "EnsureRegApp" in begin_body:
        errors.append("BeginTransition must not mutate the native marker before fallible reconcile work completes")
    if "_history.Publish(_stagedEntries, _nextRevision);" not in confirm_body:
        errors.append("published semantic revision must advance only in post-CAD-commit confirmation")
    if (
        "!Histories.TryGetValue(_document, out var current) || !ReferenceEquals(current, _history)" not in confirm_body
        or "_history.MarkDesynchronized(DesynchronizationCause.CommitHistoryLost);" not in confirm_body
    ):
        errors.append("commit-history loss must be classified only on the orphaned/replaced transition history")
    if "CurrentRevision = _nextRevision" in stage_body:
        errors.append("StageAfter must not expose an uncommitted semantic revision")
    if 'string.Equals(normalized, "U", StringComparison.OrdinalIgnoreCase)' in filter_body:
        errors.append("single-letter U is ambiguous in BricsCAD V25 and must not drive semantic Undo observation")

    if (
        "registration.PendingCommand = null;" not in will_body
        or "normalized == null || !IsActiveDocument(document)" not in will_body
        or "registration.PendingCommand = normalized;" not in will_body
    ):
        errors.append("Undo observer must replace stale intent with one latest active-document native command token")
    fallback_gate = will_body.find("if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);")
    normalize_intent = will_body.find("var normalized = NormalizeNativeUndoRedo(args?.GlobalCommandName);")
    if fallback_gate < 0 or normalize_intent < 0 or fallback_gate > normalize_intent:
        errors.append("next active-document command must reconcile the committed native marker before its body or intent handling")
    consume_gate = ended_body.find("if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;")
    ended_sync = ended_body.find("SynchronizeToNativeRevision(document);", consume_gate)
    if consume_gate < 0 or ended_sync < 0 or consume_gate > ended_sync:
        errors.append("CommandEnded must consume a matching start token before shared native-marker reconciliation")
    marker_read_start = sync_body.find("try { nativeRevision = ReadRevision(document); }")
    if (
        "var pendingCommand = registration.PendingCommand;" not in consume_body
        or "registration.PendingCommand = null;" not in consume_body
        or "string.Equals(normalized, pendingCommand, StringComparison.Ordinal)" not in consume_body
        or "IsActiveDocument(document)" not in consume_body
    ):
        errors.append("Undo completion must clear intent and match command plus active document exactly")
    active_body = coordinator[active_start:current_history_start]
    current_history_body = coordinator[current_history_start:same_document_start]
    same_document_body = coordinator[same_document_start:normalize_start]
    if "active != null && IsSameDocumentKey(active, document)" not in active_body:
        errors.append("active-document Undo authority must use the same comparer as document dictionaries")
    if (
        "Histories.TryGetValue(document, out var current)" not in current_history_body
        or "ReferenceEquals(current, history)" not in current_history_body
        or ".Name" in current_history_body
        or ".Database" in current_history_body
    ):
        errors.append("history affinity must require the dictionary's exact current entry without wrapper/native fallback")
    if (
        "EqualityComparer<Document>.Default.Equals(left, right)" not in same_document_body
        or ".Database" in same_document_body
        or ".Name" in same_document_body
    ):
        errors.append("active-document affinity must match Dictionary<Document, ...> default-comparer semantics")
    if "registration.PendingCommand = null;" not in aborted_body:
        errors.append("cancelled and failed commands must clear pending Undo intent")

    target_start = sync_body.find("HistoryEntry targetEntry;", marker_read_start)
    stamp_start = sync_body.find("if (!currentEntry.Stamp.Matches(project))", target_start)
    restore_start = sync_body.find("var restoreRollback = ProjectStateSnapshot.Capture(project);", stamp_start)
    rollback_failure_start = sync_body.find("catch (Exception rollbackError)", restore_start)
    recovered_restore_start = sync_body.find(
        '"Source Reconcile semantic Undo restore failed and was recovered.',
        rollback_failure_start,
    )
    advance_start = sync_body.find("history.CurrentRevision = nativeRevision;", recovered_restore_start)
    if min(
        marker_read_start, target_start, stamp_start, restore_start,
        rollback_failure_start, recovered_restore_start, advance_start,
    ) < 0:
        errors.append("Undo observer marker/stamp refusal boundaries are missing")
    else:
        if "MarkDesynchronized(" in sync_body[marker_read_start:target_start]:
            errors.append("a transient command-boundary marker read failure must not permanently poison history")
        if "MarkDesynchronized(" in sync_body[stamp_start:restore_start]:
            errors.append("semantic-only drift refusal must remain recoverable when the native marker returns current")
        read_only_refusals = sync_body[target_start:restore_start]
        if "MarkDesynchronized(" in read_only_refusals or "history.Desynchronized = true" in read_only_refusals:
            errors.append("read-only unknown-revision/project/backing-store refusals must not permanently poison history")
        if "MarkDesynchronized(" in sync_body[recovered_restore_start:advance_start]:
            errors.append("a failed semantic restore followed by successful exact rollback must remain recoverable")
        recovery_body = sync_body[restore_start:rollback_failure_start]
        if (
            "restoreRollback.Restore(project);" not in recovery_body
            or "if (!currentEntry.Stamp.Matches(project))" not in recovery_body
            or '"Recovered Source Reconcile semantic state does not match its current revision."' not in recovery_body
        ):
            errors.append("restore recovery must verify the canonical project returned to its current revision before remaining nonsticky")
        if sync_body.count("MarkDesynchronized(") != 1:
            errors.append("native-marker reconciliation may become sticky only when semantic restore and recovery both fail")
        double_failure = sync_body[rollback_failure_start:recovered_restore_start]
        if "MarkDesynchronized(history," not in double_failure or "new AggregateException(restoreError, rollbackError)" not in double_failure:
            errors.append("sticky desync must remain bound to combined semantic restore and recovery failure")
        mark_body = coordinator[mark_start:coordinator.find("private static void RequireCurrentHistory(", mark_start)]
        if "history.MarkDesynchronized(DesynchronizationCause.RestoreRecoveryFailed);" not in mark_body:
            errors.append("live sticky history must carry the RESTORE_RECOVERY_FAILED sanitized cause")
        require_start = coordinator.find("private static void RequireCurrentHistory(", mark_start)
        require_end = coordinator.find("private static string ReadRevision(Document document)", require_start)
        require_body = coordinator[require_start:require_end]
        if (
            "!IsCurrentHistory(document, history)" not in require_body
            or "!ReferenceEquals(history.Project, project)" not in require_body
            or "!string.Equals(history.ProjectId, project.ProjectId" not in require_body
            or "!string.Equals(history.CurrentRevision, nativeRevision" not in require_body
        ):
            errors.append("transition affinity must require current dictionary membership while retaining project/revision fail-closed guards")
        if "CurrentRevision =" in sync_body[target_start:advance_start]:
            errors.append("all observer refusals and recovered restore failures must leave CurrentRevision unchanged")

# Deterministic model of the production shadow-publication and event-intent
# contracts. The static tokens above bind these states to the coordinator and
# service surfaces. In particular, a terminal event named UNDO is insufficient
# by itself: the exact V25 failure reached sticky desync before the runner sent
# its first explicit Undo.
def normalize_command(name):
    normalized = (name or "").strip().lstrip("_.").upper()
    return normalized if normalized in ("UNDO", "REDO", "MREDO") else None


class ManagedDocument:
    def __init__(self, dictionary_key, wrapper):
        self.dictionary_key = dictionary_key
        self.wrapper = wrapper

    def __hash__(self):
        return hash(self.dictionary_key)

    def __eq__(self, other):
        return isinstance(other, ManagedDocument) and self.dictionary_key == other.dictionary_key


def is_current_history(histories, document, history):
    return histories.get(document) is history


document_a_first_wrapper = ManagedDocument("A", "first")
document_a_after_mdi_switch = ManagedDocument("A", "replacement")
document_b = ManagedDocument("B", "first")
history_a = object()
histories = {document_a_first_wrapper: history_a}
if not is_current_history(histories, document_a_after_mdi_switch, history_a):
    errors.append("dictionary-equivalent managed Document wrapper lost its current history")
if is_current_history(histories, document_b, history_a):
    errors.append("a distinct dictionary document key crossed history affinity")
replacement_history = object()
histories[document_a_after_mdi_switch] = replacement_history
if is_current_history(histories, document_a_first_wrapper, history_a):
    errors.append("a replaced history object remained current through an equivalent wrapper")
if not is_current_history(histories, document_a_first_wrapper, replacement_history):
    errors.append("dictionary-equivalent wrapper did not resolve the replacement current history")


class WrapperBoundRegistration:
    def __init__(self, subscribed_document):
        self.subscribed_document = subscribed_document
        self.subscribed = True


def attach_wrapper(registrations, document):
    existing = registrations.get(document)
    if existing is not None and existing.subscribed_document is document:
        return existing
    if existing is not None:
        existing.subscribed = False
        registrations.pop(document)
    current = WrapperBoundRegistration(document)
    registrations[document] = current
    return current


registrations = {}
first_registration = attach_wrapper(registrations, document_a_first_wrapper)
if attach_wrapper(registrations, document_a_first_wrapper) is not first_registration:
    errors.append("same-wrapper observer attach lost idempotence")
replacement_registration = attach_wrapper(registrations, document_a_after_mdi_switch)
if first_registration.subscribed:
    errors.append("equality-equivalent stale wrapper remained subscribed after rebind")
if replacement_registration.subscribed_document is not document_a_after_mdi_switch:
    errors.append("observer rebind did not capture the exact current managed wrapper")
if registrations.get(document_a_first_wrapper) is not replacement_registration:
    errors.append("replacement wrapper registration lost dictionary affinity")
document_b_registration = attach_wrapper(registrations, document_b)
if registrations.get(document_b) is not document_b_registration or len(registrations) != 2:
    errors.append("distinct document key was not isolated during observer rebind")


class CommandRegistration:
    def __init__(self):
        self.pending = None


class CommandIntent:
    def __init__(self, *documents):
        self.registrations = {document: CommandRegistration() for document in documents}

    def will_start(self, document, name, active, history_current=True):
        registration = self.registrations[document]
        normalized = normalize_command(name)
        registration.pending = None
        if normalized is not None and active and history_current:
            registration.pending = normalized

    def ended(self, document, name, active):
        registration = self.registrations[document]
        normalized = normalize_command(name)
        pending = registration.pending
        registration.pending = None
        return normalized is not None and normalized == pending and active

    def aborted(self, document):
        registration = self.registrations[document]
        registration.pending = None

    def detach(self, document):
        self.registrations.pop(document)


intent = CommandIntent("A", "B")
if intent.ended("A", "UNDO", True):
    errors.append("an unmatched internal CommandEnded(UNDO) reached semantic history")
intent.will_start("A", "UNDO", False)
if intent.ended("A", "UNDO", True):
    errors.append("an inactive-document Undo start armed semantic history")
intent.will_start("A", "UNDO", True)
if intent.ended("A", "UNDO", False):
    errors.append("an Undo completion after document deactivation reached semantic history")
intent.will_start("A", "UNDO", True)
intent.aborted("A")
if intent.ended("A", "UNDO", True):
    errors.append("a cancelled/failed Undo left stale intent")
intent.will_start("A", "UNDO", True)
if intent.ended("A", "REDO", True):
    errors.append("a mismatched terminal event consumed Undo intent")
intent.will_start("A", "UNDO", True)
intent.will_start("A", "UNDO", True)
if not intent.ended("A", "UNDO", True):
    errors.append("a stale same-document Undo start suppressed the latest explicit pair")
intent.will_start("A", "_UNDO", True)
if not intent.ended("A", ".UNDO", True):
    errors.append("a matched active-document native Undo did not reach history")
intent.will_start("A", "REDO", True)
if not intent.ended("A", "REDO", True):
    errors.append("a matched active-document native Redo did not reach history")
intent.will_start("A", "U", True)
if intent.ended("A", "U", True):
    errors.append("single-letter U armed semantic Undo history")

# V25 can omit the terminal callback when a modal command closes its own
# document. That unrelated, unbalanced event must not globally latch out the
# next deliberate paired Undo for A. A same-revision paired event remains a
# no-op in production; a changed marker is verified against exact history.
intent = CommandIntent("A", "B")
intent.will_start("B", "QS3DSRTCHECKB", True)
intent.detach("B")
intent.will_start("A", "UNDO", True)
if not intent.ended("A", "UNDO", True):
    errors.append("an unbalanced command in a detached document suppressed deliberate Undo")
intent.will_start("A", "REDO", True)
if not intent.ended("A", "REDO", True):
    errors.append("an unbalanced command in a detached document suppressed deliberate Redo")

# A usable Undo terminal callback is an optimization, not the sole coherence
# boundary. If V25 omits it, the next active-document command must reconcile
# the already-committed marker before its command body observes ProjectState.
class CommandBoundaryReconciliation:
    def __init__(self):
        self.current_revision = "final"
        self.native_revision = "final"
        self.semantic_state = "final"
        self.entries = {"before": "before", "final": "final"}

    def synchronize(self, active=True, current_history=True, exact_project=True):
        if not active or not current_history or not exact_project:
            return False
        if self.native_revision == self.current_revision:
            return False
        if self.native_revision not in self.entries:
            return False
        self.semantic_state = self.entries[self.native_revision]
        self.current_revision = self.native_revision
        return True


boundary = CommandBoundaryReconciliation()
boundary.native_revision = "before"  # native Undo completed; terminal callback was missed
if boundary.synchronize(active=False) or boundary.semantic_state != "final":
    errors.append("inactive-document command start crossed marker/history affinity")
if not boundary.synchronize() or boundary.semantic_state != "before" or boundary.current_revision != "before":
    errors.append("next active-document command did not recover a missed native Undo before its body")
if boundary.synchronize() or boundary.semantic_state != "before":
    errors.append("same-marker command start was not an observational no-op")
boundary.native_revision = "unknown"
if boundary.synchronize() or boundary.semantic_state != "before" or boundary.current_revision != "before":
    errors.append("unknown native revision mutated semantic state at command start")
boundary.native_revision = "final"
if boundary.synchronize(current_history=False) or boundary.semantic_state != "before":
    errors.append("stale/replaced history restored semantic state at command start")
if boundary.synchronize(exact_project=False) or boundary.semantic_state != "before":
    errors.append("replaced canonical project restored semantic state at command start")
if not boundary.synchronize() or boundary.semantic_state != "final" or boundary.current_revision != "final":
    errors.append("next active-document command did not recover a missed native Redo before its body")

# State classification is independent of command provenance. Internal BricsCAD
# work can emit a complete native command pair, so read-only refusal must rely
# on marker mismatch for fail-closed behavior instead of poisoning history.
def project_sanitized_history_state(cause):
    return "DESYNCHRONIZED" if cause == "RESTORE_RECOVERY_FAILED" else "NONE"


for cause in ("COMMIT_HISTORY_LOST", "HISTORY_AFFINITY_MISMATCH", "CACHE_PROJECT_MISMATCH", "NONE"):
    if project_sanitized_history_state(cause) != "NONE":
        errors.append("non-live desync cause escaped existing NONE projection: " + cause)
if project_sanitized_history_state("RESTORE_RECOVERY_FAILED") != "DESYNCHRONIZED":
    errors.append("live restore/recovery failure lost its DESYNCHRONIZED projection")


class ObserverState:
    def __init__(self):
        self.project = "canonical-before"
        self.current_revision = "known-current"
        self.native_revision = self.current_revision
        self.desynchronized = False
        self.current_stamp = "tracked-stamp"
        self.project_stamp = self.current_stamp

    def can_begin(self):
        return not self.desynchronized and self.native_revision == self.current_revision

    def refuse_read_only(self, native_revision):
        self.native_revision = native_revision

    def restore_failed_but_recovered(self, native_revision):
        self.native_revision = native_revision

    def restore_and_recovery_failed(self, native_revision):
        self.native_revision = native_revision
        self.desynchronized = True


for refusal in ("unknown-revision", "missing-project", "changed-backing-store"):
    state = ObserverState()
    before = (state.project, state.current_revision)
    state.refuse_read_only(refusal)
    if (state.project, state.current_revision) != before or state.desynchronized:
        errors.append("read-only observer refusal mutated project/revision or became sticky: " + refusal)
    if state.can_begin():
        errors.append("persistent native marker mismatch did not fail closed: " + refusal)
    state.native_revision = state.current_revision
    if not state.can_begin():
        errors.append("returning the marker to CurrentRevision did not permit safe retry: " + refusal)

state = ObserverState()
before = (state.project, state.current_revision)
state.restore_failed_but_recovered("known-target")
if (state.project, state.current_revision) != before or state.desynchronized:
    errors.append("successful restore recovery did not preserve canonical project/current revision")
if state.can_begin():
    errors.append("recovered restore failure weakened persistent marker-mismatch fail-closed behavior")
state.native_revision = state.current_revision
state.project_stamp = "intervening-semantic-edit"
if not state.can_begin() or state.project_stamp == state.current_stamp:
    errors.append("marker recovery did not permit safe semantic rebase/retry")

state = ObserverState()
state.restore_and_recovery_failed("known-target")
state.native_revision = state.current_revision
if not state.desynchronized or state.can_begin():
    errors.append("combined semantic restore/recovery failure must remain sticky and fail closed")

published = "first-success"
shadow = "failed-reconcile"
shadow = None  # transaction/transition disposed without publication
if published != "first-success":
    errors.append("aborted reconcile advanced published semantic history")
shadow = "final-success"
published = shadow  # ConfirmCommitted after native commit
if published != "final-success":
    errors.append("final successful reconcile did not publish its staged semantic revision")
published = "first-success"  # native Undo marker observation
published = "final-success"  # native Redo marker observation
if published != "final-success":
    errors.append("deterministic Undo/Redo shadow-history sequence drifted")

for token in (
    "SourceReconcileUndoCoordinator.Attach(docs.MdiActiveDocument)",
    "SourceReconcileUndoCoordinator.Attach(e.Document)",
    "SourceReconcileUndoCoordinator.Detach(document)",
    "SourceReconcileUndoCoordinator.Stop()",
):
    if token not in lifecycle:
        errors.append("Document lifecycle missing Undo coordination: " + token)

for token in (
    "public static bool TryGetCached(Document document, out ProjectState project)",
    "SourceReconcileUndoCoordinator.Forget(document);",
):
    if token not in projects:
        errors.append("Project cache lifecycle missing exact Undo identity/cleanup contract: " + token)
if projects.count("SourceReconcileUndoCoordinator.Forget(document);") < 3:
    errors.append("Reload, Forget and ForgetByName must each invalidate Source Reconcile Undo history")

forget_start = coordinator.find("public static void Forget(")
begin_start_for_forget = coordinator.find("public static PendingTransition BeginTransition(", forget_start)
forget_body = coordinator[forget_start:begin_start_for_forget]
if (
    forget_start < 0
    or begin_start_for_forget < 0
    or "Histories.Remove(document);" not in forget_body
    or "registration.PendingCommand = null;" not in forget_body
):
    errors.append("project forget/reload must clear both semantic history and any pending native command intent")

for token in (
    "native revision marker",
    "same CAD transaction",
    "Undo/Redo",
    "global native command names `UNDO`, `REDO` and `MREDO`",
    "single-letter `U`",
    "canonical cached project",
    "#1005",
):
    if token not in runbook:
        errors.append("Source Reconcile runbook missing Undo contract: " + token)

local004_start = inbox.find("## LOCAL-004")
local004_end = inbox.find("\n## LOCAL-005", local004_start)
local004 = inbox[local004_start:local004_end] if local004_start >= 0 and local004_end > local004_start else ""
for token in ("#1005", "Undo/Redo", "exact fixed-SHA rerun", "PENDING_LOCAL"):
    if token not in local004:
        errors.append("LOCAL-004 inbox missing production-fix rerun boundary: " + token)

print("QS3D Source Reconcile native Undo/Redo semantic-coherence preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print(
    "PASS: Source Reconcile writes a document-scoped revision marker in its native transaction, "
    "keeps semantic snapshots private until commit, ignores non-Undo command completion, restores only the exact cached project after native Undo/Redo, "
    "and clears history on project/document lifecycle changes without weakening command-failure rollback or failed-reconcile recovery."
)
