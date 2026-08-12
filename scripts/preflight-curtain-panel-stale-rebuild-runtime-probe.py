#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelStaleRebuildRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-stale-rebuild.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local002-p05-curtain-panel-stale-rebuild-runtime-probe.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, RUNBOOK, CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P05 stale/rebuild file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINSTALESEEDHOSTS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALESEEDOPENING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEBASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEMUTATEGRID", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEVERIFYGRID", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEMUTATEDEPTH", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEVERIFYDEPTH", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEMUTATEHEIGHT", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEVERIFYHEIGHT", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEMUTATEOPENING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEVERIFYOPENING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEMUTATESOURCE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEAFTERSYNC", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINSTALEPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_STALE_REBUILD_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_STALE_REBUILD_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P05_ONLY',
        'production_local002_qualified=false',
        'AppendLine(document, transaction, modelSpace, 0d, 0d, 5d, 0d',
        'AppendLine(document, transaction, modelSpace, 0d, 10d, 5d, 10d',
        'AppendLine(document, transaction, modelSpace, 2d, 0d, 3d, 0d',
        'RequireLegacyNoLevel(element)',
        'target.SetProperty("CurtainMaxPanelWidthM", "0.8")',
        'target.SetProperty("CurtainMaxPanelHeightM", "1")',
        'target.SetProperty("ThicknessM", "0.02")',
        'target.SetProperty("HeightM", "4.2")',
        'new HostLinkService().LinkOpening(project, state.OpeningId, state.TargetId)',
        'line.EndPoint = new Point3d(endX, line.EndPoint.Y, line.EndPoint.Z)',
        'Direct CAD edit must not fabricate semantic owner stale state before Source Sync.',
        'RequireNear(RequiredDouble(target, "LengthM", false), 5d',
        'RequireNear(RequiredDouble(target, "LengthM", false), 6d',
        'CURTAIN_PANEL_LIVE_GEOMETRY_STALE',
        'CURTAIN_PANEL_CONFIG_STALE',
        'CURTAIN_PANEL_GENERATED_STALE',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'ProjectElement.GeneratedCurtainPanelStaleSnapshotKey',
        'string.Join(";", state.Target.PanelHandles.OrderBy',
        'target.Properties.Keys.Any(x => x.StartsWith("GeneratedCurtainPanel"',
        'CadHandleService.Resolve(document, state.Target.HostHandles.Concat(state.Target.FrameHandles).Concat(state.Target.PanelHandles)).Count != 0',
        'CurtainWallDetailPlanner.Plan(',
        'CurtainWallPanelBuilderSupport.ReadLineOpenings(',
        'CurtainWallOpeningPanelPlanner.Plan(',
        'CurtainWallPanelFingerprint.Compute(',
        'CurtainWallFrameLiveFingerprint.Compute(',
        'solid.GeometricExtents',
        'MatchNativePieces(document, native, plan.Pieces, line, depthM',
        'RequireNoOpeningIntersection(plan.Pieces, openings)',
        'CadGeometryGuard.ToMeters(document, line.StartPoint.X',
        'next.PanelHandles.Intersect(state.Target.PanelHandles',
        'CadHandleService.Resolve(document, state.Target.PanelHandles).Count != 0',
        'AssertControlUnchanged(document, project, state)',
        'BoundsEqual(current.NativeBounds, expected.NativeBounds)',
        'SemanticSelectionResolver.ResolveImplied(document, project)',
        'semantic_owner_stale_transitions=4',
        'source_live_drift_transition_count=1',
        'target_replacement_count=5',
        'unrelated_owner_unchanged=true',
        'source_owner_sample_distinguished=true',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'private static readonly HashSet<string> FailurePhases',
        'private static readonly HashSet<string> FailureCodes',
        'error_code=CURTAIN_PANEL_STALE_REBUILD_RUNTIME_FAILED',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P05 command missing contract token: " + token)
    for forbidden in (
        'WallSolidBuilder.BuildSelected', 'CurtainWallPanelSolidBuilder.BuildSelected',
        'CurtainWallPathPanelSolidBuilder.BuildSelected', 'GeneratedDependentGeometryInvalidator.Prepare',
        '/ 1000d', '* 1000d',
    ):
        if forbidden in text:
            errors.append("Curtain-panel P05 probe duplicates production/build or hardcodes drawing units: " + forbidden)
    marker_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    marker_end = text.find("document.Editor.WriteMessage", marker_start)
    marker = text[marker_start:marker_end]
    for forbidden in (
        "handle=", "handles=", "element_id=", "project_id=", "drawing_path=",
        "plugin_path=", "layer=", "family_name=", "source_id=", "host_id=",
        "config_fingerprint=", "live_fingerprint=",
    ):
        if forbidden in marker.lower():
            errors.append("Curtain-panel P05 marker leaks identity field: " + forbidden)
    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure_marker = text[failure_start:failure_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P05 FAIL marker exposes exception detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-stale-rebuild-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_STALE_REBUILD_RESULT',
        'QS3D_CURTAIN_PANEL_STALE_REBUILD_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DCURTAINSTALESEEDHOSTS"',
        '"QS3DGLASSWALL"',
        '"QS3DCURTAINSTALESEEDOPENING"',
        '"QS3DDOOR"',
        '"QS3DCURTAINSTALEPREPARE"',
        '"QS3DCURTAINSTALEBASELINE"',
        '"QS3DCURTAINSTALEMUTATEGRID"',
        '"QS3DCURTAINSTALEVERIFYGRID"',
        '"QS3DCURTAINSTALEMUTATEDEPTH"',
        '"QS3DCURTAINSTALEVERIFYDEPTH"',
        '"QS3DCURTAINSTALEMUTATEHEIGHT"',
        '"QS3DCURTAINSTALEVERIFYHEIGHT"',
        '"QS3DCURTAINSTALEMUTATEOPENING"',
        '"QS3DCURTAINSTALEVERIFYOPENING"',
        '"QS3DCURTAINSTALEMUTATESOURCE"',
        '"QS3DSYNCSOURCE"',
        '"QS3DCURTAINSTALEAFTERSYNC"',
        '"QS3DCURTAINSTALEPROBE"',
        '"QS3DCURTAIN3D"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository.',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Curtain P05 runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Launched BricsCAD Curtain P05 process did not exit.',
        'Get-Process -Name "bricscad" -ErrorAction SilentlyContinue',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'Curtain P05 runtime script cleanup failed.',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'sidecar_absent_verified = $true',
        'backup_absent_verified = $true',
        'Read-Qs3dAllowedValue',
        'QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1',
        '$failurePhases = @(',
        '$failureCodes = @(',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new',
        'Curtain P05 FAIL marker contains a non-contract field.',
        '$diagnosticFailure = $true',
        'if ($diagnosticFailure)',
        'cleanup was verified.',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_STALE_REBUILD_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_STALE_REBUILD_NONCE"',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P05 runner missing contract token: " + token)
    ordered_commands = (
        '"QS3DCURTAINSTALEBASELINE"', '"QS3DCURTAINSTALEMUTATEGRID"', '"QS3DCURTAINSTALEVERIFYGRID"',
        '"QS3DCURTAINSTALEMUTATEDEPTH"', '"QS3DCURTAINSTALEVERIFYDEPTH"',
        '"QS3DCURTAINSTALEMUTATEHEIGHT"', '"QS3DCURTAINSTALEVERIFYHEIGHT"',
        '"QS3DCURTAINSTALEMUTATEOPENING"', '"QS3DCURTAINSTALEVERIFYOPENING"',
        '"QS3DCURTAINSTALEMUTATESOURCE"', '"QS3DSYNCSOURCE"', '"QS3DCURTAINSTALEAFTERSYNC"',
        '"QS3DCURTAINSTALEPROBE"',
    )
    positions = [text.find(token) for token in ordered_commands]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Curtain-panel P05 runner command state machine is not in canonical order")
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Curtain-panel P05 runner contains broad process/window action: " + forbidden)
    fail_start = text.find('if ($marker.ContainsKey("status")')
    fail_end = text.find("Stop-Qs3dLaunchedProcess -Process $process", fail_start)
    fail_branch = text[fail_start:fail_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", "GetType("):
        if forbidden in fail_branch:
            errors.append("Curtain-panel P05 runner FAIL branch exposes exception detail: " + forbidden)
    deferred_failure = text.find("if ($diagnosticFailure)", fail_start)
    drawing_hash_after = text.find("$drawingHashAfter =", fail_start)
    backup_after = text.find("Curtain P05 runtime probe persisted an unexpected sidecar or backup.", fail_start)
    if deferred_failure < 0 or drawing_hash_after < 0 or backup_after < 0 or deferred_failure < drawing_hash_after or deferred_failure < backup_after:
        errors.append("Curtain-panel P05 sanitized FAIL must be deferred until process/script/DWG/sidecar/backup cleanup checks finish")
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Curtain-panel P05 process cleanup must fail visible: " + forbidden)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata.lower():
            errors.append("Curtain-panel P05 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in (
        "LOCAL-002", "P05", "PENDING_LOCAL", "QS3DCURTAINSTALE",
        "test-bricscad-v25-curtain-panel-stale-rebuild.ps1", "legacy/no-Level",
        "QS3DSYNCSOURCE", "CURTAIN_PANEL_GENERATED_STALE",
        "CURTAIN_PANEL_LIVE_GEOMETRY_STALE", "source edit", "unrelated control",
        "QS3D_CURTAIN_PANEL_STALE_REBUILD_RUNTIME_V1",
    ):
        if token not in text:
            errors.append("Curtain-panel runbook missing P05 handoff token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P05", "No BricsCAD", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P05 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P05 claim must remain ACTIVE during implementation or be COMPLETED at close-out")

print("QS3D Curtain-panel P05 stale/rebuild runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P05 automation separates semantic owner stale from raw live-source drift, proves five target-only replacements against authoritative native plans while preserving one unrelated owner, and enforces exact-SHA/privacy/cleanup without claiming BricsCAD runtime evidence.")
