#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HOOK = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPostCommitFailureInjection.cs"
ORCHESTRATOR = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs"
PROBE = ROOT / "src/QS3D.BricsCAD.V25/CurtainPanelPostCommitRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-curtain-panel-postcommit-warnings.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
RUNBOOK = ROOT / "docs/CURTAIN-NATIVE-PANELS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local-curtain-p09-postcommit-warning.md"
errors = []

for path in (HOOK, ORCHESTRATOR, PROBE, RUNNER, HELPER, RUNBOOK, INBOX, CLAIM):
    if not path.is_file(): errors.append("missing Curtain P09 file: " + str(path.relative_to(ROOT)))

if HOOK.is_file():
    text = HOOK.read_text(encoding="utf-8")
    for token in (
        "internal static class CurtainWallPostCommitFailureInjection", "internal const string LiveFingerprint",
        "internal const string UiRefresh", "private static readonly object Sync", "private static Ticket? Armed;",
        "private static Ticket? Consumed;", "internal static void Arm(string nonce, string phase)",
        "Guid.TryParseExact(nonce, \"N\"", "internal static void ThrowIfArmed(string phase)",
        "Consumed = Armed;", "Armed = null;", "internal static void RequireConsumed(string nonce, string phase)",
        "Consumed = null;", "internal static void RequireIdle()", "internal static void Clear(string nonce)",
    ):
        if token not in text: errors.append("P09 one-shot hook missing token: " + token)
    for forbidden in ("Environment.GetEnvironmentVariable", "Environment.SetEnvironmentVariable", "public static", "CommandMethod"):
        if forbidden in text: errors.append("P09 hook must stay internal and not directly environment-configurable: " + forbidden)

if ORCHESTRATOR.is_file():
    text = ORCHESTRATOR.read_text(encoding="utf-8")
    commit = text.find("commandTransaction.Commit();")
    committed = text.find("nativeCommitted = true;", commit)
    phase = text.find('phase = "live fingerprint stamp";', committed)
    live_hook = text.find("CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.LiveFingerprint);", phase)
    frame_stamp = text.find("CurtainWallFrameLiveStateService.TryStampSelected", live_hook)
    if min(commit, committed, phase, live_hook, frame_stamp) < 0 or not commit < committed < phase < live_hook < frame_stamp:
        errors.append("P09 live-fingerprint injection must remain after native commit and before either stamp")
    finalize = text.find("private static void FinalizeUi")
    try_pos = text.find("try", finalize)
    ui_hook = text.find("CurtainWallPostCommitFailureInjection.ThrowIfArmed(CurtainWallPostCommitFailureInjection.UiRefresh);", try_pos)
    refresh = text.find("PaletteCoordinator.RefreshProject();", ui_hook)
    catch = text.find("catch (Exception ex)", refresh)
    warning = text.find("UI sync warning:", catch)
    if min(finalize, try_pos, ui_hook, refresh, catch, warning) < 0 or not finalize < try_pos < ui_hook < refresh < catch < warning:
        errors.append("P09 UI injection must stay inside the best-effort FinalizeUi warning boundary")
    reporter = text.find("private static void ReportAtomicFailure")
    for token in ("if (!nativeCommitted)", "post-commit warning", "QS3DCURTAINFRAMEHEALTH/QS3DHEALTHALL"):
        if token not in text[reporter:finalize]: errors.append("P09 truthful post-commit warning missing: " + token)

