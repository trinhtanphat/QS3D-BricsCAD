#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelBulgedPathRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-bulged-path.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local002-p04-curtain-bulged-path-runtime-probe.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, RUNBOOK, CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P04 bulged-path probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINBULGEDSEED", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINBULGEDPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINBULGEDPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_BULGED_PATH_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_BULGED_PATH_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P04_ONLY',
        'production_local002_qualified=false',
        'polyline.AddVertexAt(0, new Point2d(0d, 0d), 1d, 0d, 0d)',
        'CadGeometryGuard.ToDrawingUnits(document, 4d',
        'CadGeometryGuard.ToDrawingUnits(document, 7d',
        'polyline.Normal = Vector3d.ZAxis',
        'document.Editor.SetImpliedSelection(new[] { sourceId })',
        'ExistingProjectMutationContext.Require(document, "Curtain P04 runtime sagitta configuration")',
        'ProjectStateSnapshot.Capture(project)',
        'project.Metadata["WallArcSagittaM"] = ConfiguredSagittaM.ToString("R", CultureInfo.InvariantCulture)',
        'rollback.Restore(project)',
        'RequireLegacyNoLevel(hosts[0])',
        'polyline.GetBulgeAt(0), 1d',
        'new Point2(0d, 0d), new Point2(4d, 0d), new Point2(7d, 0d)',
        'IndependentExpectedSegmentCount(Math.PI, ArcRadiusM, sagittaM)',
        'var maximumAngle = Math.PI / 18d',
        'var bySagitta = 4d * Math.Asin(quarterSine)',
        'Math.Ceiling(includedAngle / segmentAngle)',
        'curvedSegmentCount != 50',
        'centerline.Count != curvedSegmentCount + 2',
        'chordSagitta > sagittaM + GeometryToleranceM',
        'CadPolylinePathReader.ReadOpenWcsXy(',
        'CurtainWallDetailPlanner.Plan(',
        'CurtainPathFramePlanner.Plan(centerline, rectangles)',
        'pathPlan.Pieces.Count > MaximumPanelPieces',
        'curvedSegmentsWithPanels.Count <= 1 || straightPieceCount == 0',
        'GeneratedCurtainPanelMode", "PathPanelSolids"',
        'GeneratedCurtainPanelSourceKind", "OpenPolyline"',
        'GeneratedCurtainPanelOpeningCount", 0',
        'GeneratedCurtainPanelPathSegmentCount", pathPlan.PathSegmentCount',
        'solid.GeometricExtents',
        'MatchNativePieces(native, pathPlan.Pieces, depthM, baseM)',
        'GeneratedCurtainPanelHealthService().Inspect(project, livePanels)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'SemanticSelectionResolver.ResolveImplied(document, project)',
        'tessellated_fragments_only=true',
        'exact_swept_curve_qualified=false',
        'maximum_chord_sagitta_within_limit=true',
        'budget_within_limits=true',
        'source_geometry_preserved=true',
        'ownership_sets_disjoint=true',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'private static readonly HashSet<string> FailurePhases',
        'private static readonly HashSet<string> FailureCodes',
        'TryWriteFailure(requestedPath, nonce, "SEED_SOURCE", FailureCode(error))',
        'TryWriteFailure(requestedPath, nonce, "CONFIGURE_SOURCE", FailureCode(error))',
        'TryWriteFailure(requestedPath, nonce, phase.Value, FailureCode(error))',
        'error_code=CURTAIN_PANEL_BULGED_PATH_RUNTIME_FAILED',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P04 command missing contract token: " + token)
    marker_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    marker_end = text.find("document.Editor.WriteMessage", marker_start)
    marker = text[marker_start:marker_end]
    for forbidden in (
        "handle=", "handles=", "element_id=", "project_id=", "drawing_path=",
        "plugin_path=", "layer=", "family_name=", "source_id=", "host_id=",
    ):
        if forbidden in marker.lower():
            errors.append("Curtain-panel P04 marker leaks identity field: " + forbidden)
    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure_marker = text[failure_start:failure_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P04 FAIL marker exposes exception detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-bulged-path-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_BULGED_PATH_RESULT',
        'QS3D_CURTAIN_PANEL_BULGED_PATH_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DCURTAINBULGEDSEED"',
        '"QS3DGLASSWALL"',
        '"QS3DCURTAINBULGEDPREPARE"',
        '"QS3DCURTAIN3D"',
        '"QS3DCURTAINBULGEDPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository.',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Curtain bulged-path runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Launched BricsCAD Curtain bulged-path process did not exit.',
        'Get-Process -Name "bricscad" -ErrorAction SilentlyContinue',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'Curtain bulged-path runtime script cleanup failed.',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'sidecar_absent_verified = $true',
        'backup_absent_verified = $true',
        'Read-Qs3dAllowedValue',
        'QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1',
        '$failurePhases = @(',
        '$failureCodes = @(',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new',
        'Curtain bulged-path FAIL marker contains a non-contract field.',
        '$diagnosticFailure = $true',
        'if ($diagnosticFailure)',
        'cleanup was verified.',
        'curved_tessellation_segment_count" -Expected "50"',
        'tessellated_path_segment_count" -Expected "51"',
        '$curvedSegmentsWithPanels -le 1',
        '$curvedPieceCount + $straightPieceCount -ne $pathPieceCount',
        '$pathPieceCount -gt 4096',
        '$nativeMatchCount -ne $pathPieceCount',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_BULGED_PATH_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_BULGED_PATH_NONCE"',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P04 runner missing contract token: " + token)
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Curtain-panel P04 runner contains broad process/window action: " + forbidden)
    fail_start = text.find('if ($marker.ContainsKey("status")')
    fail_end = text.find("Stop-Qs3dLaunchedProcess -Process $process", fail_start)
    fail_branch = text[fail_start:fail_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", "GetType("):
        if forbidden in fail_branch:
            errors.append("Curtain-panel P04 runner FAIL branch exposes exception detail: " + forbidden)
    deferred_failure = text.find("if ($diagnosticFailure)", fail_start)
    drawing_hash_after = text.find("$drawingHashAfter =", fail_start)
    backup_after = text.find("Curtain bulged-path runtime probe persisted an unexpected sidecar or backup.", fail_start)
    if deferred_failure < 0 or drawing_hash_after < 0 or backup_after < 0 or deferred_failure < drawing_hash_after or deferred_failure < backup_after:
        errors.append("Curtain-panel P04 sanitized FAIL must be deferred until process/script/DWG/sidecar/backup cleanup checks finish")
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Curtain-panel P04 process cleanup must fail visible: " + forbidden)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata.lower():
            errors.append("Curtain-panel P04 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in (
        "LOCAL-002", "P04", "PENDING_LOCAL", "QS3DCURTAINBULGEDPROBE",
        "test-bricscad-v25-curtain-panel-bulged-path.ps1", "legacy/no-Level",
        "bulged WCS-XY POLYLINE", "QS3D_CURTAIN_PANEL_BULGED_PATH_RUNTIME_V1",
        "tessellated straight prisms", "not exact swept-curve",
    ):
        if token not in text:
            errors.append("Curtain-panel runbook missing P04 handoff token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P04", "No BricsCAD", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P04 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P04 claim must remain ACTIVE during implementation or be COMPLETED at close-out")

print("QS3D Curtain-panel P04 bulged-path runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P04 automation seeds a synthetic legacy/no-Level open bulged WCS-XY POLYLINE, independently bounds the 1 mm sagitta tessellation, matches native prism AABBs to the authoritative path-piece plan, and enforces exact-SHA/privacy/cleanup without claiming runtime evidence or exact swept-curve geometry.")
