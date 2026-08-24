#!/usr/bin/env python3
"""Fail closed unless #3681 remains a committed pull/run-only V25 qualification lane."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-v25-wall-contact-3681.ps1"
RUNNER_NAME = RUNNER.name
PROJECT = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "QS3D.BricsCAD.V25.LocalQualification.csproj"
HARNESS = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681QualificationCommands.cs"
PRODUCTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "StructuralWallConcreteContactService.cs"
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"
SOURCE_FIX_SHA = "cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb"
CARRIER = "agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh"


def fail(message: str) -> None:
    print("ERROR: #3681 local runner preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require_tokens(text: str, label: str, tokens: tuple[str, ...]) -> None:
    for token in tokens:
        if token not in text:
            fail(label + " is missing: " + token)


for path in (RUNNER, PROJECT, HARNESS, PRODUCTION, INDEX, DISPATCH):
    if not path.is_file():
        fail("missing committed pull/run surface: " + str(path.relative_to(ROOT)))

runner = RUNNER.read_text(encoding="utf-8")
harness = HARNESS.read_text(encoding="utf-8")
project = PROJECT.read_text(encoding="utf-8")
production = PRODUCTION.read_text(encoding="utf-8")
index = INDEX.read_text(encoding="utf-8")
dispatch = DISPATCH.read_text(encoding="utf-8")

require_tokens(
    runner,
    "runner contract",
    (
        SOURCE_FIX_SHA,
        "git merge-base --is-ancestor",
        "working tree must be clean",
        "run-local-v25-qualification.ps1",
        "QS3D.BricsCAD.V25.LocalQualification.csproj",
        "QS3D3681GEOMETRY",
        "QS3D3681PERSIST",
        "QS3D3681REOPEN",
        "geometry-1",
        "geometry-2",
        "save/cold-reopen",
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
        "gross=2.6688 deduction=0.3200 net=2.3488",
    ),
)

require_tokens(
    harness,
    "runtime harness",
    (
        "case.baseline",
        "case.full_end",
        "case.partial_end",
        "case.multi_neighbor_union",
        "case.top_bottom_exclusion",
        "case.two_end_blt",
        "case.semantic_capture_refresh",
        "case.stale_missing_brep_clear",
        "case.measurement_read_only",
        "case.undo_redo",
        "case.save",
        "case.cold_reopen",
        "StructuralWallConcreteContactService",
        "TryMeasureM2",
        "SemanticCaptureService",
        "RefreshStructuralWallConcreteContacts",
        "SetImpliedSelection",
        "ProjectContextCoordinator",
        "GetOrCreate",
        "Save",
    ),
)

# The local harness must call the production measurement/lifecycle surfaces; it must not
# duplicate the host-specific BREP unwrap implementation. Lock that implementation token
# in production instead.
require_tokens(
    production,
    "production V25 contact service",
    (
        "StructuralWallConcreteContactService",
        "ExternalBoundedSurface",
        "TryMeasureM2",
    ),
)

require_tokens(
    project,
    "qualification harness project",
    (
        "<TargetFramework>net48</TargetFramework>",
        "<PlatformTarget>x64</PlatformTarget>",
        "TD_MgdBrep.dll",
        "QS3D.Core.csproj",
    ),
)

require_tokens(
    index,
    "#3681 source-ready index",
    (
        CARRIER,
        SOURCE_FIX_SHA,
        "scripts/run-local-v25-wall-contact-3681.ps1",
        "LOCAL_RUN_ONLY",
    ),
)
require_tokens(
    dispatch,
    "#3681 dispatch",
    (
        "Status: `LOCAL_READY / PULL_RUN_ONLY`",
        f"Required source-fix ancestor: `{SOURCE_FIX_SHA}`",
        f"Runnable carrier: `{CARRIER}`",
        RUNNER_NAME,
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
    ),
)

for forbidden in (
    "TODO",
    "FIXME",
    "manually create",
    "paste this",
    "edit production source",
):
    if forbidden.lower() in runner.lower() or forbidden.lower() in harness.lower():
        fail("local lane still delegates implementation work: " + forbidden)

print("PASS #3681 committed one-command licensed V25 runner/harness")
