#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelPathRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-path.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local002-p03-curtain-path-runtime-probe.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, RUNBOOK, CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P03 path probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINPATHPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINPATHPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_PATH_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_PATH_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_PATH_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P03_ONLY',
        'production_local002_qualified=false',
        'RequireLegacyNoLevel(hosts[0])',
        'polyline.NumberOfVertices != 3',
        'polyline.GetBulgeAt(index)',
        'new Point2(0d, 0d), new Point2(4d, 0d), new Point2(4d, 3d)',
        'CadPolylinePathReader.ReadOpenWcsXy(',
        'CurtainWallDetailPlanner.Plan(',
        'CurtainPathFramePlanner.Plan(centerline, rectangles)',
        'pathPlan.PathSegmentCount != 2',
        'GeneratedCurtainPanelMode", "PathPanelSolids"',
        'GeneratedCurtainPanelSourceKind", "OpenPolyline"',
        'GeneratedCurtainPanelOpeningCount", 0',
        'GeneratedCurtainPanelPathSegmentCount", pathPlan.PathSegmentCount',
        'GeneratedCurtainPanelMappedCount", rectangles.Count',
        'solid.GeometricExtents',
        'MatchNativePieces(native, pathPlan.Pieces, depthM, baseM)',
        'matchedBySegment[0] != segment0 || matchedBySegment[1] != segment1',
        'GeneratedCurtainPanelHealthService().Inspect(project, livePanels)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'SemanticSelectionResolver.ResolveImplied(document, project)',
        'source_geometry_preserved=true',
        'ownership_sets_disjoint=true',
        'open_straight_polyline=true',
        'source_length_m=7',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'private static readonly HashSet<string> FailurePhases',
        'private static readonly HashSet<string> FailureCodes',
        'TryWriteFailure(requestedPath, nonce, phase.Value, FailureCode(error))',
        'error_code=CURTAIN_PANEL_PATH_RUNTIME_FAILED',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P03 command missing contract token: " + token)
    marker_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    marker_end = text.find("document.Editor.WriteMessage", marker_start)
    marker = text[marker_start:marker_end]
    for forbidden in (
        "handle=", "handles=", "element_id=", "project_id=", "drawing_path=",
        "plugin_path=", "layer=", "family_name=", "source_id=", "host_id=",
    ):
        if forbidden in marker.lower():
            errors.append("Curtain-panel P03 marker leaks identity field: " + forbidden)
    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure_marker = text[failure_start:failure_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P03 FAIL marker exposes exception detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-path-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_PATH_RESULT',
        'QS3D_CURTAIN_PANEL_PATH_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DDRAWGLASSWALL", "0,0", "4000,0", "4000,3000", ""',
        '"QS3DCURTAINPATHPREPARE"',
        '"QS3DCURTAIN3D"',
        '"QS3DCURTAINPATHPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository.',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Curtain-path runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Launched BricsCAD Curtain-path process did not exit.',
        'Get-Process -Name "bricscad" -ErrorAction SilentlyContinue',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'Curtain-path runtime script cleanup failed.',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'sidecar_absent_verified = $true',
        'backup_absent_verified = $true',
        'Read-Qs3dAllowedValue',
        'QS3D_CURTAIN_PANEL_PATH_RUNTIME_V1',
        '$failurePhases = @(',
        '$failureCodes = @(',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new',
        'Curtain-path FAIL marker contains a non-contract field.',
        '$diagnosticFailure = $true',
        'if ($diagnosticFailure)',
        'cleanup was verified.',
        '$segment0Count + $segment1Count -ne $pathPieceCount',
        '$nativeMatchCount -ne $pathPieceCount',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_PATH_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_PATH_NONCE"',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P03 runner missing contract token: " + token)
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Curtain-panel P03 runner contains broad process/window action: " + forbidden)
    fail_start = text.find('if ($marker.ContainsKey("status")')
    fail_end = text.find("Stop-Qs3dLaunchedProcess -Process $process", fail_start)
    fail_branch = text[fail_start:fail_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", "GetType("):
        if forbidden in fail_branch:
            errors.append("Curtain-panel P03 runner FAIL branch exposes exception detail: " + forbidden)
    deferred_failure = text.find("if ($diagnosticFailure)", fail_start)
    drawing_hash_after = text.find("$drawingHashAfter =", fail_start)
    backup_after = text.find("Curtain-path runtime probe persisted an unexpected sidecar or backup.", fail_start)
    if deferred_failure < 0 or drawing_hash_after < 0 or backup_after < 0 or deferred_failure < drawing_hash_after or deferred_failure < backup_after:
        errors.append("Curtain-panel P03 sanitized FAIL must be deferred until process/script/DWG/sidecar/backup cleanup checks finish")
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Curtain-panel P03 process cleanup must fail visible: " + forbidden)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata.lower():
            errors.append("Curtain-panel P03 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in (
        "LOCAL-002", "P03", "PENDING_LOCAL", "QS3DCURTAINPATHPROBE",
        "test-bricscad-v25-curtain-panel-path.ps1", "legacy/no-Level",
        "straight-segment", "QS3D_CURTAIN_PANEL_PATH_RUNTIME_V1",
    ):
        if token not in text:
            errors.append("Curtain-panel runbook missing P03 handoff token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P03", "No BricsCAD", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P03 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P03 claim must remain ACTIVE during implementation or be COMPLETED at close-out")

print("QS3D Curtain-panel P03 straight-path runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P03 probe seeds one synthetic legacy/no-Level open straight POLYLINE, reconstructs the authoritative station-mapped panel plan, independently matches native bounds on both path segments, and enforces exact-SHA/privacy/cleanup without claiming BricsCAD runtime evidence.")
