#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileNativeBeamStretchDependentRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-native-beam-stretch-dependent.ps1"
STIRRUP = ROOT / "src" / "QS3D.Core" / "Rebar" / "LinearRebarLayoutPlanner.cs"
STIRRUP_BUILDER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "BeamStirrupSolidBuilder.cs"
REBAR_BUILDER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "BeamRebarSolidBuilder.cs"

errors = []
for path in (PROBE, RUNNER, STIRRUP, STIRRUP_BUILDER, REBAR_BUILDER):
    if not path.is_file():
        errors.append(f"missing LOCAL-004 P04 source-prep file: {path.relative_to(ROOT)}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

probe = PROBE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
planner = STIRRUP.read_text(encoding="utf-8")
stirrup_builder = STIRRUP_BUILDER.read_text(encoding="utf-8")
rebar_builder = REBAR_BUILDER.read_text(encoding="utf-8")

commands = (
    "QS3DSRBEAMP04PREPARE",
    "QS3DSRBEAMP04SELECT",
    "QS3DSRBEAMP04BASELINE",
    "QS3DSRBEAMP04STRETCHCHECK",
    "QS3DSRBEAMP04SYNCCHECK",
    "QS3DSRBEAMP04FINAL",
    "QS3DSRBEAMP04REOPEN",
)
for command in commands:
    if probe.count(f'CommandMethod("{command}"') != 1:
        errors.append(f"P04 probe must register {command} exactly once")

required_probe = (
    "LOCAL_004_P04_BEAM_DEPENDENT_STRETCH",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RUNTIME_V1",
    'owner.SetProperty("RebarNotation", "4D16")',
    'owner.SetProperty("RebarStirrupNotation", "D8@1000")',
    "ProjectStateSnapshot.Capture(context.Project)",
    "rollback.Restore(context.Project)",
    "RequireSource(context.Document, owner, 8d)",
    "RequireSemantic(owner, 5d)",
    "RequireSemantic(owner, 8d)",
    "RequireQuantities(owner, 8d)",
    "GeneratedGeometryService.HasMatchingOwnership(solid, project, owner)",
    "GeneratedRebarNativeOwnershipService.HasMatchingOwnership(solid, project, owner, handlesKey)",
    'RequireOwnedSet(document, project, owner, "GeneratedRebarHandles", "GeneratedRebarCount", 4)',
    'RequireOwnedSet(document, project, owner, "GeneratedBeamStirrupHandles", "GeneratedBeamStirrupCount", stirrupCount)',
    "RequireStirrupMetadata(owner, 9, .99275d)",
    "RequireLongitudinalExtent(output.Rebar, 8d)",
    "GENERATED_MUTATED_BY_NATIVE_STRETCH",
    "GENERATED_INVALIDATION_REJECTED",
    "GENERATED_REPLACEMENT_REJECTED",
    "stirrup_count_class=NINE_AT_D8_1000",
    "production_local004_p04_qualified=true",
    "output_families=HOST_LONGITUDINAL_STIRRUP",
)
for token in required_probe:
    if token not in probe:
        errors.append(f"P04 probe missing contract token: {token}")

for forbidden in (
    "OpenMode.ForWrite", "StartTransaction()", "AppendEntity(", ".Erase(",
    "SendStringToExecute", ".Editor.Command(", "ProjectContextCoordinator.GetOrCreate",
    "BeamRebarSolidBuilder.BuildSelected", "BeamStirrupSolidBuilder.BuildSelected",
):
    if forbidden in probe:
        errors.append(f"automation-only P04 probe must not perform production edit/generation directly: {forbidden}")

required_runner = (
    "LOCAL_004_P04_BEAM_DEPENDENT_STRETCH",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_PHASE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_NONCE",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_STRETCH_DEPENDENT_DWG",
    "status --porcelain=v1 --untracked-files=all",
    "ProductVersion",
    "ArtifactDir must stay outside repository",
    "Close BricsCAD before isolated P04 run",
    '"QS3DDRAWBEAM","0,0","5000,0"',
    '"QS3DSRBEAMP04PREPARE","QS3DBEAMREBAR3D"',
    '"QS3DSRBEAMP04SELECT","QS3DBEAMSTIRRUP3D","QS3DSRBEAMP04BASELINE"',
    '"QS3DSRBEAMP04SELECT","_.STRETCH","_C","4900,-100","5100,100","","0,0","3000,0","QS3DSRBEAMP04STRETCHCHECK"',
    '"QS3DSRBEAMP04SELECT","QS3DSYNCSOURCE","QS3DSRBEAMP04SYNCCHECK"',
    '"QS3DSRBEAMP04SELECT","QS3DBUILD3D"',
    '"QS3DSRBEAMP04SELECT","QS3DBEAMREBAR3D"',
    '"QS3DSRBEAMP04SELECT","QS3DBEAMSTIRRUP3D","QS3DSRBEAMP04FINAL"',
    "QS3DSRBEAMP04REOPEN",
    "process_cleanup_verified", "script_cleanup_verified", "private_state_cleanup_verified", "drawing_restore_verified",
)
for token in required_runner:
    if token not in runner:
        errors.append(f"P04 runner missing exact-SHA/native/cleanup token: {token}")

if runner.count('"_.STRETCH"') != 1:
    errors.append("P04 runner must issue exactly one real top-level native STRETCH")
if runner.count('"QS3DSYNCSOURCE"') != 1:
    errors.append("P04 runner must reconcile exactly once after STRETCH")
if runner.count('"QS3DBUILD3D"') != 1:
    errors.append("P04 runner must rebuild Beam host exactly once after reconcile")
if runner.count('"QS3DBEAMREBAR3D"') != 2 or runner.count('"QS3DBEAMSTIRRUP3D"') != 2:
    errors.append("P04 runner must build dependent bars/stirrups once before and once after reconcile")
if "Get-Clipboard" in runner or "Set-Clipboard" in runner:
    errors.append("P04 runner must not inspect or mutate clipboard")

# Pin the production arithmetic behind the 5 m -> 8 m redistribution assertion.
for token in (
    "var edgeClearanceM = RebarMath.Add(coverM, radiusM",
    "var intervals = RebarMath.CeilingNearInteger(intervalRatio",
    "count = checked((int)intervals + 1)",
    "var actualSpacingM = RebarMath.Divide(usableSpanM, count - 1d",
):
    if token not in planner:
        errors.append(f"linear stirrup planner missing arithmetic token: {token}")
for token in (
    'element.Properties["GeneratedBeamStirrupCount"]',
    'element.Properties["GeneratedBeamStirrupActualSpacingM"]',
):
    # builder uses update.Element; accept the stable property names instead of exact receiver text.
    key = token.split('[', 1)[1].rstrip(']')
    if key.strip('"') not in stirrup_builder:
        errors.append(f"Beam stirrup builder missing persisted redistribution property: {key}")
for token in (
    "var barLengthM = CadGeometryGuard.Finite(lengthM - twoEndCovers",
    "var longitudinalCenterX = CadGeometryGuard.Add(startX",
):
    if token not in rebar_builder:
        errors.append(f"Beam longitudinal builder missing length-sensitive placement token: {token}")

for forbidden_marker_key in ('"handle=', '"element_id=', '"project_id=', '"drawing_path=', '"exception_message=', '"stack_trace='):
    if forbidden_marker_key in probe.lower():
        errors.append("P04 sanitized marker must not publish IDs, handles, paths, or raw exception details")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-004 P04 source-prep pins real Beam STRETCH 5m->8m, pre-sync isolation, production reconcile/rebuild, four longitudinal bars, 6->9 D8@1000 stirrup redistribution, exact-SHA cold reopen and cleanup contracts.")
