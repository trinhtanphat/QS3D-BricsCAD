#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabFoundationMultiRegionMeshSolidBuilder.cs"
UNDO = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"


def fail(message: str) -> None:
    print("ERROR: LOCAL-005 multi-region native Undo semantic preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str, start: int = 0) -> int:
    pos = text.find(token, start)
    if pos < 0:
        fail(label + " missing contract token: " + token)
    return pos


def main() -> int:
    if not BUILDER.is_file():
        fail("missing SlabFoundationMultiRegionMeshSolidBuilder.cs")
    if not UNDO.is_file():
        fail("missing SourceReconcileUndoCoordinator.cs")

    builder = BUILDER.read_text(encoding="utf-8")
    undo = UNDO.read_text(encoding="utf-8")

    rollback = require(builder, "var rollback = ProjectStateSnapshot.Capture(project);", "multi-region builder")
    stamp = require(builder, "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);", "multi-region builder", rollback)
    cap = require(builder, "if (layout.TotalBarCount > MaxBarsPerBatch)", "multi-region builder", stamp)
    ownership = require(builder, "var previous = ValidateCompletePreviousOwnership(", "multi-region builder", cap)
    begin = require(builder, "undoTransition = SourceReconcileUndoCoordinator.BeginTransition(", "multi-region builder", ownership)
    marker = require(builder, "undoTransition.StageNativeMarker();", "multi-region builder", begin)
    append = require(builder, "modelSpace.AppendEntity(bar);", "multi-region builder", marker)
    erase = require(builder, "entity.Erase();", "multi-region builder", marker)
    semantic = require(builder, "element.Properties[configuration.HandlesKey] = string.Join(\";\", allHandles);", "multi-region builder", append)
    audit = require(builder, "AuditTrail.ForProject(project).Record(configuration.AuditAction", "multi-region builder", semantic)
    after_capture = require(builder, "var afterSnapshot = ProjectStateSnapshot.Capture(project);", "multi-region builder", audit)
    stage_after = require(builder, "undoTransition.StageAfter(project, afterSnapshot);", "multi-region builder", after_capture)
    commit = require(builder, "transaction.Commit();", "multi-region builder", stage_after)
    confirm = require(builder, "undoTransition?.ConfirmCommitted();", "multi-region builder", commit)
    committed = require(builder, "cadCommitted = true;", "multi-region builder", confirm)
    restore = require(builder, "rollback.Restore(project)", "multi-region builder", committed)
    dispose = require(builder, "undoTransition?.Dispose();", "multi-region builder", restore)

    if not rollback < stamp < cap < ownership < begin < marker:
        fail("before-snapshot/stamp, cap/ownership validation, and native marker ordering changed")
    if marker >= min(append, erase):
        fail("native revision marker must be staged before the first generated append/erase topology mutation")
    if not max(append, erase) < semantic < audit < after_capture < stage_after < commit < confirm < committed < restore < dispose:
        fail("semantic ownership/audit must be captured before CAD commit; history must publish only after commit; rollback/dispose ordering changed")

    if builder.count("if (!SourceReconcileUndoCoordinator.IsExternalTransitionActive(document))") < 2:
        fail("builder lost nested command-level transition suppression around before/after history staging")
    if "SourceReconcileUndoCoordinator.PendingTransition? undoTransition = null;" not in builder:
        fail("builder no longer owns a disposable pending semantic/native transition")
    if "SourceReconcileUndoCoordinator.CommitExternalTransition" in builder:
        fail("builder must not move its marker into a post-CAD helper transaction; CAD and semantic history belong to the same native transaction")

    for token in (
        "MaxBarsPerBatch = 12000",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership",
        "GeneratedRebarRegionOwnershipService.RequireMatchingOwnership",
        "GeneratedRebarNativeOwnershipService.MarkGenerated",
        "GeneratedRebarRegionOwnershipService.MarkGenerated",
        "MultiRegionRebarManifest.SerializeSources",
        "MultiRegionRebarManifest.SerializeGenerated",
    ):
        require(builder, token, "preserved LOCAL-005 safety contract")

    for forbidden in (
        "document.CommandEnded +=",
        "document.CommandWillStart +=",
        "new Dictionary<string, ProjectStateSnapshot>",
    ):
        if forbidden in builder:
            fail("multi-region builder introduced a second native Undo observer/history: " + forbidden)

    for token in (
        "if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);",
        "if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;",
        'if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";',
        'if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";',
        'if (string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)) return "MREDO";',
        "targetEntry.Snapshot.Restore(project);",
    ):
        require(undo, token, "production native semantic Undo coordinator")

    print("PASS: LOCAL-005 multi-region Slab/Foundation materialization stages one document-bound native revision before CAD topology mutation, captures generated ownership/manifests in the semantic after-snapshot before commit, publishes history only after commit, preserves pre-commit rollback/ownership/cap guards, and reuses the hardened production Undo/Redo observer.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
