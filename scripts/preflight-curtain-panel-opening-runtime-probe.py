#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelOpeningRuntimeProbeCommands.cs"
PANEL_SUPPORT = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-openings.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local-curtain-p02-opening-probe.md"
DIAGNOSTIC_CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local-curtain-p02-failure-diagnostics.md"
CENTERED_BOX_CLAIM = ROOT / "docs/agent-work-claims/2026-08-12-codex-local-curtain-panel-centered-box-placement.md"
errors = []

for path in (COMMAND, PANEL_SUPPORT, RUNNER, HELPER, RUNBOOK, INBOX, CLAIM, DIAGNOSTIC_CLAIM, CENTERED_BOX_CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P02 probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINOPENINGPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINOPENINGPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_OPENING_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_OPENING_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V2',
        'qualification_boundary=LOCAL_002_P02_ONLY',
        'production_local002_qualified=false',
        'RequireLegacyNoLevel(host, "GlassWall")',
        'RequireLegacyNoLevel(opening, "opening")',
        'CurtainWallPanelBuilderSupport.ReadLineOpenings(',
        'CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d)',
        'PositiveIntersectionArea(',
        'ReadNativePieces(',
        'MatchNativePieces(native, plan.Pieces)',
        'solid.GeometricExtents',
        'GeneratedCurtainPanelCount',
        'GeneratedCurtainPanelAreaM2',
        'GeneratedCurtainPanelMode", "LinePanelSolids.OpeningAware"',
        'GeneratedCurtainPanelBuildState", "Complete"',
        'expectedHostY = category == ElementCategory.Door ? 0d : 10d',
        'expectedOpeningStartX = category == ElementCategory.Door ? 0.8d : 0.05d',
        'raw.Split(new[] { \';\' }, StringSplitOptions.None)',
        'GeneratedCurtainPanelHealthService().Inspect(project, livePanels)',
        'CurtainWallPanelLiveStateService.Inspect(document, project)',
        'GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)',
        'SemanticSelectionResolver.ResolveImplied(document, project)',
        'complete_empty_handle_count=0',
        'source_geometry_preserved=true',
        'ownership_sets_disjoint=true',
        'WriteMarkerAtomic(resultPath',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'error_code=CURTAIN_PANEL_OPENING_RUNTIME_FAILED',
        'private static readonly HashSet<string> FailurePhases',
        'private static readonly HashSet<string> FailureCodes',
        '"OUTPUT_DISCOVERY"',
        '"DOOR_PLAN_RECONSTRUCTION"',
        '"EMPTY_NATIVE_GEOMETRY"',
        '"OWNERSHIP_DISJOINT"',
        '"RESULT_PUBLISH"',
        '"STATE_REJECTED"',
        '"DATA_REJECTED"',
        '"IO_REJECTED"',
        '"OVERFLOW_REJECTED"',
        '"UNEXPECTED_REJECTED"',
        'phase.Set(prefix + "SOURCE_SHAPE")',
        'phase.Set(prefix + "PLAN_RECONSTRUCTION")',
        'phase.Set(prefix + "OUTPUT_OWNERSHIP")',
        'phase.Set(prefix + "METADATA")',
        'phase.Set(prefix + "PLANNED_GEOMETRY")',
        'phase.Set(prefix + "NATIVE_GEOMETRY")',
        'TryWriteFailure(requestedPath, nonce, phase.Value, FailureCode(error))',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P02 command missing contract token: " + token)
    marker_start = text.find('WriteMarkerAtomic(resultPath, new[]')
    marker_end = text.find('document.Editor.WriteMessage', marker_start)
    marker = text[marker_start:marker_end]
    for forbidden in (
        "handle=", "handles=", "element_id=", "project_id=", "drawing_path=",
        "plugin_path=", "layer=", "family_name=", "opening_id=", "host_id=",
    ):
        if forbidden in marker.lower():
            errors.append("Curtain-panel P02 marker leaks identity field: " + forbidden)
    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure_marker = text[failure_start:failure_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P02 FAIL marker exposes exception detail: " + forbidden)

if PANEL_SUPPORT.is_file():
    text = PANEL_SUPPORT.read_text(encoding="utf-8")
    box_start = text.find("private static Solid3d CreateBox(")
    box_end = text.find("private static double NonNegative(", box_start)
    box_body = text[box_start:box_end]
    placement = (
        "solid.CreateBox(width, depth, height);",
        "solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));",
        "solid.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, centerZ)));",
    )
    positions = [box_body.find(token) for token in placement]
    if box_start < 0 or box_end < 0 or any(position < 0 for position in positions):
        errors.append("Curtain-panel CreateBox helper must create, rotate, then place the centered solid")
    elif positions != sorted(positions):
        errors.append("Curtain-panel CreateBox helper placement order must remain create -> rotate -> target center")
    duplicate_half_shift = "solid.TransformBy(Matrix3d.Displacement(new Vector3d(-width / 2d, -depth / 2d, -height / 2d)));"
    if duplicate_half_shift in box_body:
        errors.append("Curtain-panel CreateBox helper duplicates V25 centered-box half-extent displacement")

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-opening-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_OPENING_RESULT',
        'QS3D_CURTAIN_PANEL_OPENING_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"QS3DDRAWGLASSWALL", "0,0", "5000,0", ""',
        '"QS3DDRAWGLASSWALL", "0,10000", "5000,10000", ""',
        '"QS3DDRAWDOOR", "800,0", "2200,0"',
        '"QS3DDRAWOPENINGADV", "50,10000", "4950,10000", "3.5", "0.05", "0.01"',
        '"QS3DCURTAINOPENINGPREPARE"',
        '"QS3DCURTAIN3D"',
        '"QS3DCURTAINOPENINGPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository',
        'Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Curtain-opening runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Launched BricsCAD Curtain-opening process did not exit.',
        'git_sha = $gitHead',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'sidecar_absent_verified = $true',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'Curtain-opening runtime script cleanup failed.',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'complete_empty_build_state',
        'partial_native_opening_intersection_count',
        'if ($partialNativeMatchCount -ne $partialOutputCount)',
        'if ($emptyFullyRemovedCount -ne $emptySourceCount)',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OPENING_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OPENING_NONCE"',
        'Read-Qs3dAllowedValue',
        'QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V2',
        '$failurePhases = @(',
        '$failureCodes = @(',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new',
        'Curtain-opening FAIL marker contains a non-contract field.',
        '$diagnosticFailure = $true',
        'if ($diagnosticFailure)',
        'Curtain-opening probe failed at sanitized phase',
        'process/script/sidecar/DWG cleanup was verified.',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P02 runner missing contract token: " + token)
    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Curtain-panel P02 runner contains broad process/window action: " + forbidden)
    fail_start = text.find('if ($marker.ContainsKey("status")')
    fail_end = text.find('Require-Qs3dValue -Marker $marker -Key "status" -Expected "PASS"', fail_start)
    fail_branch = text[fail_start:fail_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", "GetType("):
        if forbidden in fail_branch:
            errors.append("Curtain-panel P02 runner FAIL branch exposes exception detail: " + forbidden)
    deferred_failure = text.find("if ($diagnosticFailure)", fail_start)
    drawing_hash_after = text.find("$drawingHashAfter =", fail_start)
    sidecar_after = text.find("Curtain-opening runtime probe unexpectedly persisted a QS3D sidecar.", fail_start)
    if deferred_failure < 0 or drawing_hash_after < 0 or sidecar_after < 0 or deferred_failure < drawing_hash_after or deferred_failure < sidecar_after:
        errors.append("Curtain-panel P02 sanitized FAIL must be deferred until process/script/DWG/sidecar cleanup checks finish")
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Curtain-panel P02 process cleanup must fail visible: " + forbidden)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata.lower():
            errors.append("Curtain-panel P02 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in (
        "LOCAL-002", "P02", "PENDING_LOCAL", "QS3DCURTAINOPENINGPROBE",
        "test-bricscad-v25-curtain-panel-openings.ps1", "legacy/no-Level",
        "partial", "complete-empty", "QS3D_CURTAIN_PANEL_OPENING_RUNTIME_V2",
        "failure_phase", "failure_code", "af0aec7f",
    ):
        if token not in text:
            errors.append("Curtain-panel runbook missing P02 handoff token: " + token)

if INBOX.is_file():
    text = INBOX.read_text(encoding="utf-8")
    local002_start = text.find("## LOCAL-002")
    next_item = text.find("\n## LOCAL-", local002_start + 1)
    local002 = text[local002_start:next_item if next_item >= 0 else len(text)]
    for token in (
        "QS3DCURTAINOPENINGPROBE", "test-bricscad-v25-curtain-panel-openings.ps1",
        "legacy/no-Level", "partial", "complete-empty", "PENDING_LOCAL",
        "failure_phase", "failure_code", "af0aec7f",
    ):
        if token not in local002:
            errors.append("LOCAL-002 missing P02 runner/evidence token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P02", "No BricsCAD launch", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P02 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P02 claim must remain ACTIVE during implementation or be COMPLETED at close-out")

if DIAGNOSTIC_CLAIM.is_file():
    text = DIAGNOSTIC_CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P02", "sanitized", "No BricsCAD launch", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P02 diagnostic claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P02 diagnostic claim must remain ACTIVE during implementation or be COMPLETED at close-out")

if CENTERED_BOX_CLAIM.is_file():
    text = CENTERED_BOX_CLAIM.read_text(encoding="utf-8")
    for token in ("7c160de66de68c811282f4cd460e927370e454cd", "DOOR_NATIVE_GEOMETRY", "STATE_REJECTED", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel centered-box claim missing diagnostic boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel centered-box claim must remain ACTIVE during implementation or be COMPLETED at close-out")

print("QS3D Curtain-panel P02 opening-clipping runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P02 probe seeds only synthetic legacy/no-Level LINE scenarios, verifies centered native placement plus partial/full-cover clipping against authoritative Core plans and native bounds, emits allowlisted detail-free failure phase/class diagnostics, and enforces exact-SHA/privacy/cleanup without claiming BricsCAD runtime evidence.")
