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
    "public Database Database { get; }",
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
    "OnCommandAborted(document)",
    "TryConsumeMatchingCommand(document, args?.GlobalCommandName)",
    "NormalizeNativeUndoRedo(args?.GlobalCommandName)",
    "registration.ActiveCommandDepth++",
    "registration.ActiveCommandDepth--",
    "HasActiveCommand()",
    "_suppressUndoUntilStableCommand",
    "IsActiveDocument(document)",
    "IsSameNativeDrawing(history, document)",
    "ReferenceEquals(history.Database, document.Database)",
    "active != null && IsSameNativeDrawing(active, document)",
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
    "modelSpace.GetXDataForApplication(RegAppName)",
    "modelSpace.XData = marker",
    "OpenModelSpace(document.Database, transaction, OpenMode.ForWrite)",
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

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate(",
    "ProjectContextCoordinator.TryGetReadOnly(",
    "Application.DocumentManager.MdiActiveDocument =",
    "SendStringToExecute",
    "_history.CurrentRevision = _nextRevision",
    "history.Entries.Clear();",
    'string.Equals(normalized, "U", StringComparison.OrdinalIgnoreCase)',
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
transaction = service.find("document.Database.TransactionManager.StartTransaction()", snapshot)
if min(snapshot, transaction, begin) < 0 or not snapshot < transaction < begin:
    errors.append("Source Reconcile must capture rollback before the native transaction and delay the Undo transition until all reconcile work is ready")
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
    if diagnostic.count("!IsSameNativeDrawing(history, document)") != 2:
        errors.append("sanitized history affinity must use exact native Database identity across managed Document wrappers")
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
aborted_start = coordinator.find("private static void OnCommandAborted(", ended_start)
consume_start = coordinator.find("private static bool TryConsumeMatchingCommand(", aborted_start)
active_start = coordinator.find("private static bool IsActiveDocument(", consume_start)
same_history_start = coordinator.find("private static bool IsSameNativeDrawing(DocumentHistory history", active_start)
same_documents_start = coordinator.find("private static bool IsSameNativeDrawing(Document left", same_history_start)
normalize_start = coordinator.find("private static string? NormalizeNativeUndoRedo(", active_start)
mark_start = coordinator.find("private static InvalidOperationException MarkDesynchronized(", normalize_start)
stage_body = coordinator[stage_start:confirm_start]
confirm_body = coordinator[confirm_start:dispose_start]
begin_body = coordinator[begin_start:will_start]
will_body = coordinator[will_start:ended_start]
ended_body = coordinator[ended_start:aborted_start]
aborted_body = coordinator[aborted_start:consume_start]
consume_body = coordinator[consume_start:active_start]
filter_body = coordinator[normalize_start:mark_start]
if min(
    stage_start, confirm_start, dispose_start, begin_start, will_start,
    ended_start, aborted_start, consume_start, active_start, same_history_start,
    same_documents_start, normalize_start, mark_start,
) < 0:
    errors.append("Undo coordinator transition method boundaries are missing")
else:
    if "modelSpace.XData = marker" not in stage_body or "_stagedEntries = stagedEntries;" not in stage_body:
        errors.append("native marker must be written only after the private shadow history is fully staged")
    if "modelSpace.XData = marker" in begin_body or "EnsureRegApp" in begin_body:
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
        "if (registration.PendingCommand != null)" not in will_body
        or "registration.PendingCommand = null;" not in will_body
        or "var nested = HasActiveCommand();" not in will_body
        or "var topLevel = !_suppressUndoUntilStableCommand && !nested;" not in will_body
        or "registration.ActiveCommandDepth++;" not in will_body
        or "normalized == null || !topLevel || !IsActiveDocument(document)" not in will_body
        or "registration.PendingCommand = normalized;" not in will_body
    ):
        errors.append("Undo observer must arm one globally top-level active-document native command token and invalidate nested starts")
    consume_gate = ended_body.find("if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;")
    marker_read_start = ended_body.find("try { nativeRevision = ReadRevision(document); }")
    if consume_gate < 0 or marker_read_start < 0 or consume_gate > marker_read_start:
        errors.append("CommandEnded must consume a matching start token before inspecting or poisoning native history")
    if (
        "var pendingCommand = registration.PendingCommand;" not in consume_body
        or "registration.PendingCommand = null;" not in consume_body
        or "if (registration.ActiveCommandDepth <= 0) return false;" not in consume_body
        or "registration.ActiveCommandDepth--;" not in consume_body
        or "string.Equals(normalized, pendingCommand, StringComparison.Ordinal)" not in consume_body
        or "IsActiveDocument(document)" not in consume_body
    ):
        errors.append("Undo completion must clear intent and match command plus active document exactly")
    active_body = coordinator[active_start:same_history_start]
    same_history_body = coordinator[same_history_start:same_documents_start]
    same_documents_body = coordinator[same_documents_start:normalize_start]
    if "active != null && IsSameNativeDrawing(active, document)" not in active_body:
        errors.append("active-document Undo authority must accept only the same native drawing across managed wrappers")
    if (
        "ReferenceEquals(history.Document, document)" not in same_history_body
        or "ReferenceEquals(history.Database, document.Database)" not in same_history_body
        or ".Name" in same_history_body
    ):
        errors.append("history affinity must use wrapper-or-exact-Database identity, never drawing name/path")
    if (
        "ReferenceEquals(left, right)" not in same_documents_body
        or "ReferenceEquals(left.Database, right.Database)" not in same_documents_body
        or ".Name" in same_documents_body
    ):
        errors.append("document affinity must use wrapper-or-exact-Database identity, never drawing name/path")
    if (
        "registration.PendingCommand = null;" not in aborted_body
        or "registration.ActiveCommandDepth--;" not in aborted_body
    ):
        errors.append("cancelled and failed commands must clear pending Undo intent and close command depth")

    target_start = ended_body.find("HistoryEntry targetEntry;", marker_read_start)
    stamp_start = ended_body.find("if (!currentEntry.Stamp.Matches(project))", target_start)
    restore_start = ended_body.find("var restoreRollback = ProjectStateSnapshot.Capture(project);", stamp_start)
    rollback_failure_start = ended_body.find("catch (Exception rollbackError)", restore_start)
    recovered_restore_start = ended_body.find(
        '"Source Reconcile semantic Undo restore failed and was recovered.',
        rollback_failure_start,
    )
    advance_start = ended_body.find("history.CurrentRevision = nativeRevision;", recovered_restore_start)
    if min(
        marker_read_start, target_start, stamp_start, restore_start,
        rollback_failure_start, recovered_restore_start, advance_start,
    ) < 0:
        errors.append("Undo observer marker/stamp refusal boundaries are missing")
    else:
        if "MarkDesynchronized(" in ended_body[marker_read_start:target_start]:
            errors.append("a transient command-end marker read failure must not permanently poison history")
        if "MarkDesynchronized(" in ended_body[stamp_start:restore_start]:
            errors.append("semantic-only drift refusal must remain recoverable when the native marker returns current")
        read_only_refusals = ended_body[target_start:restore_start]
        if "MarkDesynchronized(" in read_only_refusals or "history.Desynchronized = true" in read_only_refusals:
            errors.append("read-only unknown-revision/project/backing-store refusals must not permanently poison history")
        if "MarkDesynchronized(" in ended_body[recovered_restore_start:advance_start]:
            errors.append("a failed semantic restore followed by successful exact rollback must remain recoverable")
        recovery_body = ended_body[restore_start:rollback_failure_start]
        if (
            "restoreRollback.Restore(project);" not in recovery_body
            or "if (!currentEntry.Stamp.Matches(project))" not in recovery_body
            or '"Recovered Source Reconcile semantic state does not match its current revision."' not in recovery_body
        ):
            errors.append("restore recovery must verify the canonical project returned to its current revision before remaining nonsticky")
        if ended_body.count("MarkDesynchronized(") != 1:
            errors.append("command-end observation may become sticky only when semantic restore and recovery both fail")
        double_failure = ended_body[rollback_failure_start:recovered_restore_start]
        if "MarkDesynchronized(history," not in double_failure or "new AggregateException(restoreError, rollbackError)" not in double_failure:
            errors.append("sticky desync must remain bound to combined semantic restore and recovery failure")
        mark_body = coordinator[mark_start:coordinator.find("private static void RequireCurrentHistory(", mark_start)]
        if "history.MarkDesynchronized(DesynchronizationCause.RestoreRecoveryFailed);" not in mark_body:
            errors.append("live sticky history must carry the RESTORE_RECOVERY_FAILED sanitized cause")
        require_start = coordinator.find("private static void RequireCurrentHistory(", mark_start)
        require_end = coordinator.find("private static string ReadRevision(Document document)", require_start)
        require_body = coordinator[require_start:require_end]
        if (
            "!IsSameNativeDrawing(history, document)" not in require_body
            or "!ReferenceEquals(history.Project, project)" not in require_body
            or "!string.Equals(history.ProjectId, project.ProjectId" not in require_body
            or "!string.Equals(history.CurrentRevision, nativeRevision" not in require_body
        ):
            errors.append("transition affinity must normalize only Document wrappers while retaining project/revision fail-closed guards")
        if "CurrentRevision =" in ended_body[target_start:advance_start]:
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
    def __init__(self, database):
        self.database = database


