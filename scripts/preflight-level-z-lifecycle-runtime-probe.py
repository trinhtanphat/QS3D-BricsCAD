#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/LevelZLifecycleRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-level-z-lifecycle.ps1"
P11_COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelUndoReopenRuntimeProbeCommands.cs"
P11_RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-undo-reopen.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-11-codex-local-019ff0c5-local003-level-z-chain.md"
errors: list[str] = []

for path in (COMMAND, RUNNER, P11_COMMAND, P11_RUNNER, HELPER, CLAIM):
    if not path.is_file():
        errors.append("missing Level lifecycle boundary file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DLEVELZLIFECYCLECONFIGURE", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLEBASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLECHECKUNDO", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLECHECKREDO", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLESESSION1", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLEREOPEN", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLEAFTERREBUILD", CommandFlags.Modal)',
        'CommandMethod("QS3DLEVELZLIFECYCLECOMPLETE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_LEVEL_Z_LIFECYCLE_RESULT"',
        'PhaseResultVariable = "QS3D_LEVEL_Z_LIFECYCLE_PHASE_RESULT"',
        'NonceVariable = "QS3D_LEVEL_Z_LIFECYCLE_NONCE"',
        'SourceShaVariable = "QS3D_LEVEL_Z_LIFECYCLE_SOURCE_SHA"',
        'ExpectedHostVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_HOSTS"',
        'ExpectedFrameVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_FRAMES"',
        'ExpectedPanelVariable = "QS3D_LEVEL_Z_LIFECYCLE_EXPECTED_PANELS"',
        'EndsWith(".level-z-lifecycle-probe-copy.dwg"',
        'RequireAssemblyRevision(typeof(LevelZLifecycleRuntimeProbeCommands).Assembly, sourceSha',
        'RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha',
        'ProjectFloorService.Create(context.Project, BottomLevelId',
        'ProjectFloorService.Create(context.Project, TopLevelId',
        'ProjectFloorService.AssignBottomLevel(',
        'ProjectFloorService.AssignTopLevel(',
        'ElementVerticalPlacementService.Resolve(project, owner, 0d, 3.6d, 0d)',
        'RequireNear(BoundedBottomM, placement.BottomElevationM',
        'RequireNear(BoundedTopM, placement.TopElevationM',
        'RequireSnapshot(owner, "GeneratedSolid", BoundedBottomM, BoundedTopM, "BottomTopLevels")',
        'RequireSnapshot(owner, "GeneratedCurtainFrame", BoundedBottomM, BoundedTopM, "BottomTopLevels")',
        'RequireSnapshot(owner, "GeneratedCurtainPanel", BoundedBottomM, BoundedTopM, "BottomTopLevels")',
        'owner.IsGeneratedSolidStale()',
        'owner.IsGeneratedCurtainFrameStale()',
        'owner.IsGeneratedCurtainPanelStale()',
        'LevelReferenceHealthService().Inspect(project)',
        'state.UndoLevelConfigurationPreserved',
        'state.UndoPreBuildHostRestored',
        'state.UndoGeneratedAfterAbsent',
        'state.RedoLevelOutputCoherent',
        'state.OldGeneratedRemoved = AllAbsent(',
        'state.NewGeneratedDisjoint =',
        'state.RebuildLevelOutputCoherent =',
        'schema=" + Schema',
        'qualification_boundary=LOCAL_003_LEVEL_LIFECYCLE_ONLY',
        'production_local003_qualified=false',
        'level_lifecycle_qualified=',
        'FileMode.CreateNew',
        'File.Move(temporaryPath, fullPath)',
    ):
        if token not in text:
            errors.append("Level lifecycle command missing contract token: " + token)
    for forbidden in (
        '"handle=',
        '"owner_id=',
        '"element_id=',
        '"drawing_path=',
        '"sidecar_path=',
        '"family_name=',
        'exception_message=',
        'exception_stack=',
    ):
        if forbidden.lower() in text.lower():
            errors.append("Level lifecycle marker exposes a forbidden field: " + forbidden)
    for forbidden in (
        "error.Message",
        "error.StackTrace",
        "error.Source",
        "error.Data",
        "ProjectContextCoordinator.Save(",
        "ProjectContextCoordinator.Reload(",
        "CurtainWallUndoCoordinator",
        "ProjectStateSnapshot",
    ):
        if forbidden in text:
            errors.append("Level lifecycle additive command crosses its production boundary: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[Parameter(Mandatory = $true)][string]$ExpectedSourceSha',
        '[Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy',
        '*.level-z-lifecycle-probe-copy.dwg',
        '$DrawingCopy.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar',
        '$ArtifactDir.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar',
        'status --porcelain=v1 --untracked-files=all',
        'ExpectedSourceSha does not match the worktree HEAD.',
        'Level lifecycle qualification requires a clean committed worktree.',
        'Level lifecycle assembly was not built from ExpectedSourceSha.',
        'Close all BricsCAD processes before isolated Level lifecycle qualification.',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $Process',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'Stop-Process -Id $Process.Id -Force',
        'QS3D_LEVEL_Z_LIFECYCLE_RESULT',
        'QS3D_LEVEL_Z_LIFECYCLE_PHASE_RESULT',
        'QS3D_LEVEL_Z_LIFECYCLE_NONCE',
        'QS3D_LEVEL_Z_LIFECYCLE_SOURCE_SHA',
        'QS3D_CURTAIN_P11_RESULT',
        'QS3D_CURTAIN_P11_PHASE_RESULT',
        'QS3D_CURTAIN_P11_NONCE',
        '"QS3DDRAWGLASSWALL"',
        '"QS3DLEVELZLIFECYCLECONFIGURE"',
        '"QS3DCURTAINP11PREPARE"',
        '"QS3DCURTAINP11SELECT"',
        '"_.UNDO", "_Mark"',
        '"_.UNDO", "_Back"',
        '"_.UNDO", "_Begin"',
        '"_.UNDO", "_End"',
        '"_.U"',
        '"_.REDO"',
        '"QS3DLEVELZLIFECYCLECHECKUNDO"',
        '"QS3DLEVELZLIFECYCLECHECKREDO"',
        '"QS3DSAVE"',
        '"_.QSAVE"',
        '"QS3DLEVELZLIFECYCLEREOPEN"',
        '"QS3DLEVELZLIFECYCLEAFTERREBUILD"',
        '"QS3DLEVELZLIFECYCLECOMPLETE"',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_saved',
        'drawing_copy_sha256_rebuilt',
        'drawing_copy_sha256_restored',
        'process_cleanup_verified',
        'script_cleanup_verified',
        'sidecar_cleanup_verified',
        'drawing_lock_cleanup_verified',
        'drawing_backup_cleanup_verified',
        'drawing_restore_verified',
        'Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force',
        'foreach ($privatePath in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup))',
        'Restore-EnvironmentValue -Name $name -Value $oldEnvironment[$name]',
        'production_local003_qualified',
        'undo_level_config_preserved',
        'undo_prebuild_host_restored',
        'undo_generated_after_absent',
        'redo_level_output_coherent',
        'reopen_level_config_coherent',
        'reopen_level_output_coherent',
        'rebuild_level_output_coherent',
        'level_lifecycle_qualified',
    ):
        if token not in text:
            errors.append("Level lifecycle runner missing contract token: " + token)
    if text.count('Start-Process -FilePath $bricscadExe') != 2:
        errors.append("Level lifecycle runner must launch exactly two isolated BricsCAD sessions")
    if text.count('"QS3DCURTAIN3D", "P", ""') != 3:
        errors.append("Level lifecycle runner must execute exactly three production Curtain builds")
    if text.count('"QS3DLEVELZLIFECYCLEBASELINE"') != 2:
        errors.append("Level lifecycle runner must capture one baseline for each session-one build")
    compact = "".join(text.split())
    session_one = (
        '"QS3DLEVELZLIFECYCLECONFIGURE","QS3DCURTAINP11PREPARE","QS3DCURTAINP11SELECT",'
        '"_.UNDO","_Mark","QS3DCURTAIN3D","P","","QS3DCURTAINP11BASELINE",'
        '"QS3DLEVELZLIFECYCLEBASELINE","_.UNDO","_Back","QS3DCURTAINP11CHECKUNDO",'
        '"QS3DLEVELZLIFECYCLECHECKUNDO","QS3DCURTAINP11SELECT","_.UNDO","_Begin",'
        '"QS3DCURTAIN3D","P","","QS3DCURTAINP11BASELINE","QS3DLEVELZLIFECYCLEBASELINE",'
        '"_.UNDO","_End","_.U","_.REDO","QS3DCURTAINP11CHECKREDO",'
        '"QS3DLEVELZLIFECYCLECHECKREDO","QS3DSAVE","_.QSAVE",'
        '"QS3DCURTAINP11SESSION1","QS3DLEVELZLIFECYCLESESSION1"'
    )
    session_two = (
        '"QS3DCURTAINP11REOPEN","QS3DLEVELZLIFECYCLEREOPEN","QS3DCURTAINP11SELECT",'
        '"QS3DCURTAIN3D","P","","QS3DCURTAINP11AFTERREBUILD",'
        '"QS3DLEVELZLIFECYCLEAFTERREBUILD","QS3DSAVE","_.QSAVE",'
        '"QS3DCURTAINP11COMPLETE","QS3DLEVELZLIFECYCLECOMPLETE"'
    )
    if session_one not in compact:
        errors.append("Level lifecycle runner must preserve the exact grouped Undo/Redo session-one order")
    if session_two not in compact:
        errors.append("Level lifecycle runner must validate cold reopen before ownership-scoped rebuild")
    for forbidden in (
        "Get-Process -Name '*'",
        "Process.GetProcesses",
        "SendKeys",
        "SetForegroundWindow",
        "git reset",
        "git clean",
        "drawing_path =",
        "sidecar_path =",
        "qualification_error =",
    ):
        if forbidden in text:
            errors.append("Level lifecycle runner contains a broad/private operation: " + forbidden)

if P11_COMMAND.is_file():
    text = P11_COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINP11CHECKUNDO", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11CHECKREDO", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11REOPEN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11AFTERREBUILD", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11COMPLETE", CommandFlags.Modal)',
    ):
        if token not in text:
            errors.append("existing Curtain P11 lifecycle dependency is missing: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in (
        "Issue `#1521`",
        "LevelZLifecycleRuntimeProbeCommands.cs",
        "test-bricscad-v25-level-z-lifecycle.ps1",
        "preflight-level-z-lifecycle-runtime-probe.py",
        "Undo/Redo",
        "save/close",
        "cold reopen",
        "production_local003_qualified=false",
    ):
        if token not in text:
            errors.append("Level lifecycle claim is missing reservation token: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] LOCAL-003 Level lifecycle reuses production Curtain P11 Undo/save/reopen, verifies Bottom+Top Level Z and snapshots through Undo/Redo/cold rebuild, binds exact SHA, sanitizes markers, restores the synthetic DWG and keeps broader LOCAL-003 pending.")
