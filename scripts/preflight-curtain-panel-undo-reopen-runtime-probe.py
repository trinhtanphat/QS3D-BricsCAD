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
        'SEMANTIC_NATIVE_DIVERGENCE',
        'production_local002_qualified=false',
        'p11_qualified=',
        'FileMode.CreateNew',
        'File.Move(tempPath, path)',
    ):
        if token not in text:
            errors.append("Curtain P11 command missing contract token: " + token)

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
        '"_.UNDO", "1"',
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
        'foreach ($privatePath in @($projectSidecar, $sidecarBackup, $sidecarLock, $drawingLock, $drawingLock2))',
        'Remove-ExactFile -Path $privatePath',
        'Stop-Qs3dLaunchedProcess -Process $processOne',
        'Stop-Qs3dLaunchedProcess -Process $processTwo',
        'process_cleanup_verified',
        'script_cleanup_verified',
        'sidecar_cleanup_verified',
        'drawing_lock_cleanup_verified',
        'drawing_restore_verified',
        '"rebuild_counts_stable"',
        'Restore-EnvironmentValue -Name $name',
    ):
        if token not in text:
            errors.append("Curtain P11 runner missing contract token: " + token)

    if text.count('Start-Process -FilePath $bricscadExe') != 2:
        errors.append("Curtain P11 runner must launch exactly two isolated BricsCAD sessions")
    if text.count('"QS3DCURTAINP11SELECT"') != 2:
        errors.append("Curtain P11 runner must restore the canonical source selection immediately before both builds")
    if text.count('"QS3DCURTAIN3D", "P", ""') != 2:
        errors.append("Curtain P11 runner must explicitly accept the previous canonical source selection for both production builds")
    first_prepare = text.find('"QS3DCURTAINP11PREPARE"')
    first_select = text.find('"QS3DCURTAINP11SELECT"', first_prepare)
    first_build = text.find('"QS3DCURTAIN3D", "P", ""', first_select)
    reopen = text.find('"QS3DCURTAINP11REOPEN"')
    second_select = text.find('"QS3DCURTAINP11SELECT"', reopen)
    second_build = text.find('"QS3DCURTAIN3D", "P", ""', second_select)
    if not (first_prepare < first_select < first_build and reopen < second_select < second_build):
        errors.append("Curtain P11 runner must reselect at a distinct command boundary immediately before each production build")
    if text.find('"_.UNDO", "1"') > text.find('"_.REDO"'):
        errors.append("Curtain P11 runner must execute Undo before Redo")
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

print("PASS: P11 probe keeps handles/IDs private, checks one-sided Undo plus Redo, cold QSDB reopen and ownership-scoped rebuild across two exact-SHA V25 sessions, restores the disposable DWG, removes private sidecars/scripts and keeps broader LOCAL-002 pending.")
