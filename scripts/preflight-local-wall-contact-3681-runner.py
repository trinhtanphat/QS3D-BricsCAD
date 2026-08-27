#!/usr/bin/env python3
"""Fail closed unless #3681 retains its committed runner and completed V25 evidence."""

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
SOURCE_READY_FLOOR_SHA = "c64eb8c1b83761e155da670904a72e64669464b7"
TOUCHING_PROBE_FLOOR_SHA = "4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0"
ACCEPTED_RUNTIME_SHA = "a4f1a53683a9296532a0290fcb79bc49b9d4b892"
ACCEPTED_EVIDENCE_SHA = "7fec6f36a7c1181d7113f0e7220ea3dafca66e29"


def fail(message: str) -> None:
    print("ERROR: #3681 local runner preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require_tokens(text: str, label: str, tokens: tuple[str, ...]) -> None:
    for token in tokens:
        if token not in text:
            fail(label + " is missing: " + token)


def require_ordered_tokens(text: str, label: str, tokens: tuple[str, ...]) -> None:
    last_offset = -1
    for token in tokens:
        offset = text.find(token)
        if offset < 0:
            fail(label + " is missing: " + token)
        if offset <= last_offset:
            fail(label + " must bind the trusted drawing unit before semantic quantity creation/capture: " + token)
        last_offset = offset


def contains_forbidden(text: str, forbidden: str) -> bool:
    if forbidden in ("TODO", "FIXME"):
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
        SOURCE_READY_FLOOR_SHA,
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
        "using QS3D.Core.Units;",
        "private static void BindFixtureMillimeterUnit(ProjectState project)",
        "LengthUnit.Millimeter",
        "DrawingUnitResolutionPolicy.ValidateQuantityCompatibility",
        "DrawingUnitResolutionPolicy.BindQuantityUnit",
        "DrawingUnitResolutionSource.ProjectOverride",
        "DrawingUnitResolutionPolicy.SetProjectOverride",
    ),
)

capture_start = harness.find("private static void RunCaptureRefreshAndMissingTargetClear")
capture_end = harness.find("private static void RunReadOnlyMutationGuard", capture_start)
if capture_start < 0 or capture_end < 0:
    fail("runtime harness capture-refresh method boundary is missing")
require_ordered_tokens(
    harness[capture_start:capture_end],
    "runtime harness capture-refresh path",
    (
        "project.Elements.Clear();",
        "BindFixtureMillimeterUnit(project);",
        'var wall = NewWall("local-3681-capture-wall", wallSolid.Handle);',
        "project.Elements.Add(wall);",
        "new StructuralRegenerator().Regenerate(project, wall);",
        "CaptureSelection(document, ElementCategory.Column)",
    ),
)

persistence_start = harness.find("private static IDictionary<string, string> RunPersistenceSetup")
persistence_end = harness.find("private static IDictionary<string, string> RunColdReopenVerification", persistence_start)
if persistence_start < 0 or persistence_end < 0:
    fail("runtime harness persistence method boundary is missing")
require_ordered_tokens(
    harness[persistence_start:persistence_end],
    "runtime harness persistence setup",
    (
        "project.Elements.Clear();",
        "BindFixtureMillimeterUnit(project);",
        'var wall = NewWall("local-3681-wall", wallSolid.Handle);',
        "project.Elements.Add(wall);",
        "new StructuralRegenerator().Regenerate(project, wall);",
        "RefreshContacts(document, project);",
    ),
)

project_start = harness.find("private static ProjectState NewProject")
project_end = harness.find("private static ProjectElement NewWall", project_start)
if project_start < 0 or project_end < 0:
    fail("runtime harness direct-measure project method boundary is missing")
require_ordered_tokens(
    harness[project_start:project_end],
    "runtime harness direct-measure project setup",
    (
        'var project = new ProjectState("local-3681-" + Guid.NewGuid().ToString("N"), "LOCAL 3681");',
        "BindFixtureMillimeterUnit(project);",
        'var wall = NewWall("wall", wallHandle);',
        "project.Elements.Add(wall);",
    ),
)

# The local harness must contain the stable native-probe-floor correction as well as the
# later harness-minimum and finite-footprint corrections integrated in SOURCE_READY_FLOOR_SHA.
require_tokens(
    production,
    "production V25 contact service",
    (
        "StructuralWallConcreteContactService",
        "ExternalBoundedSurface",
        "TryMeasureM2",
        "var contactProbeDistanceCad = Math.Max(distanceCad, 1e-5d / lengthToMeter);",
        "TryOffset(contactProbe, contactProbeDistanceCad)",
        "TryCreateFootprintContact(",
        "TrySubtract(residual, footprintContact)",
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
        SOURCE_READY_FLOOR_SHA,
        TOUCHING_PROBE_FLOOR_SHA,
        "#3833",
        "#3836",
        "scripts/run-local-v25-wall-contact-3681.ps1",
        "touching-only",
        "0.05 m penetration",
        "LOCAL_RUN_ONLY",
    ),
)
require_tokens(
    dispatch,
    "#3681 completed dispatch",
    (
        "Status: `COMPLETED / DO_NOT_RERUN`",
        f"Minimum source-ready ancestor: `{SOURCE_READY_FLOOR_SHA}`",
        f"Exact runtime source: `{ACCEPTED_RUNTIME_SHA}`",
        f"Accepted evidence: PR #3849 / merge `{ACCEPTED_EVIDENCE_SHA}`",
        "#3833",
        "#3836",
        RUNNER_NAME,
        "regression reference",
        "touching-only",
        "0.05 m penetration",
        "LOCAL_PASS",
        "Do not execute it by default",
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

if "StartTime.ToUniversalTime() -ge $startedUtc" in runner:
    fail("runner must not infer BricsCAD process ownership from process start time")

print("PASS #3681 runner remains committed as a regression reference and dispatch binds accepted completed V25 evidence")
