#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileNativeLineEditRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-native-line-edit.ps1"
CLAIM = ROOT / "docs" / "agent-work-claims" / "2026-08-20-codex-issue3281-native-line-edit-qualification.md"

errors = []
for path in (PROBE, RUNNER, CLAIM):
    if not path.is_file():
        errors.append(f"missing required LOCAL-004 P01 file: {path.relative_to(ROOT)}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

probe = PROBE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
claim = CLAIM.read_text(encoding="utf-8")

commands = (
    "QS3DSRNATIVEPREPARE",
    "QS3DSRNATIVEMOVE",
    "QS3DSRNATIVESELECT",
    "QS3DSRNATIVECHECKMOVE",
    "QS3DSRNATIVECHECKMOVEBUILD",
    "QS3DSRNATIVEROTATE",
    "QS3DSRNATIVECHECKROTATE",
    "QS3DSRNATIVESTRETCH",
    "QS3DSRNATIVECHECKSTRETCH",
    "QS3DSRNATIVEFINAL",
    "QS3DSRNATIVEREOPEN",
)
for command in commands:
    token = f'CommandMethod("{command}"'
    if probe.count(token) != 1:
        errors.append(f"probe must register {command} exactly once")

required_probe_tokens = (
    'context.Document.Editor.Command(\n                    "_.MOVE"',
    'context.Document.Editor.Command(\n                    "_.ROTATE"',
    '"_.STRETCH"',
    '"_Displacement"',
    'new Point3d(0d, Drawing(context.Document, 2d), 0d)',
    'new Point3d(0d, Drawing(context.Document, 3d), 0d)',
    '"90"',
    '"_C"',
    'NATIVE_STRETCH_COMMAND_REJECTED',
    'NATIVE_STRETCH_GEOMETRY_REJECTED',
    'RequireSemanticLength(owner, 5d, "native STRETCH before reconcile")',
    'RequireSemanticLength(owner, 8d, "STRETCH reconcile")',
    'RequireNoGenerated(context.Document, owner, state.InitialGeneratedHandle, "MOVE reconcile")',
    'RequireNoGenerated(context.Document, owner, state.MoveGeneratedHandle, "ROTATE reconcile")',
    'RequireGenerated(context.Document, owner, "cold reopen")',
    'qualification_boundary=LOCAL_004_P01_LINE_ONLY',
    'production_local004_p01_qualified=true',
    'edit_commands=MOVE_ROTATE_STRETCH',
    'final_length_class=EIGHT_METERS',
    'failure_code=" + OneLine(code)',
)
for token in required_probe_tokens:
    if token not in probe:
        errors.append(f"probe is missing required native LINE contract token: {token}")

for forbidden in (
    "OpenMode.ForWrite",
    "StartTransaction()",
    "AppendEntity(",
    ".Erase(",
    ".StartPoint =",
    ".EndPoint =",
    ".SetPointAt(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectStateSnapshot.Capture",
    "SendStringToExecute",
):
    if forbidden in probe:
        errors.append(f"automation-only probe must not mutate CAD/semantic state directly: {forbidden}")

if probe.count(".Editor.Command(") != 3:
    errors.append("probe must delegate exactly MOVE, ROTATE and STRETCH to Editor.Command")
if 'CommandFlags.Modal | CommandFlags.UsePickSet' not in probe:
    errors.append("source reselection command must preserve PICKFIRST for the next production command")

script_start = runner.find("$scriptOne = @(")
script_end = runner.find("    )", script_start)
if script_start < 0 or script_end < 0:
    errors.append("runner session-one script block is missing")
else:
    script = runner[script_start:script_end]
    ordered = (
        '"QS3DDRAWWALL"',
        '"QS3DSRNATIVEPREPARE"',
        '"QS3DSRNATIVEMOVE"',
        '"QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKMOVE"',
        '"QS3DSRNATIVESELECT", "QS3DBUILD3D", "QS3DSRNATIVECHECKMOVEBUILD"',
        '"QS3DSRNATIVEROTATE"',
        '"QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKROTATE"',
        '"QS3DSRNATIVESTRETCH"',
        '"QS3DSRNATIVESELECT", "QS3DSYNCSOURCE", "QS3DSRNATIVECHECKSTRETCH"',
        '"QS3DSRNATIVESELECT", "QS3DBUILD3D", "QS3DSRNATIVEFINAL"',
        '"QS3DSAVE", "_.QSAVE", "_.QUIT", "_Y"',
    )
    cursor = -1
    for token in ordered:
        current = script.find(token, cursor + 1)
        if current < 0:
            errors.append(f"runner session-one order is missing: {token}")
            break
        cursor = current
    if script.count('"QS3DSYNCSOURCE"') != 3:
        errors.append("runner must reconcile exactly once after each native edit")
    if script.count('"QS3DBUILD3D"') != 2:
        errors.append("runner must rebuild only after MOVE and final STRETCH")

required_runner_tokens = (
    "QS3DSRNATIVEREOPEN",
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_PHASE_RESULT",
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_NONCE",
    "QS3D_SOURCE_RECONCILE_NATIVE_LINE_DWG",
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
    "LOCAL_004_P01_LINE_ONLY",
)
for token in required_runner_tokens:
    if token not in runner:
        errors.append(f"runner is missing exact-SHA/privacy/cleanup contract token: {token}")

if runner.count('"QS3DSRNATIVEREOPEN"') < 1:
    errors.append("runner must execute the cold-reopen verification command")
if "Get-Clipboard" in runner or "Set-Clipboard" in runner:
    errors.append("runner must not inspect or mutate the user's clipboard")
for forbidden_marker_key in (
    '"handle=', '"element_id=', '"project_id=', '"drawing_path=',
    '"exception_message=', '"stack_trace=', '"exception_type=',
):
    if forbidden_marker_key in probe.lower():
        errors.append("sanitized probe marker must not publish IDs, handles, paths, or raw exception details")

claim_tokens = (
    "issue #3281",
    "Status: `ACTIVE`",
    "MOVE",
    "ROTATE",
    "STRETCH",
    "LINE",
    "automation-only",
    "grip",
)
for token in claim_tokens:
    if token not in claim:
        errors.append(f"claim is missing bounded ownership token: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-004 P01 drives one production Direct Draw LINE through native MOVE/ROTATE/STRETCH, production reconcile/rebuild/save, cold reopen and exact privacy/cleanup guards without direct probe mutation.")
