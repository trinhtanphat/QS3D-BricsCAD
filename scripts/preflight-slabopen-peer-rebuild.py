#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
REPLAY = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabOpeningPeerReplayService.cs"


def require(path, tokens):
    if not path.is_file():
        raise RuntimeError("missing source: " + str(path.relative_to(ROOT)))
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise RuntimeError(str(path.relative_to(ROOT)) + " missing contract token: " + token)
    return text


def main():
    try:
        builder = require(BUILDER, (
            "SlabOpeningPeerReplayService.CaptureAppliedOpeningIds(project, element, previousHandle)",
            "AppliedSlabOpeningIds = appliedSlabOpeningIds",
            "GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, update.Category)",
            "pending.Where(x => x.Category == ElementCategory.Slab && x.AppliedSlabOpeningIds.Count > 0)",
            "SlabOpeningPeerReplayService.ReplayAppliedOpenings(",
            "update.PreviousHandle",
            "update.AppliedSlabOpeningIds",
            "project.Touch();",
            "undoTransition.StageAfter(project, afterSnapshot);",
            "transaction.Commit()",
        ))
        replay = require(REPLAY, (
            "CaptureAppliedOpeningIds(",
            "SlabOpeningContract.AppliedSolidHandleKey",
            "ReplayAppliedOpenings(",
            "GeneratedGeometryService.RequireMatchingOwnership(",
            "SlabOpeningContract.RequireHostSlabId(opening)",
            "CadElementVerticalPlacement.Resolve(",
            "SlabOpeningCutPlanner.Plan(",
            '"slabOpen-v2"',
            "hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter)",
            "opening.Properties[SlabOpeningContract.AppliedSolidHandleKey] = currentSolidHandle",
            "opening.Properties[SlabOpeningContract.AppliedFingerprintKey] = fingerprint",
            'host.Properties["SlabOpeningCutCount"] = CountAppliedOpenings(project, host.Id, currentSolidHandle)',
            "if (replayed != openingIds.Count)",
        ))
    except RuntimeError as exc:
        print("Slab peer-opening rebuild preflight FAILED")
        print(" -", exc)
        return 1

    commit_pos = builder.find("GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, update.Category)")
    replay_pos = builder.find("SlabOpeningPeerReplayService.ReplayAppliedOpenings(", commit_pos)
    touch_pos = builder.find("project.Touch();", replay_pos)
    stage_after_pos = builder.find("undoTransition.StageAfter(project, afterSnapshot);", touch_pos)
    tx_pos = builder.find("transaction.Commit()", stage_after_pos)
    if min(commit_pos, replay_pos, touch_pos, stage_after_pos, tx_pos) < 0 or not (commit_pos < replay_pos < touch_pos < stage_after_pos < tx_pos):
        print("Slab peer-opening rebuild preflight FAILED")
        print(" - peer replay must happen after new generated-handle commit, before the single project Touch, and be captured in staged semantic Undo before CAD transaction commit")
        return 1

    capture_pos = builder.find("SlabOpeningPeerReplayService.CaptureAppliedOpeningIds(project, element, previousHandle)")
    append_pos = builder.find("modelSpace.AppendEntity(solid)", capture_pos)
    if capture_pos < 0 or append_pos < 0 or capture_pos > append_pos:
        print("Slab peer-opening rebuild preflight FAILED")
        print(" - previously applied peer-opening identities must be captured from the retiring handle before the new host is appended")
        return 1

    if "project.Touch()" in replay:
        print("Slab peer-opening rebuild preflight FAILED")
        print(" - internal peer replay must participate in StructuralSolidBuilder's one atomic project revision, not touch per opening")
        return 1

    print("PASS: rebuilding a Slab captures exactly the slabOpen peers applied to the retiring Solid3d, replays them onto the new owned Solid3d inside the same CAD transaction, rewrites applied handle/fingerprint/count, stages that semantic state for native Undo, and fails closed so outer CAD/project rollback remains atomic.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
