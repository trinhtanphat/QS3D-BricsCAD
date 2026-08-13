#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/LevelZRuntimeProbeCommands.cs"
STRUCTURAL = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-level-z.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-11-codex-local-019ff0c5-local003-level-z-chain.md"
errors: list[str] = []

for path in (COMMAND, STRUCTURAL, RUNNER, HELPER, CLAIM):
    if not path.is_file():
        errors.append("missing Level-Z boundary probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DLEVELZPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_LEVEL_Z_RESULT"',
        'NonceVariable = "QS3D_LEVEL_Z_NONCE"',
        'SourceShaVariable = "QS3D_LEVEL_Z_SOURCE_SHA"',
        'EndsWith(".level-z-probe-copy.dwg"',
        'CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit)',
        'DrawingUnitResolutionPolicy.BindQuantityUnit(',
        'DrawingUnitResolutionSource.NativeInsunits',
        'CadGeometryGuard.ToDrawingUnits(document, 1d, "Level Z probe meter scale")',
        'RequireAssemblyRevision(typeof(LevelZRuntimeProbeCommands).Assembly, sourceSha',
        'RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha',
        'WallSolidBuilder.BuildSelectedLineWalls(',
        'StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Beam)',
        'OpeningBooleanService.CutLinkedOpenings(',
        'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls(',
        'CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(',
        'BeamRebarSolidBuilder.BuildSelected(',
        'BeamStirrupSolidBuilder.BuildSelected(',
        'ProjectFloorService.AssignBottomLevel(',
        'ProjectFloorService.AssignTopLevel(',
        'VerifyTopOnlyFailsBeforeMutation(',
        'RequireSnapshot(',
        'ProjectFloorService.Update(project, "L2"',
        'ProjectFloorService.Update(project, "L1"',
        'LevelReferenceHealthService().Inspect(project)',
        'schema=QS3D_LEVEL_Z_RUNTIME_V1',
        'LEVEL_Z_RUNTIME_CONTEXT_FAILED',
        'LEVEL_Z_RUNTIME_SOURCE_FAILED',
        'LEVEL_Z_RUNTIME_HOST_BUILD_FAILED',
        'LEVEL_Z_RUNTIME_OPENING_FAILED',
        'LEVEL_Z_RUNTIME_CURTAIN_FRAME_BUILD_FAILED',
        'LEVEL_Z_RUNTIME_CURTAIN_PANEL_BUILD_FAILED',
        'LEVEL_Z_RUNTIME_CURTAIN_RANGE_FAILED',
        'LEVEL_Z_RUNTIME_CURTAIN_MODE_FAILED',
        'LEVEL_Z_RUNTIME_REBAR_FAILED',
        'LEVEL_Z_RUNTIME_LEVEL_EDIT_FAILED',
        'TryWriteFailure(requestedPath, failureCode, observedLegacyRange)',
        'observed_legacy_min_z_m=',
        'observed_legacy_max_z_m=',
        'physical_opening_volume_reduced=true',
        'level_edit_invalidation=true',
        'top_only_fail_closed=',
        'WriteMarkerAtomic(resultPath',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
    ):
        if token not in text:
            errors.append("Level-Z command missing contract token: " + token)
    marker_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    marker_end = text.find("document.Editor.WriteMessage", marker_start)
    marker = text[marker_start:marker_end]
    for forbidden in ("handle=", "element_id=", "drawing_path=", "layer=", "family_name="):
        if forbidden in marker.lower():
            errors.append("Level-Z marker leaks identity field: " + forbidden)

if STRUCTURAL.is_file():
    text = STRUCTURAL.read_text(encoding="utf-8")
    for token in (
        'solid.CreateBox(length, width, height);',
        'Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin)',
        'Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z))',
    ):
        if token not in text:
            errors.append("Structural LINE prism missing centered-box placement token: " + token)
    if 'new Vector3d(-length / 2d, -width / 2d, -height / 2d)' in text:
        errors.append("Structural LINE prism double-offsets centered Solid3d.CreateBox output")

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[ValidatePattern("^[0-9a-fA-F]{40}$")][string]$ExpectedSourceSha',
        '[switch]$ConfirmDisposableCopy',
        '*.level-z-probe-copy.dwg',
        'QS3D_LEVEL_Z_RESULT',
        'QS3D_LEVEL_Z_NONCE',
        'QS3D_LEVEL_Z_SOURCE_SHA',
        'git -C $repoRoot rev-parse --verify HEAD',
        '$sourceShaExitCode = $LASTEXITCODE',
        'git -C $repoRoot status --porcelain=v1 --untracked-files=all',
        '$worktreeExitCode = $LASTEXITCODE',
        'Assembly was not built from ExpectedSourceSha',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DLEVELZPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'Stop-Qs3dLevelProcess -Process $process',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'physical_opening_volume_reduced',
        'curtain_frame_count',
        'curtain_panel_count',
        'beam_rebar_count',
        'beam_stirrup_count',
        'stale_snapshot_count_after_edit',
        'Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_NONCE"',
        'Restore-EnvironmentValue -Name "QS3D_LEVEL_Z_SOURCE_SHA"',
    ):
        if token not in text:
            errors.append("Level-Z runner missing contract token: " + token)
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Level-Z runner contains broad process/window action: " + forbidden)
    if "rev-parse HEAD 2>$null | Select-Object -First 1" in text:
        errors.append("Level-Z runner must not pipe rev-parse through Select-Object because early pipeline closure can corrupt LASTEXITCODE")

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-003", "PENDING_LOCAL", "LOCAL_PASS", "production_level_qualified=false"):
        if token not in text:
            errors.append("Level-Z claim is missing qualification-boundary token: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] automation-only Level-Z probe binds exact source/assembly SHA, uses a disposable synthetic copy, verifies host/opening/Curtain/rebar Z plus fail-closed and Level-edit behavior, sanitizes evidence and does not claim full product qualification")
