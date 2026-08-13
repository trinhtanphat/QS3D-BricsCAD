#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelOwnershipFailureRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-ownership-failures.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
LINE_BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs"
SUPPORT = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local-curtain-p06-ownership-failures.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, LINE_BUILDER, SUPPORT, RUNBOOK, CLAIM):
    if not path.is_file():
        errors.append("missing Curtain-panel P06 ownership file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DCURTAINP06SEED", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06PREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06BASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06MISSING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CHECKMISSING", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06DUPLICATE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CHECKDUPLICATE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06FOREIGN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CHECKFOREIGN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CROSS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CHECKCROSS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06CLEARCROSS", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06VALID", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP06PROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_OWNERSHIP_RESULT"',
        'NonceVariable = "QS3D_CURTAIN_PANEL_OWNERSHIP_NONCE"',
        'schema=QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P06_ONLY',
        'production_local002_qualified=false',
        'private const int OwnerCount = 6;',
        'solid.Erase();',
        'var alias = "0" + canonical;',
        'CadHandleService.NormalizeHexHandle(alias)',
        'foreign.CreateBox(',
        'owner.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = string.Join(";", handles);',
        'private const string CrossOwnerSlot = "GeneratedRebarHandles";',
        'claimant.Properties[CrossOwnerSlot] = claimedHandle;',
        'claimant.Properties.Remove(CrossOwnerSlot)',
        'GeneratedCurtainPanelNativeOwnershipService.HasMatchingOwnership(solid, project, owner)',
        'CaptureProjectDigest(project)',
        'CurrentSpaceHandles(document)',
        'SequenceEqual(attempt.CurrentSpaceHandles, StringComparer.OrdinalIgnoreCase)',
        'SequenceEqual(attempt.LiveOriginalPanelHandles, StringComparer.OrdinalIgnoreCase)',
        'CaptureOwnerDigest(owner)',
        'next.PanelHandles.Intersect(state.ValidOldPanels',
        'CadHandleService.Resolve(document, state.ValidOldPanels).Count != 0',
        'missing_handle_refused=true',
        'duplicate_canonical_refused=true',
        'foreign_unmarked_refused=true',
        'cross_owner_refused=true',
        'no_erase_append_verified=true',
        'semantic_metadata_preserved=true',
        'valid_replacement_succeeded=true',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
        'error_code=CURTAIN_PANEL_OWNERSHIP_RUNTIME_FAILED',
        '"failure_phase=" + phase',
        '"failure_code=" + failureCode',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P06 command missing contract token: " + token)
    for forbidden in (
        'CurtainWallPanelSolidBuilder.BuildSelected',
        'CurtainWallPathPanelSolidBuilder.BuildSelected',
        'WallSolidBuilder.BuildSelected',
        'GeneratedCurtainPanelOwnershipGuard.Build(',
        '/ 1000d', '* 1000d',
    ):
        if forbidden in text:
            errors.append("Curtain-panel P06 probe duplicates production/build or hardcodes drawing units: " + forbidden)
    pass_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    pass_end = text.find("document.Editor.WriteMessage", pass_start)
    pass_marker = text[pass_start:pass_end]
    for forbidden in ("SpecialHandle", ".ElementId", "ProjectId", "DrawingPath", ".Message", ".StackTrace"):
        if forbidden in pass_marker:
            errors.append("Curtain-panel P06 PASS marker exposes identity/detail expression: " + forbidden)
    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure_marker = text[failure_start:failure_end]
    for forbidden in (".Message", ".StackTrace", ".InnerException", ".GetType("):
        if forbidden in failure_marker:
            errors.append("Curtain-panel P06 FAIL marker exposes exception detail: " + forbidden)

if LINE_BUILDER.is_file() and SUPPORT.is_file():
    builder = LINE_BUILDER.read_text(encoding="utf-8")
    support = SUPPORT.read_text(encoding="utf-8")
    validate = builder.find("CurtainWallPanelBuilderSupport.ValidatePrevious(")
    erase = builder.find("CurtainWallPanelBuilderSupport.ErasePrevious(", validate)
    append = builder.find("modelSpace.AppendEntity(solid)", erase)
    if min(validate, erase, append) < 0 or not validate < erase < append:
        errors.append("production LINE panel replacement must validate before erase before append")
    for token in (
        "Generated curtain panel metadata contains a duplicate handle:",
        "Generated curtain panel live-handle set is incomplete.",
        "GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership",
        "GeneratedCurtainPanelOwnershipGuard.OwnershipIndex ownership",
    ):
        if token not in support:
            errors.append("production panel replacement missing fail-closed P06 token: " + token)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopy',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-ownership-probe-copy.dwg',
        'QS3D_CURTAIN_PANEL_OWNERSHIP_RESULT',
        'QS3D_CURTAIN_PANEL_OWNERSHIP_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository.',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Curtain P06 runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Get-Process -Name "bricscad" -ErrorAction SilentlyContinue',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'sidecar_absent_verified = $true',
        'backup_absent_verified = $true',
        'Read-Qs3dAllowedValue',
        'QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1',
        '$failureKeys = [Collections.Generic.HashSet[string]]::new',
        'Curtain P06 FAIL marker contains a non-contract field.',
        '$diagnosticFailure = $true',
        'if ($diagnosticFailure)',
        'cleanup was verified.',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OWNERSHIP_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_CURTAIN_PANEL_OWNERSHIP_NONCE"',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain-panel P06 runner missing contract token: " + token)
    ordered = (
        '"QS3DCURTAINP06SEED"', '"QS3DGLASSWALL"', '"QS3DCURTAINP06PREPARE"',
        '"QS3DCURTAIN3D"', '"QS3DCURTAINP06BASELINE"', '"QS3DCURTAINP06MISSING"',
        '"QS3DCURTAINP06CHECKMISSING"', '"QS3DCURTAINP06DUPLICATE"',
        '"QS3DCURTAINP06CHECKDUPLICATE"', '"QS3DCURTAINP06FOREIGN"',
        '"QS3DCURTAINP06CHECKFOREIGN"', '"QS3DCURTAINP06CROSS"',
        '"QS3DCURTAINP06CHECKCROSS"', '"QS3DCURTAINP06CLEARCROSS"',
        '"QS3DCURTAINP06VALID"', '"QS3DCURTAINP06PROBE"',
    )
    positions = [text.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Curtain-panel P06 runner command state machine is not in canonical order")
    script_start = text.find("$script = @(")
    script_end = text.find("Set-Content -LiteralPath $scriptPath", script_start)
    script = text[script_start:script_end]
    if script.count('"QS3DCURTAIN3D"') != 6:
        errors.append("Curtain-panel P06 runner must invoke production QS3DCURTAIN3D once for baseline, four refusals, and one valid control")
    for mutate, verify in (
        ('"QS3DCURTAINP06MISSING"', '"QS3DCURTAINP06CHECKMISSING"'),
        ('"QS3DCURTAINP06DUPLICATE"', '"QS3DCURTAINP06CHECKDUPLICATE"'),
        ('"QS3DCURTAINP06FOREIGN"', '"QS3DCURTAINP06CHECKFOREIGN"'),
        ('"QS3DCURTAINP06CROSS"', '"QS3DCURTAINP06CHECKCROSS"'),
        ('"QS3DCURTAINP06VALID"', '"QS3DCURTAINP06PROBE"'),
    ):
        left = script.find(mutate)
        product = script.find('"QS3DCURTAIN3D"', left)
        right = script.find(verify, product)
        if min(left, product, right) < 0 or not left < product < right:
            errors.append("Curtain-panel P06 runner must place production QS3DCURTAIN3D between " + mutate + " and " + verify)
    fail_start = text.find('if ($marker.ContainsKey("status")')
    deferred_failure = text.find("if ($diagnosticFailure)", fail_start)
    drawing_hash_after = text.find("$drawingHashAfter =", fail_start)
    backup_after = text.find("Curtain P06 runtime probe persisted an unexpected sidecar or backup.", fail_start)
    if min(deferred_failure, drawing_hash_after, backup_after) < 0 or deferred_failure < drawing_hash_after or deferred_failure < backup_after:
        errors.append("Curtain-panel P06 sanitized FAIL must be deferred until process/script/DWG/sidecar/backup checks finish")
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Curtain-panel P06 process cleanup must fail visible: " + forbidden)
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata.lower():
            errors.append("Curtain-panel P06 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in (
        "LOCAL-002", "P06", "PENDING_LOCAL", "QS3DCURTAINP06",
        "test-bricscad-v25-curtain-panel-ownership-failures.ps1",
        "one missing", "duplicate canonical", "foreign", "cross-owner",
        "QS3D_CURTAIN_PANEL_OWNERSHIP_RUNTIME_V1",
    ):
        if token not in text:
            errors.append("Curtain-panel runbook missing P06 handoff token: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P06", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Curtain-panel P06 claim missing boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Curtain-panel P06 claim must remain ACTIVE during implementation or be COMPLETED at close-out")

print("QS3D Curtain-panel P06 ownership-failure runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-002/P06 automation drives four production replacement refusals plus one valid control, verifies exact semantic/native preservation, and enforces exact-SHA privacy and cleanup without claiming licensed runtime evidence.")
