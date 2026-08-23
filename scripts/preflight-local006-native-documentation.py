#!/usr/bin/env python3
"""Static source contract for LOCAL-006 native documentation source completion.

This gate deliberately covers source-safe contracts only. Licensed BricsCAD runtime
qualification remains in docs/LOCAL-AGENT-INBOX.md and must not be inferred from
this preflight.
"""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def require_file(relative):
    path = ROOT / relative
    if not path.is_file():
        raise AssertionError("missing required source file: " + relative)
    return path.read_text(encoding="utf-8")


def require_all(text, relative, needles):
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(relative + " is missing contract token(s): " + ", ".join(missing))


def check_mleader_lifecycle():
    builder_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticMLeaderBuilder.cs"
    builder = require_file(builder_rel)
    require_all(
        builder,
        builder_rel,
        (
            "new MLeader",
            "GeneratedSemanticTagHealthService.HandlesKey",
            "GeneratedSemanticTagArtifactKind",
            "GeneratedSemanticTagLeaderTargetHandle",
            "GeneratedSemanticTagLeaderTextX",
            "GeneratedGeometryService.MarkGenerated",
            "ProjectStateSnapshot.Capture",
            "documentation.semantic-tag.mleader",
            "BuildBatch",
        ),
    )

    removal_rel = "src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs"
    removal = require_file(removal_rel)
    require_all(removal, removal_rel, ("MText", "MLeader", "RequireSupportedSemanticTag"))

    health_rel = "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
    health = require_file(health_rel)
    require_all(
        health,
        health_rel,
        (
            "MText",
            "MLeader",
            "InspectMLeader",
            "SEMANTIC_TAG_MLEADER",
            "GeneratedSemanticTagLeaderTargetHandle",
        ),
    )

    commands_rel = "src/QS3D.BricsCAD.V25/SemanticTagCommands.cs"
    commands = require_file(commands_rel)
    require_all(
        commands,
        commands_rel,
        (
            'CommandMethod("QS3DTAGLEADER",',
            'CommandMethod("QS3DTAGLEADERBATCH",',
            "SemanticMLeaderBuilder.Build",
            "SemanticMLeaderBuilder.BuildBatch",
        ),
    )


if __name__ == "__main__":
    try:
        check_mleader_lifecycle()
    except AssertionError as exc:
        print("ERROR:", exc)
        sys.exit(1)
    print("PASS: LOCAL-006 native MLeader source contract is present.")
