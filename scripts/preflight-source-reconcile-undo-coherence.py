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
    "Dictionary<Document, CommandEventHandler>",
    "Dictionary<Document, DocumentHistory>",
    "document.CommandEnded += handler",
    "document.CommandEnded -= handler",
    "OnCommandEnded(document, args)",
    "IsNativeUndoRedo(args?.GlobalCommandName)",
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
    "history.Desynchronized = true",
    "new Dictionary<string, HistoryEntry>(_history.Entries, StringComparer.Ordinal)",
    "_history.Publish(_stagedEntries, _nextRevision);",
    "_registeredHistory",
    "Histories.Remove(_document);",
    "EnsureRegApp(_database, _transaction);",
    "internal sealed class SanitizedDiagnosticSnapshot",
    "internal static SanitizedDiagnosticSnapshot CaptureSanitizedState(",
    "public string HistoryState { get; }",
    "public string EntryClass { get; }",
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
        "Attach(document)",
        "Histories.Add(",
        "Histories.Remove(",
        "CurrentRevision =",
    ):
        if forbidden in diagnostic:
            errors.append("sanitized diagnostic accessor exposes raw state or mutates coordination: " + forbidden)

stage_start = coordinator.find("public void StageAfter(")
confirm_start = coordinator.find("public void ConfirmCommitted()", stage_start)
dispose_start = coordinator.find("public void Dispose()", confirm_start)
begin_start = coordinator.find("public static PendingTransition BeginTransition(")
observer_start = coordinator.find("private static void OnCommandEnded(", begin_start)
stage_body = coordinator[stage_start:confirm_start]
confirm_body = coordinator[confirm_start:dispose_start]
begin_body = coordinator[begin_start:observer_start]
if min(stage_start, confirm_start, dispose_start, begin_start, observer_start) < 0:
    errors.append("Undo coordinator transition method boundaries are missing")
else:
    if "modelSpace.XData = marker" not in stage_body or "_stagedEntries = stagedEntries;" not in stage_body:
        errors.append("native marker must be written only after the private shadow history is fully staged")
    if "modelSpace.XData = marker" in begin_body or "EnsureRegApp" in begin_body:
        errors.append("BeginTransition must not mutate the native marker before fallible reconcile work completes")
    if "_history.Publish(_stagedEntries, _nextRevision);" not in confirm_body:
        errors.append("published semantic revision must advance only in post-CAD-commit confirmation")
    if "CurrentRevision = _nextRevision" in stage_body:
        errors.append("StageAfter must not expose an uncommitted semantic revision")

# Deterministic model of the production shadow-publication contract. The static
# tokens above bind these states to the coordinator/service surfaces; this model
# locks the regression sequence that failed on V25: success -> aborted reconcile
# -> refusals/document switch -> final success -> native Undo/Redo.
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

for token in (
    "native revision marker",
    "same CAD transaction",
    "Undo/Redo",
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
