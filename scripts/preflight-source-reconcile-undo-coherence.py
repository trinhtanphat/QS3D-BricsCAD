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
    "history.Entries.Clear();",
    "history.Entries.Add(previousRevision, new HistoryEntry(beforeSnapshot, ProjectRevisionStamp.Capture(project)))",
):
    if token not in coordinator:
        errors.append("Undo coordinator missing contract: " + token)

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate(",
    "ProjectContextCoordinator.TryGetReadOnly(",
    "Application.DocumentManager.MdiActiveDocument =",
    "SendStringToExecute",
):
    if forbidden in coordinator:
        errors.append("Undo observer must not load/create/switch/drive a project or command: " + forbidden)

begin = service.find("SourceReconcileUndoCoordinator.BeginTransition(")
prepare = service.find("GeneratedDependentGeometryInvalidator.Prepare", begin)
refresh = service.find("RefreshSourceDerivedState(project", prepare)
metadata = service.find("invalidation.CommitMetadata()", refresh)
stage = service.find("undoTransition.StageAfter(project, ProjectStateSnapshot.Capture(project));", metadata)
commit = service.find("transaction.Commit();", stage)
confirm = service.find("undoTransition.ConfirmCommitted();", commit)
committed = service.find("cadCommitted = true;", confirm)
restore = service.find("rollback.Restore(project);", committed)
positions = (begin, prepare, refresh, metadata, stage, commit, confirm, committed, restore)
if any(position < 0 for position in positions) or list(positions) != sorted(positions):
    errors.append(
        "Source Reconcile must register marker -> invalidate/refresh -> stage semantic Redo -> native commit -> confirm, while preserving pre-commit semantic rollback"
    )

snapshot = service.find("var rollback = ProjectStateSnapshot.Capture(project);")
transaction = service.find("document.Database.TransactionManager.StartTransaction()", snapshot)
if min(snapshot, transaction, begin) < 0 or not snapshot < transaction < begin:
    errors.append("Source Reconcile must capture rollback before opening the native transaction/Undo transition")
if "if (!cadCommitted)" not in service or "new AggregateException(operationError, restoreError)" not in service:
    errors.append("Source Reconcile command-failure semantic rollback contract drifted")

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
    "stages semantic snapshots before commit, restores only the exact cached project after native Undo/Redo, "
    "and clears history on project/document lifecycle changes without weakening command-failure rollback."
)
