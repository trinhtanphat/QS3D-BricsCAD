#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelUndoReopenRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-undo-reopen.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, RUNBOOK):
    if not path.is_file():
        errors.append("missing Curtain P11 probe surface: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINP11PREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11SELECT", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11BASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11CHECKUNDO", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11CHECKREDO", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11SESSION1", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11REOPEN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11AFTERREBUILD", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP11COMPLETE", CommandFlags.Modal)',
        'QS3D_CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_V1',
        'QS3D_CURTAIN_P11_SENTINEL',
        'CreateSentinel(context.Document, context.Nonce)',
        'SelectSingleSource(context.Document, owner)',
        'SameSemanticAndNative(state.Before, current)',
        'AllPresent(context.Document, GeneratedHandles(state.Before))',
        'AllAbsent(context.Document, GeneratedHandles(after))',
        'SameSemanticAndNative(after, current)',
        'RequireExpectedCount(ExpectedHostVariable, reopened.HostHandles.Count)',
        'RequireExpectedCount(ExpectedFrameVariable, reopened.FrameHandles.Count)',
        'RequireExpectedCount(ExpectedPanelVariable, reopened.PanelHandles.Count)',
        'state.OldGeneratedRemoved = AllAbsent(context.Document, previous)',
        'state.NewGeneratedDisjoint = previous.All(handle => !current.Contains(handle))',
        'state.CountsStable = rebuilt.HostHandles.Count == state.Reopened.HostHandles.Count',
        'new GeneratedCurtainFrameHealthService().Inspect(project, liveFrames)',
        'CurtainWallFrameLiveStateService.Inspect(document, project)',
        'new GeneratedCurtainPanelHealthService().Inspect(project, livePanels)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'error_code=CURTAIN_PANEL_UNDO_REOPEN_RUNTIME_FAILED',
        'UNDO_AFTER_GENERATED_STILL_PRESENT',
        'UNDO_NATIVE_REMOVED_SEMANTIC_NOT_RESTORED',
        'UNDO_SOURCE_SENTINEL_DRIFT',
        'undo_after_generated_absent=',
        'undo_semantic_before_restored=',
        'undo_source_sentinel_preserved=',
        'undo_failure_code=',
        'production_local002_qualified=false',
        'p11_qualified=',
        'FileMode.CreateNew',
        'File.Move(tempPath, path)',
    ):
        if token not in text:
            errors.append("Curtain P11 command missing contract token: " + token)

    for code in (
        'UNDO_AFTER_GENERATED_STILL_PRESENT',
        'UNDO_NATIVE_REMOVED_SEMANTIC_NOT_RESTORED',
        'UNDO_SOURCE_SENTINEL_DRIFT',
    ):
        if text.count('"' + code + '"') != 1:
            errors.append("Curtain P11 probe must define exactly one sanitized Undo branch code: " + code)
    if 'SEMANTIC_NATIVE_DIVERGENCE' in text:
        errors.append("Curtain P11 probe must not collapse every Undo mismatch into SEMANTIC_NATIVE_DIVERGENCE")
    check_undo = text[text.find('public void CheckUndo()'):text.find('[CommandMethod("QS3DCURTAINP11CHECKREDO"', text.find('public void CheckUndo()'))]
    for token in (
        'state.UndoAfterGeneratedAbsent = AllAbsent(context.Document, GeneratedHandles(after));',
        'state.UndoSemanticBeforeRestored = SameSemanticAndNative(state.Before, current)',
        'state.UndoSourceSentinelPreserved = SameSourceAndSentinel(state.Before, current);',
        'state.UndoFailureCode = ClassifyUndoFailure(',
    ):
        if token not in check_undo:
            errors.append("Curtain P11 Undo check must publish the bounded sanitized branch evidence: " + token)

    complete = text[text.find('public void Complete()'):text.find('private static void Execute(', text.find('public void Complete()'))]
    for forbidden in ("handle=", "element_id=", "project_id=", "family_id=", "drawing_path=", "profile=", "exception=", "message="):
        if forbidden in complete.lower():
            errors.append("Curtain P11 final marker leaks a forbidden field: " + forbidden)
    if 'GeneratedCurtainPanelHandles' not in text or 'GeneratedCurtainFrameHandles' not in text:
        errors.append("Curtain P11 command must validate canonical frame/panel owner slots")
    for forbidden in ("DllImport", "dynamic ", "GetType().Get", "BLT"):
        if forbidden in text:
            errors.append("Curtain P11 command crosses the public/local automation boundary: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopy',
        '*.curtain-undo-reopen-probe-copy.dwg',
        'DrawingCopy must be an ordinary disposable copy outside the repository.',
        'ArtifactDir must stay outside the repository.',
        'QS3D_CURTAIN_P11_RESULT',
        'QS3D_CURTAIN_P11_PHASE_RESULT',
        'QS3D_CURTAIN_P11_NONCE',
        'QS3D_CURTAIN_P11_UNDO_AFTER_GENERATED_ABSENT',
        'QS3D_CURTAIN_P11_UNDO_SEMANTIC_BEFORE_RESTORED',
        'QS3D_CURTAIN_P11_UNDO_SOURCE_SENTINEL_PRESERVED',
        'QS3D_CURTAIN_P11_UNDO_FAILURE_CODE',
        'rev-parse HEAD',
        'status --porcelain=v1 --untracked-files=all',
        '$expectedAssemblyRevision = "+" + $gitHead',
        'ProductVersion',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        '"QS3DDRAWGLASSWALL"',
        '"QS3DCURTAINP11PREPARE"',
        '"QS3DCURTAINP11SELECT"',
        '"QS3DCURTAIN3D", "P", ""',
        '"QS3DCURTAINP11BASELINE"',
        '"_.UNDO", "_Mark"',
        '"_.UNDO", "_Back"',
        '"QS3DCURTAINP11CHECKUNDO"',
        '"_.REDO"',
        '"QS3DCURTAINP11CHECKREDO"',
        '"QS3DSAVE"',
        '"_.QSAVE"',
        '"QS3DCURTAINP11SESSION1"',
        '"QS3DCURTAINP11REOPEN"',
        '"QS3DCURTAINP11AFTERREBUILD"',
        '"QS3DCURTAINP11COMPLETE"',
        'Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force',
        'foreach ($privatePath in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2, $drawingBackup))',
        'Remove-ExactFile -Path $privatePath',
        'Stop-Qs3dLaunchedProcess -Process $processOne',
        'Stop-Qs3dLaunchedProcess -Process $processTwo',
        'process_cleanup_verified',
        'script_cleanup_verified',
        'sidecar_cleanup_verified',
        'drawing_lock_cleanup_verified',
        'drawing_backup_cleanup_verified',
        'drawing_restore_verified',
        '"rebuild_counts_stable"',
        'Restore-EnvironmentValue -Name $name',
    ):
        if token not in text:
            errors.append("Curtain P11 runner missing contract token: " + token)

    if text.count('Start-Process -FilePath $bricscadExe') != 2:
        errors.append("Curtain P11 runner must launch exactly two isolated BricsCAD sessions")
    if text.count('"QS3DCURTAINP11SELECT"') != 3:
        errors.append("Curtain P11 runner must restore the canonical source selection immediately before all three builds")
    if text.count('"QS3DCURTAIN3D", "P", ""') != 3:
        errors.append("Curtain P11 runner must explicitly accept the previous canonical source selection for all three production builds")
    if text.count('"QS3DCURTAINP11BASELINE"') != 2:
        errors.append("Curtain P11 runner must capture one in-memory after-state for each session-one build")
    if text.count('"QS3DCURTAINP11CHECKUNDO"') != 1 or text.count('"QS3DCURTAINP11CHECKREDO"') != 1:
        errors.append("Curtain P11 runner must check exactly one isolated Undo cycle and one isolated Redo cycle")
    first_prepare = text.find('"QS3DCURTAINP11PREPARE"')
    first_select = text.find('"QS3DCURTAINP11SELECT"', first_prepare)
    first_undo_mark = text.find('"_.UNDO", "_Mark"', first_select)
    first_build = text.find('"QS3DCURTAIN3D", "P", ""', first_undo_mark)
    first_baseline = text.find('"QS3DCURTAINP11BASELINE"', first_build)
    first_undo_back = text.find('"_.UNDO", "_Back"', first_baseline)
    check_undo = text.find('"QS3DCURTAINP11CHECKUNDO"', first_undo_back)
    second_select = text.find('"QS3DCURTAINP11SELECT"', check_undo)
    second_undo_mark = text.find('"_.UNDO", "_Mark"', second_select)
    second_build = text.find('"QS3DCURTAIN3D", "P", ""', second_undo_mark)
    second_baseline = text.find('"QS3DCURTAINP11BASELINE"', second_build)
    second_undo_back = text.find('"_.UNDO", "_Back"', second_baseline)
    redo = text.find('"_.REDO"', second_undo_back)
    check_redo = text.find('"QS3DCURTAINP11CHECKREDO"', redo)
    reopen = text.find('"QS3DCURTAINP11REOPEN"')
    third_select = text.find('"QS3DCURTAINP11SELECT"', reopen)
    third_build = text.find('"QS3DCURTAIN3D", "P", ""', third_select)
    if not (
        first_prepare < first_select < first_undo_mark < first_build < first_baseline < first_undo_back < check_undo
        < second_select < second_undo_mark < second_build < second_baseline < second_undo_back < redo < check_redo
        and reopen < third_select < third_build
    ):
        errors.append("Curtain P11 runner must isolate one Undo-check build, one immediate-Redo build and one cold-reopen rebuild")
    compact = ''.join(text.split())
    if (
        text.count('"_.UNDO"') != 4
        or text.count('"_Mark"') != 2
        or text.count('"_Back"') != 2
        or text.count('"_.REDO"') != 1
        or '"_.UNDO","1"' in compact
        or '"QS3DCURTAINP11SELECT","_.UNDO","_Mark","QS3DCURTAIN3D","P",""' not in compact
        or '"QS3DCURTAINP11BASELINE","_.UNDO","_Back","QS3DCURTAINP11CHECKUNDO","QS3DCURTAINP11SELECT","_.UNDO","_Mark","QS3DCURTAIN3D","P","","QS3DCURTAINP11BASELINE","_.UNDO","_Back","_.REDO","QS3DCURTAINP11CHECKREDO"' not in compact
    ):
        errors.append("Curtain P11 runner must use two native Undo Mark/Back pairs and let the second Back immediately precede Redo")
    if text.find('"QS3DCURTAINP11REOPEN"') > text.rfind('"QS3DCURTAIN3D"'):
        errors.append("Curtain P11 second session must validate cold reopen before rebuild")
    if text.find('Copy-Item -LiteralPath $originalCopyPath -Destination $DrawingCopy -Force') < text.find('Stop-Qs3dLaunchedProcess -Process $processTwo'):
        errors.append("Curtain P11 runner must stop the second host before restoring the disposable DWG")
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow", "git reset", "git clean"):
        if forbidden in text:
            errors.append("Curtain P11 runner contains a broad/destructive operation: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P11", "Undo", "save/reopen", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain panel runbook is missing P11 boundary token: " + token)

print("QS3D Curtain P11 Undo/save-reopen runtime preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: P11 probe keeps handles/IDs private, isolates separate Undo-check and immediate-Redo builds with native Undo Mark/Back, retains cold QSDB reopen and ownership-scoped rebuild across two exact-SHA V25 sessions, restores the disposable DWG, removes private sidecars/scripts and keeps broader LOCAL-002 pending.")
