#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs"
UNDO = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR: opening-boolean native Undo semantic preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str, start: int = 0) -> int:
    pos = text.find(token, start)
    if pos < 0:
        fail(label + " missing contract token: " + token)
    return pos


def main() -> int:
    for path, label in (
        (SERVICE, "OpeningBooleanService.cs"),
        (COMMANDS, "OpeningBooleanCommands.cs"),
        (UNDO, "SourceReconcileUndoCoordinator.cs"),
    ):
        if not path.is_file():
            fail("missing " + label)

    service = SERVICE.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
    undo = UNDO.read_text(encoding="utf-8")

    rollback = require(service, "var rollback = ProjectStateSnapshot.Capture(project);", "opening boolean service")
    stamp = require(service, "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);", "opening boolean service", rollback)
    pending_transition = require(service, "SourceReconcileUndoCoordinator.PendingTransition? undoTransition = null;", "opening boolean service", stamp)
    cut_gate = require(service, "if (cutsToApply.Count > 0)", "opening boolean service", pending_transition)
    ensure_before_cut = require(service, "EnsureUndoTransition(", "opening boolean service", cut_gate)
    boolean = require(service, "hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);", "opening boolean service", ensure_before_cut)
    pending_gate = require(service, "if (pending.Count > 0)", "opening boolean service", boolean)
    ensure_before_semantic = require(service, "EnsureUndoTransition(", "opening boolean service", pending_gate)
    semantic = require(service, "foreach (var update in pending) CommitSemanticUpdate(project, update);", "opening boolean service", ensure_before_semantic)
    after_capture = require(service, "var afterSnapshot = ProjectStateSnapshot.Capture(project);", "opening boolean service", semantic)
    stage_after = require(service, "undoTransition.StageAfter(project, afterSnapshot);", "opening boolean service", after_capture)
    commit = require(service, "transaction.Commit();", "opening boolean service", stage_after)
    confirm = require(service, "undoTransition?.ConfirmCommitted();", "opening boolean service", commit)
    committed = require(service, "cadCommitted = true;", "opening boolean service", confirm)
    restore = require(service, "rollback.Restore(project)", "opening boolean service", committed)
    dispose = require(service, "undoTransition?.Dispose();", "opening boolean service", restore)

    if not rollback < stamp < pending_transition < cut_gate < ensure_before_cut < boolean:
        fail("service must capture semantic before-state and stage its native revision before the first destructive boolean")
    if not boolean < pending_gate < ensure_before_semantic < semantic < after_capture < stage_after < commit < confirm < committed < restore < dispose:
        fail("service semantic after-state/history publication/rollback/dispose ordering changed")

    helper = require(service, "private static void EnsureUndoTransition(", "opening boolean service")
    external_suppression = require(service, "SourceReconcileUndoCoordinator.IsExternalTransitionActive(document)", "opening boolean service", helper)
    begin = require(service, "undoTransition = SourceReconcileUndoCoordinator.BeginTransition(", "opening boolean service", external_suppression)
    marker = require(service, "undoTransition.StageNativeMarker();", "opening boolean service", begin)
    if not helper < external_suppression < begin < marker:
        fail("direct/shared service path lost external-scope suppression or native marker staging")

    command_before = require(commands, "var beforeSnapshot = ProjectStateSnapshot.Capture(project);", "opening boolean command")
    command_stamp = require(commands, "var beforeStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);", "opening boolean command", command_before)
    scope = require(commands, "using (SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document))", "opening boolean command", command_stamp)
    service_call = require(commands, "OpeningBooleanService.CutLinkedOpenings(document, project", "opening boolean command", scope)
    live_stamp = require(commands, "PhysicalOpeningCutLiveStateService.StampStraight(document, project, openingIds);", "opening boolean command", service_call)
    changed = require(commands, "if (!beforeStamp.Matches(project))", "opening boolean command", live_stamp)
    external_commit = require(commands, "SourceReconcileUndoCoordinator.CommitExternalTransition(", "opening boolean command", changed)
    finalize = require(commands, "FinalizeUi(document, message + liveNote);", "opening boolean command", external_commit)
    if not command_before < command_stamp < scope < service_call < live_stamp < changed < external_commit < finalize:
        fail("command must wrap service + straight live-state stamp in one external transition before UI completion")

    for token in (
        'update.Host.Properties["PhysicalOpeningCutSolidHandle"]',
        'update.Host.Properties["PhysicalOpeningCutFingerprint"]',
        'update.Host.Properties["PhysicalOpeningCutCount"]',
        "PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);",
        '"geometry.opening.boolean"',
        "PhysicalOpeningCutLiveStateService.StampStraight",
        "ProjectStateSnapshot.Capture(project)",
    ):
        if token not in service and token not in commands:
            fail("physical-opening semantic coverage token missing: " + token)

    for forbidden in (
        "document.CommandEnded +=",
        "document.CommandWillStart +=",
        "new Dictionary<string, ProjectStateSnapshot>",
    ):
        if forbidden in service or forbidden in commands:
            fail("opening-cut path introduced a competing Undo observer/history: " + forbidden)

    for token in (
        "if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);",
        "if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;",
        'if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";',
        'if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";',
        "targetEntry.Snapshot.Restore(project);",
        "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    ):
        require(undo, token, "production native semantic Undo coordinator")

    print("PASS: straight physical opening cuts stage direct-service semantic/native history before destructive boolean work, suppress nested history under the command scope, capture cut metadata/audit before CAD commit, and checkpoint the post-cut live fingerprint in the same document-bound Undo revision.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
