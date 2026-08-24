#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticTagBuilder.cs"
UNDO = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR: LOCAL-006 semantic-tag native Undo semantic preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str, start: int = 0) -> int:
    pos = text.find(token, start)
    if pos < 0:
        fail(label + " missing contract token: " + token)
    return pos


def main() -> int:
    if not BUILDER.is_file():
        fail("missing SemanticTagBuilder.cs")
    if not UNDO.is_file():
        fail("missing SourceReconcileUndoCoordinator.cs")

    builder = BUILDER.read_text(encoding="utf-8")
    undo = UNDO.read_text(encoding="utf-8")

    rollback = require(builder, "var rollback = ProjectStateSnapshot.Capture(project);", "semantic tag builder")
    stamp = require(builder, "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);", "semantic tag builder", rollback)
    previous = require(builder, "var previous = ValidatePrevious(document.Database, project, element, ownership);", "semantic tag builder", stamp)
    begin = require(builder, "undoTransition = SourceReconcileUndoCoordinator.BeginTransition(", "semantic tag builder", previous)
    marker = require(builder, "undoTransition.StageNativeMarker();", "semantic tag builder", begin)
    erase = require(builder, "ErasePrevious(transaction, project, element, previous);", "semantic tag builder", marker)
    append = require(builder, "owner.AppendEntity(tag);", "semantic tag builder", marker)
    native_owner = require(builder, "GeneratedGeometryService.MarkGenerated(document, transaction, tag, project.ProjectId, element.Id, element.Category);", "semantic tag builder", append)
    semantic_owner = require(builder, "element.Properties[GeneratedSemanticTagHealthService.HandlesKey] = generatedHandle;", "semantic tag builder", native_owner)
    audit = require(builder, "AuditTrail.ForProject(project).Record(", "semantic tag builder", semantic_owner)
    after_capture = require(builder, "var afterSnapshot = ProjectStateSnapshot.Capture(project);", "semantic tag builder", audit)
    stage_after = require(builder, "undoTransition.StageAfter(project, afterSnapshot);", "semantic tag builder", after_capture)
    commit = require(builder, "transaction.Commit();", "semantic tag builder", stage_after)
    confirm = require(builder, "undoTransition?.ConfirmCommitted();", "semantic tag builder", commit)
    committed = require(builder, "cadCommitted = true;", "semantic tag builder", confirm)
    restore = require(builder, "rollback.Restore(project)", "semantic tag builder", committed)
    dispose = require(builder, "undoTransition?.Dispose();", "semantic tag builder", restore)

    if not rollback < stamp < previous < begin < marker:
        fail("before snapshot/revision and previous-ownership validation must precede native history staging")
    if marker >= min(erase, append):
        fail("native revision marker must be staged before retiring/appending semantic-tag CAD topology")
    if not max(erase, append) < native_owner < semantic_owner < audit < after_capture < stage_after < commit < confirm < committed < restore < dispose:
        fail("generated ownership/audit must enter the semantic after-snapshot before CAD commit; history publication/rollback/dispose ordering changed")

    if builder.count("if (!SourceReconcileUndoCoordinator.IsExternalTransitionActive(document))") < 2:
        fail("builder lost external-transition suppression around before/after history staging")
    if "SourceReconcileUndoCoordinator.PendingTransition? undoTransition = null;" not in builder:
        fail("builder no longer owns a disposable pending semantic/native transition")

    for token in (
        "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "GeneratedGeometryService.MarkGenerated",
        "RequireSupportedSemanticTag(entity, item.Key",
        "if (!(entity is MText) && !(entity is MLeader))",
        "RequireSingleSourceHandle(element)",
        "GeneratedSemanticTagHealthService.PositionScopeKey",
        "GeneratedSemanticTagHealthService.ArtifactKindKey",
        "ProjectStateSnapshot.Capture(project)",
    ):
        require(builder, token, "preserved semantic-tag safety contract")

    for forbidden in (
        "document.CommandEnded +=",
        "document.CommandWillStart +=",
        "new Dictionary<string, ProjectStateSnapshot>",
        "SourceReconcileUndoCoordinator.CommitExternalTransition",
    ):
        if forbidden in builder:
            fail("semantic tag builder introduced a competing Undo observer/history path: " + forbidden)

    for token in (
        "if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);",
        "if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;",
        'if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";',
        'if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";',
        'if (string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)) return "MREDO";',
        "targetEntry.Snapshot.Restore(project);",
    ):
        require(undo, token, "production native semantic Undo coordinator")

    print("PASS: LOCAL-006 semantic MText/MLeader replacement validates retiring ownership before mutation, stages one native revision before erase/append, captures generated ownership/audit/version in the semantic after-snapshot before CAD commit, publishes history only after commit, preserves pre-commit rollback/document affinity/health metadata, and reuses the hardened production Undo/Redo observer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