def same_native_drawing(left, right):
    return left is right or left.database is right.database


database_a = object()
database_b = object()
document_a_first_wrapper = ManagedDocument(database_a)
document_a_after_mdi_switch = ManagedDocument(database_a)
document_b = ManagedDocument(database_b)
if not same_native_drawing(document_a_first_wrapper, document_a_after_mdi_switch):
    errors.append("same native drawing was rejected after managed Document wrapper replacement")
if same_native_drawing(document_a_first_wrapper, document_b):
    errors.append("different native Database objects crossed document affinity")


class CommandRegistration:
    def __init__(self):
        self.pending = None
        self.depth = 0


class CommandIntent:
    def __init__(self, *documents):
        self.registrations = {document: CommandRegistration() for document in documents}
        self.suppress_until_stable = False

    def has_active_command(self):
        return any(registration.depth > 0 for registration in self.registrations.values())

    def will_start(self, document, name, active, history_current=True):
        registration = self.registrations[document]
        normalized = normalize_command(name)
        nested = self.has_active_command()
        if self.suppress_until_stable and normalized is None and not nested and active:
            self.suppress_until_stable = False
        top_level = not self.suppress_until_stable and not nested
        registration.depth += 1
        if registration.pending is not None:
            registration.pending = None
            return
        if normalized is not None and top_level and active and history_current:
            registration.pending = normalized

    def ended(self, document, name, active):
        registration = self.registrations[document]
        normalized = normalize_command(name)
        pending = registration.pending
        registration.pending = None
        if registration.depth <= 0:
            return False
        registration.depth -= 1
        return normalized is not None and normalized == pending and active

    def aborted(self, document):
        registration = self.registrations[document]
        registration.pending = None
        if registration.depth > 0:
            registration.depth -= 1

    def detach(self, document):
        registration = self.registrations.pop(document)
        if registration.depth > 0:
            self.suppress_until_stable = True


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
if intent.ended("A", "UNDO", True):
    errors.append("ambiguous duplicate starts retained native Undo intent")
