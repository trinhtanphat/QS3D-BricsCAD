#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HOOK = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallBuildFailureInjection.cs"
ORCHESTRATOR = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs"
PROBE = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelAtomicFailureRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-atomic-failures.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local-curtain-p08-six-phase-failure-injection.md"
errors = []

for path in (HOOK, ORCHESTRATOR, PROBE, RUNNER, HELPER, RUNBOOK, CLAIM):
    if not path.is_file(): errors.append("missing Curtain P08 file: " + str(path.relative_to(ROOT)))

phases = (
    "SemanticRegeneration", "LineHost", "PathHost", "LineFrame", "PathFrame", "LinePanel", "PathPanel"
)

if HOOK.is_file():
    text = HOOK.read_text(encoding="utf-8")
    for token in (
        "internal static class CurtainWallBuildFailureInjection", "private static readonly object Sync",
        "private static Ticket? Armed;", "private static Ticket? Consumed;",
        "internal static void Arm(string nonce, string phase)", "Guid.TryParseExact(nonce, \"N\"",
        "internal static void ThrowIfArmed(string phase)", "Consumed = Armed;", "Armed = null;",
        "internal static void RequireConsumed(string nonce, string phase)", "Consumed = null;",
        "internal static void RequireIdle()", "internal static void Clear(string nonce)",
    ):
        if token not in text: errors.append("P08 one-shot hook missing token: " + token)
    for phase in phases:
        if ('internal const string ' + phase + ' = ') not in text: errors.append("P08 hook missing allowlisted phase: " + phase)
    for forbidden in ("Environment.GetEnvironmentVariable", "Environment.SetEnvironmentVariable", "public static", "CommandMethod"):
        if forbidden in text: errors.append("P08 hook must stay internal and not directly environment-configurable: " + forbidden)

if ORCHESTRATOR.is_file():
    text = ORCHESTRATOR.read_text(encoding="utf-8")
    anchors = (
        ("RegenerateDirty(project);", "SemanticRegeneration"),
        ("WallSolidBuilder.BuildSelectedLineWalls", "LineHost"),
        ("PolylineWallSolidBuilder.BuildSelected", "PathHost"),
        ("CurtainWallFrameSolidBuilder.BuildSelectedLineWalls", "LineFrame"),
        ("CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines", "PathFrame"),
        ("CurtainWallPanelSolidBuilder.BuildSelectedLineWalls", "LinePanel"),
        ("CurtainWallPathPanelSolidBuilder.BuildSelectedOpenPolylines", "PathPanel"),
    )
    for anchor, phase in anchors:
        build = text.find(anchor)
        injection = text.find("CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection." + phase + ")", build)
        if build < 0 or injection < build: errors.append("P08 orchestrator injection must follow completed phase: " + phase)
    first_native = text.find("using (var commandTransaction")
    commit = text.find("commandTransaction.Commit();", first_native)
    last_injection = text.find("CurtainWallBuildFailureInjection.ThrowIfArmed(CurtainWallBuildFailureInjection.PathPanel)")
    if min(first_native, commit, last_injection) < 0 or not first_native < last_injection < commit:
        errors.append("P08 six native injection points must remain inside the outer transaction before commit")
    for token in ("rollback = ProjectStateSnapshot.Capture(project);", "if (!nativeCommitted && rollback != null && project != null)", "rollback.Restore(project);"):
        if token not in text: errors.append("P08 orchestrator rollback boundary missing: " + token)

