#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SaveAsLifecycleProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-saveas-lifecycle.ps1"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"SAVEAS lifecycle preflight failed: missing {label}: {token}")


probe = PROBE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
v26 = V26.read_text(encoding="utf-8")

for token, label in [
    ('CommandMethod("QS3DSAVEASLIFECYCLEPREP"', "prepare command"),
    ('CommandMethod("QS3DSAVEASLIFECYCLEVERIFY"', "verify command"),
    ('ProjectContextCoordinator.Save(document)', "baseline persistence"),
    ('ProjectContextCoordinator.HasPendingChanges(document)', "pending-state assertion"),
    ('ProjectContextCoordinator.Forget(document)', "cold-cache transition"),
    ('canonical_project_identity_preserved=true', "canonical identity marker"),
    ('original_sidecar_unchanged=true', "old-sidecar isolation marker"),
    ('cold_cache_reload_matched=true', "cold-cache marker"),
]:
    require(probe, token, label)

for token, label in [
    ("[ValidateSet(25, 26)][int]$ExpectedHostMajor", "V25/V26 host-major guard"),
    ("Assert-Qs3dExactSourceIdentity", "exact-source identity guard"),
    ("QS3DSAVEASLIFECYCLEPREP", "prepare invocation"),
    ("_.SAVEAS", "native SAVEAS invocation"),
    ("QS3DSAVEASLIFECYCLEVERIFY", "verify invocation"),
    ("samples/generated/QS3D-Sample.dwg", "synthetic fixture restriction"),
    ("Wait-Qs3dNoExactBricsCadProcesses", "process cleanup guard"),
]:
    require(runner, token, label)

require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared-source inclusion")

for forbidden in ["customer.dwg", "private.dwg", "LOCAL_PASS"]:
    if forbidden in probe:
        raise SystemExit(f"SAVEAS lifecycle preflight failed: probe contains forbidden evidence token: {forbidden}")

print("SAVEAS lifecycle source/runner preflight PASS")
