#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileNativeGripRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-source-reconcile-native-grip.ps1"

errors = []
for path in (PROBE, RUNNER):
    if not path.is_file():
        errors.append(f"missing LOCAL-004 P05 source-prep file: {path.relative_to(ROOT)}")

if not errors:
    probe = PROBE.read_text(encoding="utf-8")
    runner = RUNNER.read_text(encoding="utf-8")

    for command in (
        "QS3DSRGRIPP05BASELINE",
        "QS3DSRGRIPP05SELECT",
        "QS3DSRGRIPP05CANCELCHECK",
        "QS3DSRGRIPP05EDITCHECK",
        "QS3DSRGRIPP05SYNCCHECK",
        "QS3DSRGRIPP05FINAL",
        "QS3DSRGRIPP05REOPEN",
    ):
        if probe.count(f'CommandMethod("{command}"') != 1:
            errors.append(f"P05 probe must register {command} exactly once")

    required_probe = (
        "LOCAL_004_P05_MANUAL_GRIP_CANCEL_COMMIT",
        "QS3D_SOURCE_RECONCILE_NATIVE_GRIP_RUNTIME_V1",
        "manual_grip_cancel_verified=true",
        "manual_grip_commit_verified=true",
        "source_reconcile_verified=true",
        "replacement_generated=true",
        "production_local004_p05_reopen_candidate=true",
        "prior_session_phases_replayed=false",
        "cold_reopen_verified=true",
        "RequireSource(context.Document, owner, 5d)",
        "RequireSource(context.Document, owner, 8d)",
        "RequireSemantic(owner, 5d)",
        "RequireSemantic(owner, 8d)",
        "RequireQuantities(owner, 5d)",
        "RequireQuantities(owner, 8d)",
        "CadHandleService.GetLiveHandles",
        "GeneratedGeometryService.HasMatchingOwnership",
        'owner.Properties.TryGetValue("GeneratedSolidHandle"',
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
    )
    for token in required_probe:
        if token not in probe:
            errors.append(f"P05 probe missing contract token: {token}")

    forbidden_probe = (
        "OpenMode.ForWrite", "StartTransaction()", "AppendEntity(", ".Erase(",
        "SendStringToExecute", ".Editor.Command(", "ProjectContextCoordinator.GetOrCreate",
        "SemanticCaptureService.Capture", "RegenerateDirtySubset", "BuildSelected(",
        "production_local004_p05_qualified_candidate=true",
    )
    for token in forbidden_probe:
        if token in probe:
            errors.append(f"read-only P05 probe must not perform native edit/reconcile/build directly: {token}")

    phase_claims = (
        "manual_grip_cancel_verified=true",
        "manual_grip_commit_verified=true",
        "source_reconcile_verified=true",
        "replacement_generated=true",
        "cold_reopen_verified=true",
    )
    for token in phase_claims:
        if probe.count(token) != 1:
            errors.append(f"P05 phase claim must be emitted exactly once by its own phase: {token}")

    required_runner = (
        "preflight-source-reconcile-native-grip-runtime-probe.py",
        "BRICSCAD_V25_DIR",
        "QS3DDRAWBEAM",
        "QS3DSRGRIPP05BASELINE",
        "QS3DSRGRIPP05SELECT",
        "QS3DSRGRIPP05CANCELCHECK",
        "QS3DSRGRIPP05EDITCHECK",
        "QS3DSYNCSOURCE",
        "QS3DSRGRIPP05SYNCCHECK",
        "QS3DBUILD3D",
        "QS3DSRGRIPP05FINAL",
        "QS3DSRGRIPP05REOPEN",
        "manual endpoint grip",
        "ESC",
        "PENDING_LOCAL",
        "status --porcelain=v1",
        "[string[]]$ArgumentList",
        "@ArgumentList",
        "REOPEN proves only current cold state",
    )
    for token in required_runner:
        if token not in runner:
            errors.append(f"P05 runner missing exact-SHA/manual-native token: {token}")

    for token in ("[string[]]$Args", "@Args"):
        if token in runner:
            errors.append(f"P05 runner must not shadow PowerShell automatic $Args: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: LOCAL-004 P05 source-prep pins manual Beam endpoint-grip ESC/commit, pre-sync isolation, production reconcile/rebuild and cold-reopen; licensed V25 execution remains PENDING_LOCAL.")
