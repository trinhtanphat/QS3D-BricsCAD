#!/usr/bin/env python3
"""Fail closed unless #3681 is a pull/run-only licensed V25 qualification lane."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-v25-wall-contact-3681.ps1"
PROJECT = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "QS3D.BricsCAD.V25.LocalQualification.csproj"
HARNESS = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681QualificationCommands.cs"
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"
SOURCE_FIX_SHA = "cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb"


def fail(message: str) -> None:
    print("ERROR: #3681 local runner preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


for path in (RUNNER, PROJECT, HARNESS, INDEX, DISPATCH):
    if not path.is_file():
        fail("missing committed pull/run surface: " + str(path.relative_to(ROOT)))

runner = RUNNER.read_text(encoding="utf-8")
harness = HARNESS.read_text(encoding="utf-8")
project = PROJECT.read_text(encoding="utf-8")
index = INDEX.read_text(encoding="utf-8")
dispatch = DISPATCH.read_text(encoding="utf-8")

for token in (
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
):
    if token not in runner:
        fail("runner contract missing token: " + token)

for case in (
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
):
    if case not in harness:
        fail("runtime harness case is missing: " + case)

for token in (
    "StructuralWallConcreteContactService",
    "TryMeasureM2",
    "ExternalBoundedSurface",
):
    if token == "ExternalBoundedSurface":
        # The production preflight owns the concrete wrapper token. This local harness must
        # exercise the production service rather than duplicate its implementation.
        production = (ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "StructuralWallConcreteContactService.cs").read_text(encoding="utf-8")
        if token not in production:
            fail("production V25 BREP unwrapping contract disappeared")
    elif token not in harness:
        fail("harness does not invoke production contact path: " + token)

for token in (
    "SemanticCaptureService",
    "RefreshStructuralWallConcreteContacts",
    "SetImpliedSelection",
    "ProjectContextCoordinator",
    "Save",
    "GetOrCreate",
):
    if token not in harness:
        fail("production lifecycle runtime probe is missing: " + token)

for token in (
    "<TargetFramework>net48</TargetFramework>",
    "<PlatformTarget>x64</PlatformTarget>",
    "TD_MgdBrep.dll",
    "QS3D.Core.csproj",
):
    if token not in project:
        fail("qualification harness project drifted: " + token)

for doc, label in ((index, "index"), (dispatch, "dispatch")):
    for token in (
        "run-local-v25-wall-contact-3681.ps1",
        "pull",
        "run",
    ):
        if token.lower() not in doc.lower():
            fail(f"#3681 {label} is not pull/run-only: {token}")

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
