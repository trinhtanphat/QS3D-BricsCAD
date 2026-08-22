#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

handoff = ROOT / "docs/AGENT-HANDOFF-CURRENT-2026-08-10-1710.md"
grid = ROOT / "docs/GRID-WORKFLOW.md"
level = ROOT / "docs/LEVEL-REFERENCES.md"
source_edit = ROOT / "docs/SOURCE-EDIT-WORKFLOW.md"
interchange = ROOT / "docs/INTERCHANGE-JSON.md"
documentation = ROOT / "docs/DOCUMENTATION-LAYER.md"
local_addendum = ROOT / "docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md"

for path in (handoff, grid, level, source_edit, interchange, documentation, local_addendum):
    if not path.is_file():
        errors.append("missing current-status contract: " + str(path.relative_to(ROOT)))

if handoff.is_file():
    text = handoff.read_text(encoding="utf-8")
    for token in (
        "docs/REMOTE-AGENT-SCOPE.md",
        "QS3DGRID",
        "BottomLevelId",
        "TopLevelId",
        "ElementVerticalPlacementService",
        "QS3DSYNCSOURCE",
        "QS3DINTERCHANGEJSON",
        "SemanticTagRenderer",
        "docs/DOCUMENTATION-LAYER.md",
        "geometry.rebar.shape",
        "QS3DRUNTIMECHECK",
        "QS3DRUNTIMEPROBE",
        "QS3DSUPPORTBUNDLE",
        "LOCAL_ONLY",
    ):
        if token not in text:
            errors.append("current handoff missing source/status token: " + token)
    stale = (
        "Future top/bottom level-reference semantics should extend",
        "Future top/bottom reference semantics should extend",
    )
    for token in stale:
        if token in text:
            errors.append("current handoff still describes implemented Level semantics as future: " + token)

if grid.is_file():
    text = grid.read_text(encoding="utf-8")
    for token in (
        "BottomLevelId",
        "BottomLevelOffsetM",
        "TopLevelId",
        "TopLevelOffsetM",
        "ElementVerticalPlacementService",
        "LevelReferenceHealthService",
        "QS3DSYNCSOURCE",
        "LOCAL_ONLY",
    ):
        if token not in text:
            errors.append("Grid workflow missing current Level/source-reconcile token: " + token)
    if "Future top/bottom reference semantics" in text:
        errors.append("Grid workflow regressed to stale pre-Level wording")

if local_addendum.is_file():
    text = local_addendum.read_text(encoding="utf-8")
    for token in (
        "Level references → native placement/UI integration",
        "ElementVerticalPlacementService",
        "Level integration",
    ):
        if token not in text:
            errors.append("local addendum missing Level native handoff token: " + token)

if documentation.is_file():
    text = documentation.read_text(encoding="utf-8")
    for token in (
        "SemanticTagRenderer",
        "DWG tables — source-implemented native Table slice",
        "Still open for native table qualification/expansion:",
    ):
        if token not in text:
            errors.append("documentation-layer boundary missing token: " + token)

print("QS3D current handoff/source-status sync preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical current handoff and Grid workflow reflect current Grid, Level, source-reconcile, interchange, documentation, diagnostics and LOCAL_ONLY boundaries without stale pre-Level wording.")
