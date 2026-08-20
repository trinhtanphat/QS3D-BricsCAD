#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileNativeBeamDependentRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-native-beam-dependent.ps1"
CLAIM = ROOT / "docs" / "agent-work-claims" / "2026-08-20-codex-issue3289-native-beam-dependent-move.md"

errors = []
for path in (PROBE, RUNNER, CLAIM):
    if not path.is_file():
        errors.append(f"missing required LOCAL-004 P03 file: {path.relative_to(ROOT)}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

probe = PROBE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
claim = CLAIM.read_text(encoding="utf-8")

commands = (
    "QS3DSRBEAMP03PREPARE",
    "QS3DSRBEAMP03BASELINE",
    "QS3DSRBEAMP03SELECT",
    "QS3DSRBEAMP03MOVECHECK",
    "QS3DSRBEAMP03SYNCCHECK",
    "QS3DSRBEAMP03FINAL",
    "QS3DSRBEAMP03REOPEN",
)
for command in commands:
    if probe.count(f'CommandMethod("{command}"') != 1:
        errors.append(f"probe must register {command} exactly once")

required_probe_tokens = (
    "SourceReconcileNativeBeamDependentRuntimeProbeCommands",
    "ElementCategory.Beam",
    'owner.SetProperty("RebarNotation", "4D16")',
    'owner.SetProperty("RebarStirrupNotation", "D8@1000")',
    "ProjectStateSnapshot.Capture(context.Project)",
    "rollback.Restore(context.Project)",
    "context.Project.Touch()",
    'RequireRebarSet(document, project, owner, "GeneratedRebarHandles", "GeneratedRebarCount", 4)',
    'RequireRebarSet(document, project, owner, "GeneratedBeamStirrupHandles", "GeneratedBeamStirrupCount", 6)',
    "GeneratedGeometryService.HasMatchingOwnership(solid, project, owner)",
    "GeneratedRebarNativeOwnershipService.HasMatchingOwnership(solid, project, owner, handlesKey)",
    "RequireContained(host, rebar)",
    "RequireContained(host, stirrups)",
    "GENERATED_MUTATED_BY_NATIVE_MOVE",
    "RequireNoGenerated(context.Document, owner, state.RequiredBaseline)",
    "RequireTranslatedReplacement(state.RequiredBaseline, rebuilt)",
    "GeneratedRebarHealthService",
    "GeneratedBeamStirrupHealthService",
    "GeneratedRebarOwnershipHealthService",
    "GeneratedRebarModeHealthService",
    'qualification_boundary=" + Boundary',
    "production_local004_p03_qualified=true",
    "output_families=HOST_LONGITUDINAL_STIRRUP",
    'failure_code=" + OneLine(code)',
)
for token in required_probe_tokens:
    if token not in probe:
        errors.append(f"probe is missing required Beam dependent-output contract token: {token}")

for forbidden in (
    "OpenMode.ForWrite",
    "StartTransaction()",
    "AppendEntity(",
    ".Erase(",
    "SendStringToExecute",
    ".Editor.Command(",
    "ProjectContextCoordinator.GetOrCreate",
    "BeamRebarSolidBuilder.BuildSelected",
    "BeamStirrupSolidBuilder.BuildSelected",
    "StructuralSolidBuilder.BuildSelected",
):
    if forbidden in probe:
        errors.append(f"automation-only probe must not perform production/native generation or MOVE directly: {forbidden}")

if probe.count('owner.SetProperty("RebarNotation", "4D16")') != 1 or probe.count(
    'owner.SetProperty("RebarStirrupNotation", "D8@1000")'
) != 1:
    errors.append("probe may provision each bounded fixture notation exactly once")
if "CommandFlags.Modal | CommandFlags.UsePickSet" not in probe:
    errors.append("source reselection command must preserve PICKFIRST for production commands")

script_start = runner.find("$scriptOne = @(")
script_end = runner.find("    )", script_start)
if script_start < 0 or script_end < 0:
    errors.append("runner session-one script block is missing")
else:
    script = runner[script_start:script_end]
    ordered = (
        '"QS3DDRAWBEAM", "0,0", "5000,0"',
        '"QS3DSRBEAMP03PREPARE", "QS3DBEAMREBAR3D"',
        '"QS3DSRBEAMP03SELECT", "QS3DBEAMSTIRRUP3D", "QS3DSRBEAMP03BASELINE"',
        '"QS3DSRBEAMP03SELECT", "_.MOVE", "", "_Displacement", "0,1000", "QS3DSRBEAMP03MOVECHECK"',
        '"QS3DSRBEAMP03SELECT", "QS3DSYNCSOURCE", "QS3DSRBEAMP03SYNCCHECK"',
        '"QS3DSRBEAMP03SELECT", "QS3DBUILD3D"',
        '"QS3DSRBEAMP03SELECT", "QS3DBEAMREBAR3D"',
        '"QS3DSRBEAMP03SELECT", "QS3DBEAMSTIRRUP3D", "QS3DSRBEAMP03FINAL"',
        '"QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"',
    )
    cursor = -1
    for token in ordered:
        current = script.find(token, cursor + 1)
        if current < 0:
            errors.append(f"runner session-one order is missing: {token}")
            break
        cursor = current
    if script.count('"_.MOVE"') != 1:
        errors.append("runner must issue exactly one real top-level native MOVE")
    if script.count('"QS3DSYNCSOURCE"') != 1:
        errors.append("runner must reconcile exactly once after native MOVE")
    if script.count('"QS3DBUILD3D"') != 1:
        errors.append("runner must rebuild the host exactly once after reconcile")
    if script.count('"QS3DBEAMREBAR3D"') != 2:
        errors.append("runner must build longitudinal bars once before and once after reconcile")
    if script.count('"QS3DBEAMSTIRRUP3D"') != 2:
        errors.append("runner must build stirrups once before and once after reconcile")

required_runner_tokens = (
    "QS3DSRBEAMP03REOPEN",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_PHASE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_NONCE",
    "QS3D_SOURCE_RECONCILE_NATIVE_BEAM_DEPENDENT_DWG",
    "status --porcelain=v1 --untracked-files=all",
    "qualification requires a clean exact-SHA worktree",
    "ProductVersion",
    "EndsWith($expectedAssemblyRevision",
    "FixtureDwg must be the repository-generated QS3D sample",
    "ArtifactDir must stay outside the repository",
    "Close existing BricsCAD processes",
    "Start-Process -FilePath $bricscadExe",
    "-WindowStyle Hidden",
    "Stop-Qs3dLateHandoffProcesses",
    "process_cleanup_verified",
    "script_cleanup_verified",
    "private_state_cleanup_verified",
    "drawing_restore_verified",
    "drawing_persisted_changed",
    "sidecar_persisted",
    "LOCAL_004_P03_BEAM_DEPENDENT_MOVE",
)
for token in required_runner_tokens:
    if token not in runner:
        errors.append(f"runner is missing exact-SHA/privacy/cleanup contract token: {token}")

if "Get-Clipboard" in runner or "Set-Clipboard" in runner:
    errors.append("runner must not inspect or mutate the user's clipboard")
for forbidden_marker_key in (
    '"handle=', '"element_id=', '"project_id=', '"drawing_path=',
    '"exception_message=', '"stack_trace=', '"exception_type=',
):
    if forbidden_marker_key in probe.lower():
        errors.append("sanitized probe marker must not publish IDs, handles, paths, or raw exception details")

claim_tokens = (
    "issue #3289",
    "Status: `ACTIVE`",
    "Lane-Key: `issue-3289`",
    "4D16",
    "D8@1000",
    "QS3DBEAMREBAR3D",
    "QS3DBEAMSTIRRUP3D",
    "automation-only",
    "Parent issue #80 remains open",
)
for token in claim_tokens:
    if token not in claim:
        errors.append(f"claim is missing bounded ownership token: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-004 P03 drives one production Direct Draw Beam through real native MOVE, production host/longitudinal/stirrup invalidation and rebuild, save/cold reopen, and exact privacy/cleanup guards without production edits.")
