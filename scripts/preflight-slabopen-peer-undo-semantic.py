#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
UNDO = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"


def fail(message: str) -> None:
    raise RuntimeError(message)


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        fail(f"{label} missing contract token: {token}")
    return pos


def main() -> int:
    try:
        if not BUILDER.is_file():
            fail("missing StructuralSolidBuilder.cs")
        if not UNDO.is_file():
            fail("missing SourceReconcileUndoCoordinator.cs")

        builder = BUILDER.read_text(encoding="utf-8")
        undo = UNDO.read_text(encoding="utf-8")

        rollback_pos = require(
            builder,
            "var rollback = ProjectStateSnapshot.Capture(project);",
            "structural rebuild",
        )
        stamp_pos = require(
            builder,
            "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
            "structural rebuild",
        )
        lazy_pos = require(
            builder,
            "if (undoTransition == null)",
            "structural rebuild",
        )
        begin_pos = require(
            builder,
            "undoTransition = SourceReconcileUndoCoordinator.BeginTransition(",
            "structural rebuild",
        )
        marker_pos = require(
            builder,
            "undoTransition.StageNativeMarker();",
            "structural rebuild",
        )
        prepare_pos = require(
            builder,
            "GeneratedGeometryService.PrepareReplacement(document, transaction, project, element)",
            "structural rebuild",
        )
        replay_pos = require(
            builder,
            "SlabOpeningPeerReplayService.ReplayAppliedOpenings(",
            "structural rebuild",
        )
        touch_pos = require(
            builder,
            "project.Touch();",
            "structural rebuild",
        )
        after_capture_pos = require(
            builder,
            "var afterSnapshot = ProjectStateSnapshot.Capture(project);",
            "structural rebuild",
        )
        stage_after_pos = require(
            builder,
            "undoTransition.StageAfter(project, afterSnapshot);",
            "structural rebuild",
        )
        commit_pos = require(
            builder,
            "transaction.Commit();",
            "structural rebuild",
        )
        confirm_pos = require(
            builder,
            "undoTransition?.ConfirmCommitted();",
            "structural rebuild",
        )
        dispose_pos = require(
            builder,
            "undoTransition?.Dispose();",
            "structural rebuild",
        )

        if not rollback_pos < stamp_pos < lazy_pos <= begin_pos < marker_pos < prepare_pos:
            fail(
                "native semantic Undo must capture the before snapshot/stamp and lazily stage its native marker before replacement topology mutates"
            )
        if not prepare_pos < replay_pos < touch_pos < after_capture_pos < stage_after_pos < commit_pos < confirm_pos < dispose_pos:
            fail(
                "peer replay metadata must be inside the after snapshot, with semantic history staged before native commit and published only after commit"
            )

        # Pin the exact metadata that LOCAL-018 proved stale after native Undo.
        replay = (ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabOpeningPeerReplayService.cs").read_text(encoding="utf-8")
        for token in (
            'opening.Properties[SlabOpeningContract.AppliedSolidHandleKey] = currentSolidHandle',
            'opening.Properties[SlabOpeningContract.AppliedFingerprintKey] = fingerprint',
            'host.Properties["SlabOpeningCutCount"] = CountAppliedOpenings(project, host.Id, currentSolidHandle)',
        ):
            require(replay, token, "slabOpen peer replay")
        require(
            builder,
            "GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, update.Category)",
            "structural rebuild",
        )

        # Reuse the hardened document-scoped native Undo observer rather than
        # introducing an Ended-only hook. These tokens pin both exact matched
        # completion and the next-command fallback for V25 missed terminals.
        for token in (
            "if (IsActiveDocument(document)) SynchronizeToNativeRevision(document);",
            "if (!TryConsumeMatchingCommand(document, args?.GlobalCommandName)) return;",
            'if (string.Equals(normalized, "UNDO", StringComparison.OrdinalIgnoreCase)) return "UNDO";',
            'if (string.Equals(normalized, "REDO", StringComparison.OrdinalIgnoreCase)) return "REDO";',
            'if (string.Equals(normalized, "MREDO", StringComparison.OrdinalIgnoreCase)) return "MREDO";',
            "targetEntry.Snapshot.Restore(project);",
        ):
            require(undo, token, "native semantic Undo coordinator")

        # No-op/unsupported selections must not manufacture a native marker.
        foreach_pos = require(builder, "foreach (var id in ids)", "structural rebuild")
        if begin_pos < foreach_pos:
            fail("Undo transition must remain lazy and start only after a real structural target resolves")

    except (OSError, RuntimeError) as exc:
        print("Slab peer native Undo semantic preflight FAILED")
        print(" -", exc)
        return 1

    print(
        "PASS: structural rebuild stages the shared native revision marker before generated-solid replacement, "
        "captures post-replay GeneratedSolidHandle/applied-handle/fingerprint/cut-count semantics before commit, "
        "publishes history only after native commit, and reuses the hardened matched/fallback Undo/Redo observer."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
