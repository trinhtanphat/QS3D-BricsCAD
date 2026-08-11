#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panels.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, RUNBOOK):
    if not path.is_file():
        errors.append("missing curtain-panel runtime probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINPANELPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINPANELPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_RUNTIME_V1',
        'x.Category == ElementCategory.GlassWall',
        'GeneratedCurtainPanelBuildState',
        'GeneratedSolidHandle',
        'GeneratedCurtainFrameHandles',
        'GeneratedCurtainPanelHandles',
        'RequireDisjoint(sourceHandles, hostHandles, frameHandles, panelHandles)',
        'CadHandleService.GetLiveSolidHandles(document, hostHandles)',
        'CadHandleService.GetLiveSolidHandles(document, frameHandles)',
        'CadHandleService.GetLiveSolidHandles(document, panelHandles)',
        'new GeneratedCurtainPanelHealthService().Inspect(project, livePanels)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'SemanticSelectionResolver.ResolveImplied(document, project)',
        'document.Editor.SetImpliedSelection(sourceIds.ToArray())',
        'ownership_sets_disjoint=true',
        'panel_build_state_complete=true',
        'WriteMarkerAtomic(resultPath',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
    ):
        if token not in text:
            errors.append("curtain-panel runtime command missing contract token: " + token)
    marker = text[text.find('WriteMarkerAtomic(resultPath, new[]'):text.find('document.Editor.WriteMessage', text.find('WriteMarkerAtomic(resultPath, new[]'))]
    for forbidden in ("handle=", "element_id=", "drawing_path=", "layer=", "family_name="):
        if forbidden in marker.lower():
            errors.append("curtain-panel runtime marker leaks identity field: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopy',
        '*.curtain-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_RESULT',
        'QS3D_CURTAIN_PANEL_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DDRAWGLASSWALL"',
        '"0,0", "5000,0", ""',
        '"QS3DCURTAINPANELPREPARE"',
        '"QS3DCURTAIN3D"',
        '"QS3DCURTAINPANELPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'Require-Qs3dValue -Marker $marker -Key "health_issue_count" -Expected "0"',
        'if ($panelCount -ne $panelMetadataCount)',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_NONCE"',
    ):
        if token not in text:
            errors.append("curtain-panel runtime runner missing contract token: " + token)
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("curtain-panel runtime runner contains broad process/window action: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P01", "PENDING_LOCAL", "QS3DCURTAIN3D"):
        if token not in text:
            errors.append("curtain-panel runbook missing runtime-boundary token: " + token)

print("QS3D curtain-panel runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: automation-only curtain-panel P01 probe uses a disposable synthetic copy, validates distinct live host/frame/panel ownership plus Health and Locate, records aggregate evidence, and keeps the broader LOCAL-002 matrix PENDING_LOCAL.")