if PROBE.is_file():
    text = PROBE.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DCURTAINP09SEED", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09PREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09BASELINE", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09ARMFINGERPRINT", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09VERIFYFINGERPRINT", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09PRECLEAN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09VERIFYCLEAN", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09ARMUI", CommandFlags.Modal)',
        'CommandMethod("QS3DCURTAINP09PROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_CURTAIN_PANEL_P09_RESULT"', 'NonceVariable = "QS3D_CURTAIN_PANEL_P09_NONCE"',
        "CurtainWallPostCommitFailureInjection.Arm(nonce, CurtainWallPostCommitFailureInjection.LiveFingerprint);",
        "CurtainWallPostCommitFailureInjection.RequireConsumed(nonce, CurtainWallPostCommitFailureInjection.LiveFingerprint);",
        "CurtainWallPostCommitFailureInjection.Arm(nonce, CurtainWallPostCommitFailureInjection.UiRefresh);",
        "CurtainWallPostCommitFailureInjection.RequireConsumed(nonce, CurtainWallPostCommitFailureInjection.UiRefresh);",
        '"CURTAIN_FRAME_LIVE_FINGERPRINT_MISSING"', '"CURTAIN_PANEL_LIVE_FINGERPRINT_MISSING"',
        "RequireReplacement(document", "CaptureSourceDigest(document", "GeneratedCurtainPanelRuntimeHealthService.Inspect",
        "fingerprint_failure_committed=true", "fingerprint_health_review_required=true", "ui_failure_committed=true",
        "ui_health_issue_count=0", "schema=QS3D_CURTAIN_PANEL_POSTCOMMIT_RUNTIME_V1",
        "qualification_boundary=LOCAL_002_P09_ONLY", "production_local002_qualified=false",
        "error_code=CURTAIN_PANEL_POSTCOMMIT_RUNTIME_FAILED", "FileMode.CreateNew", "File.Move(tempPath, fullPath)",
    ):
        if token not in text: errors.append("P09 probe missing contract token: " + token)
    for forbidden in (
        "WallSolidBuilder.BuildSelected", "PolylineWallSolidBuilder.BuildSelected",
        "CurtainWallFrameSolidBuilder.BuildSelected", "CurtainWallPathFrameSolidBuilder.BuildSelected",
        "CurtainWallPanelSolidBuilder.BuildSelected", "CurtainWallPathPanelSolidBuilder.BuildSelected",
        "/ 1000d", "* 1000d",
    ):
        if forbidden in text: errors.append("P09 probe duplicates production or hardcodes units: " + forbidden)
    pass_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    pass_end = text.find("document.Editor.WriteMessage", pass_start)
    for forbidden in ("ElementId", "ProjectId", "DrawingPath", ".Message", ".StackTrace", "SourceHandle"):
        if forbidden in text[pass_start:pass_end]: errors.append("P09 PASS marker exposes identity/detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        "[switch]$ConfirmDisposableCopy", "[string]::IsNullOrWhiteSpace($Profile)",
        "*.curtain-postcommit-probe-copy.dwg", "QS3D_CURTAIN_PANEL_P09_RESULT", "QS3D_CURTAIN_PANEL_P09_NONCE",
        '"PICKFIRST", "1"',
        ". $windowInteropPath", "Close-Qs3dProxyInformationDialog -Process $process",
        "Start-Process -FilePath $bricscadExe", "-WindowStyle Hidden",
        "PluginDll must be the exact repository x64 Release V25 build output.", "ArtifactDir must stay outside the repository.",
        "rev-parse HEAD", "status --porcelain --untracked-files=normal", "ArtifactDir must be empty.",
        "Stop-Qs3dLaunchedProcess -Process $process", "function Remove-Qs3dDrawingLocks",
        '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl")', '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")',
        "Remove-Qs3dDrawingLocks -Paths $drawingLocks", "Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop",
        "drawing_copy_sha256_before", "drawing_copy_sha256_after", "process_cleanup_verified = $true",
        "script_cleanup_verified = $true", "drawing_lock_cleanup_verified = $true",
        "sidecar_absent_verified = $true", "backup_absent_verified = $true",
        "QS3D_CURTAIN_PANEL_POSTCOMMIT_RUNTIME_V1", "$diagnosticFailure = $true", "if ($diagnosticFailure)",
        "cleanup was verified.",
    ):
        if token not in text: errors.append("P09 runner missing contract token: " + token)
    script_start = text.find("$script = @(")
    script_end = text.find("Set-Content -LiteralPath $scriptPath", script_start)
    script = text[script_start:script_end]
    pickfirst = script.find('"PICKFIRST", "1"')
    seed = script.find('"QS3DCURTAINP09SEED"')
    if min(pickfirst, seed) < 0 or pickfirst > seed:
        errors.append("P09 runner must enable PICKFIRST before probe selection is seeded")
    commands = (
        "QS3DCURTAINP09SEED", "QS3DGLASSWALL", "QS3DCURTAINP09PREPARE", "QS3DCURTAINP09BASELINE",
        "QS3DCURTAINP09ARMFINGERPRINT", "QS3DCURTAINP09VERIFYFINGERPRINT", "QS3DCURTAINP09PRECLEAN",
        "QS3DCURTAINP09VERIFYCLEAN", "QS3DCURTAINP09ARMUI", "QS3DCURTAINP09PROBE",
    )
    for command in commands:
        if ('"' + command + '"') not in script: errors.append("P09 runner missing state-machine command: " + command)
    if script.count('"QS3DCURTAIN3D"') != 4:
        errors.append("P09 runner must invoke production QS3DCURTAIN3D for baseline, two injected cases and clean recovery")
    order = (
        "QS3DCURTAINP09PREPARE", "QS3DCURTAIN3D", "QS3DCURTAINP09BASELINE",
        "QS3DCURTAINP09ARMFINGERPRINT", "QS3DCURTAIN3D", "QS3DCURTAINP09VERIFYFINGERPRINT",
        "QS3DCURTAINP09PRECLEAN", "QS3DCURTAIN3D", "QS3DCURTAINP09VERIFYCLEAN",
        "QS3DCURTAINP09ARMUI", "QS3DCURTAIN3D", "QS3DCURTAINP09PROBE",
    )
    cursor = 0
    for command in order:
        cursor = script.find('"' + command + '"', cursor)
        if cursor < 0: errors.append("P09 runner command ordering is invalid at: " + command); break
        cursor += 1
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end].lower()
    for forbidden in ("profile =", "drawing_path", "plugin_path", "artifact_path", "handle"):
        if forbidden in metadata: errors.append("P09 metadata contains private/identity field: " + forbidden)

if RUNBOOK.is_file():
    text = RUNBOOK.read_text(encoding="utf-8")
    for token in ("P09", "PENDING_LOCAL", "post-commit"):
        if token not in text: errors.append("Curtain runbook missing P09 boundary: " + token)

if INBOX.is_file():
    text = INBOX.read_text(encoding="utf-8")
    for token in ("LOCAL-002", "P09-P12", "post-commit warning"):
        if token not in text: errors.append("Local inbox missing P09 boundary: " + token)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("P09 claim must be ACTIVE or COMPLETED")

print("QS3D Curtain-panel P09 post-commit runtime probe preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: P09 keeps fault activation internal and one-shot, injects only after native commit, proves committed replacement plus Health review for missing fingerprints, and isolates UI failure under the existing best-effort warning boundary with exact-SHA privacy/cleanup guards.")