intent.ended("A", "UNDO", True)
intent.will_start("A", "_UNDO", True)
if not intent.ended("A", ".UNDO", True):
    errors.append("a matched active-document native Undo did not reach history")
intent.will_start("A", "REDO", True)
if not intent.ended("A", "REDO", True):
    errors.append("a matched active-document native Redo did not reach history")
intent.will_start("A", "U", True)
if intent.ended("A", "U", True):
    errors.append("single-letter U armed semantic Undo history")

# A modal command in B can activate A and close B. Any complete native Undo
# pair emitted for A while B's command is live is internal host work, not user
# Undo authority. If B is detached before its terminal event, suppression must
# survive that missing callback until A reaches an ordinary stable boundary.
intent = CommandIntent("A", "B")
intent.will_start("B", "QS3DSRTCHECKB", True)
intent.will_start("A", "UNDO", True)
if intent.ended("A", "UNDO", True):
    errors.append("cross-document nested Undo reached Source Reconcile history")
intent.detach("B")
intent.will_start("A", "REDO", True)
if intent.ended("A", "REDO", True):
    errors.append("post-detach internal Redo escaped incomplete-command suppression")
intent.will_start("A", "QS3DSRTSELECTSOURCES", True)
intent.ended("A", "QS3DSRTSELECTSOURCES", True)
intent.will_start("A", "UNDO", True)
if not intent.ended("A", "UNDO", True):
    errors.append("stable ordinary command boundary did not re-enable deliberate top-level Undo")

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