if PROBE.is_file():
    text = PROBE.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINP08SEEDLINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08PREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08BASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08ARM", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08VERIFY", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08VALID", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP08PROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_P08_RESULT"', 'NonceVariable = "QS3D_CURTAIN_PANEL_P08_NONCE"',
        'CurtainWallBuildFailureInjection.Arm(nonce, phase);',
        'CurtainWallBuildFailureInjection.RequireConsumed(nonce, attempt.Phase);',
        'CurtainWallBuildFailureInjection.RequireIdle();',
        'CurtainWallBuildFailureInjection.Clear(nonce);',
        'state.BaselineSnapshot = ProjectStateSnapshot.Capture(project);', 'RestoreBaseline(project, state);',
        'CaptureProjectDigest(project)', 'CaptureNativeDigest(document)', 'entity.GeometricExtents',
        'semantic_regeneration_rollback=true', 'line_host_rollback=true', 'path_host_rollback=true',
        'line_frame_rollback=true', 'path_frame_rollback=true', 'line_panel_rollback=true', 'path_panel_rollback=true',
        'whole_batch_native_preserved=true', 'whole_batch_semantic_preserved=true',
        'valid_replacement_succeeded=true', 'schema=QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1',
        'qualification_boundary=LOCAL_002_P08_ONLY', 'production_local002_qualified=false',
        'error_code=CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_FAILED', 'FileMode.CreateNew', 'File.Move(tempPath, fullPath)',
    ):
        if token not in text: errors.append("P08 probe missing contract token: " + token)
    for forbidden in (
        "WallSolidBuilder.BuildSelected", "PolylineWallSolidBuilder.BuildSelected",
        "CurtainWallFrameSolidBuilder.BuildSelected", "CurtainWallPathFrameSolidBuilder.BuildSelected",
        "CurtainWallPanelSolidBuilder.BuildSelected", "CurtainWallPathPanelSolidBuilder.BuildSelected",
        "/ 1000d", "* 1000d",
    ):
        if forbidden in text: errors.append("P08 probe duplicates production or hardcodes units: " + forbidden)
    pass_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    pass_end = text.find("document.Editor.WriteMessage", pass_start)
    for forbidden in ("ElementId", "ProjectId", "DrawingPath", ".Message", ".StackTrace"):
        if forbidden in text[pass_start:pass_end]: errors.append("P08 PASS marker exposes identity/detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopy', '[string]::IsNullOrWhiteSpace($Profile)',
        '*.curtain-atomic-failure-probe-copy.dwg', 'QS3D_CURTAIN_PANEL_P08_RESULT', 'QS3D_CURTAIN_PANEL_P08_NONCE',
        '. $windowInteropPath', 'Close-Qs3dProxyInformationDialog -Process $process',
        'Start-Process -FilePath $bricscadExe', '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.', 'ArtifactDir must stay outside the repository.',
        'rev-parse HEAD', 'status --porcelain --untracked-files=normal', 'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process', 'function Remove-Qs3dDrawingLocks',
        '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl")', '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")',
        'Remove-Qs3dDrawingLocks -Paths $drawingLocks', 'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'drawing_copy_sha256_before', 'drawing_copy_sha256_after', 'process_cleanup_verified = $true',
        'script_cleanup_verified = $true', 'drawing_lock_cleanup_verified = $true',
        'sidecar_absent_verified = $true', 'backup_absent_verified = $true',
        'QS3D_CURTAIN_PANEL_ATOMIC_FAILURE_RUNTIME_V1', '$diagnosticFailure = $true',
        'if ($diagnosticFailure)', 'cleanup was verified.',
    ):
        if token not in text: errors.append("P08 runner missing contract token: " + token)
    script_start = text.find("$script = @(")
    script_end = text.find("Set-Content -LiteralPath $scriptPath", script_start)
    script = text[script_start:script_end]
    for token in ('"QS3DCURTAINP08SEEDLINE"', '"QS3DGLASSWALL"', '"QS3DDRAWGLASSWALL"', '"QS3DCURTAINP08PREPARE"', '"QS3DCURTAINP08BASELINE"', '"QS3DCURTAINP08VALID"', '"QS3DCURTAINP08PROBE"'):
        if token not in script: errors.append("P08 runner missing state-machine command: " + token)
    if script.count('"QS3DCURTAINP08ARM"') != 7 or script.count('"QS3DCURTAINP08VERIFY"') != 7:
        errors.append("P08 runner must arm and verify exactly seven failure phases")
    if script.count('"QS3DCURTAIN3D"') != 9:
        errors.append("P08 runner must invoke production QS3DCURTAIN3D for baseline, seven failures and valid control")
    cursor = 0
    for _ in range(7):
        arm = script.find('"QS3DCURTAINP08ARM"', cursor)
        product = script.find('"QS3DCURTAIN3D"', arm)
        verify = script.find('"QS3DCURTAINP08VERIFY"', product)
        if min(arm, product, verify) < 0 or not arm < product < verify: errors.append("P08 runner failure triplet ordering is invalid"); break
        cursor = verify + 1
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end].lower()
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata: errors.append("P08 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("P08", "PENDING_LOCAL", "semantic regeneration", "six host/frame/panel phases"):
        if token not in text: errors.append("Curtain runbook missing P08 boundary: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text: errors.append("P08 claim must be ACTIVE or COMPLETED")

print("QS3D Curtain-panel P08 atomic-failure runtime probe preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors)); sys.exit(1)
print("PASS: P08 uses an internal one-shot seven-phase seam only after completed production phases, drives the real mixed-source Curtain command, verifies exact semantic/native rollback, and keeps exact-SHA privacy/cleanup guarded without claiming licensed evidence.")
