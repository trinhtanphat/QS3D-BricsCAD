#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelBudgetProvenanceRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-budget-provenance.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
ORCHESTRATOR = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs"
LINE_BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs"
SUPPORT = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local-curtain-p07-budget-provenance-rollback.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, ORCHESTRATOR, LINE_BUILDER, SUPPORT, RUNBOOK, CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P07 file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINP07SEEDHOSTS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07SEEDOPENINGS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07PREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07BASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07BUDGET", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07CHECKBUDGET", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07MISSING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07CHECKMISSING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07OFFHOST", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07CHECKOFFHOST", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07VALID", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP07PROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_P07_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_P07_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P07_ONLY',
        'production_local002_qualified=false',
        'later.SetProperty("CurtainMaxPanelWidthM", "0.07")',
        'later.SetProperty("CurtainMaxPanelHeightM", "0.06")',
        'new HostLinkService().LinkOpening(project, state.OnHostOpeningId, state.LaterId)',
        'opening.SourceHandles.Clear();',
        'new HostLinkService().LinkOpening(project, state.OffHostOpeningId, state.LaterId)',
        'state.BaselineSnapshot = ProjectStateSnapshot.Capture(project);',
        'snapshot.Restore(project);',
        'CaptureProjectDigest(project)',
        'CaptureNativeDigest(document)',
        'entity.GeometricExtents',
        'panel_budget_refused=true',
        'missing_source_refused=true',
        'off_host_refused=true',
        'later_element_failure_verified=true',
        'whole_batch_native_preserved=true',
        'whole_batch_semantic_preserved=true',
        'valid_replacement_succeeded=true',
        'CadHandleService.Resolve(document, firstOld.Concat(laterOld)).Count != 0',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'error_code=CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_FAILED',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P07 command missing contract token: " + token)
    for forbidden in (
        'CurtainWallPanelSolidBuilder.BuildSelected',
        'CurtainWallPathPanelSolidBuilder.BuildSelected',
        'WallSolidBuilder.BuildSelected',
        'GeneratedCurtainPanelOwnershipGuard.Build(',
        '/ 1000d', '* 1000d',
    ):
        if forbidden in text:
            errors.append("Curtain-panel P07 probe duplicates production/build or hardcodes drawing units: " + forbidden)
    pass_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    pass_end = text.find("document.Editor.WriteMessage", pass_start)
    pass_marker = text[pass_start:pass_end]
    for forbidden in ("ElementId", "ProjectId", "DrawingPath", ".Message", ".StackTrace"):
        if forbidden in pass_marker:
            errors.append("Curtain-panel P07 PASS marker exposes identity/detail: " + forbidden)
    fail_start = text.find("private static void TryWriteFailure")
    fail_end = text.find("private static void WriteMarkerAtomic", fail_start)
    failure_marker = text[fail_start:fail_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P07 FAIL marker exposes exception detail: " + forbidden)

if ORCHESTRATOR.is_file() and LINE_BUILDER.is_file() and SUPPORT.is_file():
    command = ORCHESTRATOR.read_text(encoding="utf-8")
    builder = LINE_BUILDER.read_text(encoding="utf-8")
    support = SUPPORT.read_text(encoding="utf-8")
    for token in (
        'rollback = ProjectStateSnapshot.Capture(project);',
        'using (var commandTransaction = document.Database.TransactionManager.StartTransaction())',
        'linePanels = CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(document, project);',
        'commandTransaction.Commit();',
        'if (!nativeCommitted && rollback != null && project != null)',
        'rollback.Restore(project);',
    ):
        if token not in command:
            errors.append("Curtain P07 production outer rollback contract missing: " + token)
    for token in (
        'private const int MaxPanelsPerElement = 4096;',
        'detail.Panels.Count > MaxPanelsPerElement',
        'CurtainWallOpeningPanelPlanner.Plan(detail.Panels, openings, 0d)',
        'CurtainWallPanelBuilderSupport.ValidatePrevious(',
        'CurtainWallPanelBuilderSupport.ErasePrevious(',
    ):
        if token not in builder:
            errors.append("Curtain P07 production budget/replacement contract missing: " + token)
    for token in (
        'requires exactly one live CAD source for panel clipping.',
        'is too far from the GlassWall centerline for safe panel clipping.',
        'CadVerticalPlacementResolver.ResolveHostedOpening(',
    ):
        if token not in support:
            errors.append("Curtain P07 production provenance contract missing: " + token)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy', '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-budget-provenance-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_P07_RESULT', 'QS3D_CURTAIN_PANEL_P07_NONCE',
        '. $windowInteropPath', 'Close-Qs3dProxyInformationDialog -Process $process',
        'Start-Process -FilePath $bricscadExe', '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository.', 'rev-parse HEAD',
        'status --porcelain --untracked-files=normal', 'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process', 'function Remove-Qs3dDrawingLocks',
        '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl")', '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")',
        'Remove-Qs3dDrawingLocks -Paths $drawingLocks',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'drawing_copy_sha256_before', 'drawing_copy_sha256_after',
        'process_cleanup_verified = $true', 'script_cleanup_verified = $true',
        'drawing_lock_cleanup_verified = $true', 'sidecar_absent_verified = $true', 'backup_absent_verified = $true',
        'QS3D_CURTAIN_PANEL_BUDGET_PROVENANCE_RUNTIME_V1',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new', '$diagnosticFailure = $true',
        'if ($diagnosticFailure)', 'cleanup was verified.',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P07 runner missing contract token: " + token)
    ordered = (
        '"QS3DCURTAINP07SEEDHOSTS"', '"QS3DGLASSWALL"', '"QS3DCURTAINP07SEEDOPENINGS"', '"QS3DDOOR"',
        '"QS3DCURTAINP07PREPARE"', '"QS3DCURTAIN3D"', '"QS3DCURTAINP07BASELINE"',
        '"QS3DCURTAINP07BUDGET"', '"QS3DCURTAINP07CHECKBUDGET"',
        '"QS3DCURTAINP07MISSING"', '"QS3DCURTAINP07CHECKMISSING"',
        '"QS3DCURTAINP07OFFHOST"', '"QS3DCURTAINP07CHECKOFFHOST"',
        '"QS3DCURTAINP07VALID"', '"QS3DCURTAINP07PROBE"',
    )
    positions = [text.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Curtain-panel P07 runner command state machine is not in canonical order")
    script_start = text.find("$script = @(")
    script_end = text.find("Set-Content -LiteralPath $scriptPath", script_start)
    script = text[script_start:script_end]
    if script.count('"QS3DCURTAIN3D"') != 5:
        errors.append("Curtain-panel P07 runner must invoke production QS3DCURTAIN3D for baseline, three refusals, and valid control")
    for prepare, verify in (
        ('"QS3DCURTAINP07BUDGET"', '"QS3DCURTAINP07CHECKBUDGET"'),
        ('"QS3DCURTAINP07MISSING"', '"QS3DCURTAINP07CHECKMISSING"'),
        ('"QS3DCURTAINP07OFFHOST"', '"QS3DCURTAINP07CHECKOFFHOST"'),
        ('"QS3DCURTAINP07VALID"', '"QS3DCURTAINP07PROBE"'),
    ):
        left = script.find(prepare)
        product = script.find('"QS3DCURTAIN3D"', left)
        right = script.find(verify, product)
        if min(left, product, right) < 0 or not left < product < right:
            errors.append("Curtain-panel P07 runner must place production command between " + prepare + " and " + verify)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end].lower()
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata:
            errors.append("Curtain-panel P07 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("P07", "PENDING_LOCAL", "panel/fragment budget exceeded", "malformed/off-host opening provenance"):
        if token not in text:
            errors.append("Curtain-panel runbook missing P07 boundary token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P07", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P07 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P07 claim must be ACTIVE or COMPLETED")

print("QS3D Curtain-panel P07 budget/provenance rollback runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P07 automation proves later-element budget and opening-provenance failures preserve the whole selected batch, then requires a valid production replacement; exact-SHA privacy/cleanup remain guarded without claiming licensed runtime evidence.")
