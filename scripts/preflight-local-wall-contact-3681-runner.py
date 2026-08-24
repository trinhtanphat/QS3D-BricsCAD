#!/usr/bin/env python3
"""Fail closed unless #3681 remains a committed pull/run-only V25 qualification lane."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-v25-wall-contact-3681.ps1"
RUNNER_NAME = RUNNER.name
PROJECT = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "QS3D.BricsCAD.V25.LocalQualification.csproj"
HARNESS = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681QualificationCommands.cs"
GATE = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681SourceFixGateCommands.cs"
PRODUCTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "StructuralWallConcreteContactService.cs"
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"
SOURCE_FIX_SHA = "4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0"
CARRIER = "agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh"


def fail(message: str) -> None:
    print("ERROR: #3681 local runner preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require_tokens(text: str, label: str, tokens: tuple[str, ...]) -> None:
    for token in tokens:
        if token not in text:
            fail(label + " is missing: " + token)


def contains_forbidden(text: str, forbidden: str) -> bool:
    if forbidden in ("TODO", "FIXME"):
        # Treat work-marker words as tokens. A substring check would falsely reject valid
        # identifiers such as Convert.ToDouble because "todouble" starts with "todo".
        pattern = rf"(?<![A-Za-z0-9_]){re.escape(forbidden)}(?![A-Za-z0-9_])"
        return re.search(pattern, text, flags=re.IGNORECASE) is not None
    return forbidden.lower() in text.lower()


for path in (RUNNER, PROJECT, HARNESS, GATE, PRODUCTION, INDEX, DISPATCH):
    if not path.is_file():
        fail("missing committed pull/run surface: " + str(path.relative_to(ROOT)))

runner = RUNNER.read_text(encoding="utf-8")
harness = HARNESS.read_text(encoding="utf-8")
gate = GATE.read_text(encoding="utf-8")
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
        "QS3D3681SOURCEFIXGATE",
        "source-fix-gate",
        'Require-Case $gate "touching_one_end"',
        'Require-Case $gate "penetration_005m"',
        "QS3D3681GEOMETRY",
        "QS3D3681PERSIST",
        "QS3D3681REOPEN",
        "geometry-1",
        "geometry-2",
        "save/cold-reopen",
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
        "touching=0.1600 penetration=0.1600 gross=2.6688 deduction=0.3200 net=2.3488",
    ),
)

require_tokens(
    gate,
    "source-fix gate harness",
    (
        'CommandMethod("QS3D3681SOURCEFIXGATE")',
        'case.touching_one_end',
        'case.penetration_005m',
        'RunMeasureCase(document, -100d, 100d)',
        'RunMeasureCase(document, -100d, 150d)',
        'touching.PositiveVolumeCutCount != 0',
        'touching.ContactProbeCutCount < 1',
        'penetration.PositiveVolumeCutCount < 1',
        'ExpectedOneEndM2 = 0.1600d',
        'ExpectedOneEndNetM2 = 2.5088d',
        'StructuralWallConcreteContactService',
        'TryMeasureM2',
        'FailedNativeCutCount',
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
        "ProjectContextCoordinator",
        "GetOrCreate",
        "Save",
    ),
)

# Both local harnesses call the production measurement/lifecycle surfaces; neither may
# duplicate the host-specific BREP unwrap/modeler algorithm. Lock the final #3729 modeler
# floor in production, while the local gate proves the two licensed acceptance controls.
require_tokens(
    production,
    "production V25 contact service",
    (
        "StructuralWallConcreteContactService",
        "ExternalBoundedSurface",
        "TryMeasureM2",
        "var contactProbeDistanceCad = Math.Max(distanceCad, 1e-5d / lengthToMeter);",
        "TryOffset(contactProbe, contactProbeDistanceCad)",
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
        "#3729",
        "scripts/run-local-v25-wall-contact-3681.ps1",
        "touching-only",
        "0.05 m penetration",
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
        "#3729",
        "touching-only",
        "0.05 m penetration",
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
    if contains_forbidden(runner, forbidden) or contains_forbidden(harness, forbidden) or contains_forbidden(gate, forbidden):
        fail("local lane still delegates implementation work: " + forbidden)

# Process ownership must be explicit. Inferring ownership from a process start timestamp can
# terminate a user-launched BricsCAD session that happened to start while qualification ran.
if "StartTime.ToUniversalTime() -ge $startedUtc" in runner:
    fail("runner must not infer BricsCAD process ownership from process start time")

print("PASS #3681 committed one-command licensed V25 runner with #3729 touching/penetration fail-fast gate")
