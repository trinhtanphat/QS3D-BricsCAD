#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNINSTALL = ROOT / "scripts" / "uninstall-v25-autoload.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing uninstall source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def file_removal_allowed(*, default_scope: bool, force: bool, identity_valid: bool) -> bool:
    if not identity_valid:
        return False
    return default_scope or force


def main() -> int:
    text = read(UNINSTALL)

    require(text, "function Get-RegistryTreeSnapshot", "recursive registry snapshot helper")
    require(text, "$key.GetValueNames()", "registry value enumeration")
    require(text, "$key.GetValueKind($name).ToString()", "registry value-kind snapshot")
    require(text, "$key.GetSubKeyNames()", "registry child-key enumeration")
    require(text, "Children = @($children)", "recursive child snapshots")
    require(text, "function Restore-RegistryTreeSnapshot", "registry restore helper")
    require(text, "[Microsoft.Win32.RegistryValueKind][Enum]::Parse", "registry kind restoration")
    require(text, "$key.SetValue([string]$value.Name, $value.Value, $kind)", "registry value restoration")
    require(text, "Restore-RegistryTreeSnapshot -Snapshot $child", "recursive child restoration")
    require(text, "function Get-DemandLoadTargets", "non-mutating registry target discovery")

    require(text, "$registryPlan = @()", "pre-mutation registry plan")
    require(text, "$PSCmdlet.ShouldProcess", "ShouldProcess preservation")
    require(text, "$snapshot = Get-RegistryTreeSnapshot -Path $target.AppKey", "approved registry snapshot")
    require(text, "$stageFiles = $PSCmdlet.ShouldProcess($installFull, 'Remove QS3D installed files')", "approved file staging")
    require(text, "('.qs3d-uninstall-' + [Guid]::NewGuid().ToString('N'))", "unique same-parent quarantine")
    require(text, "Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop", "canonical install staging")
    require(text, "$removedSnapshots += $entry.Snapshot", "rollback tracking before registry mutation")
    require(text, "Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop", "planned registry removal")

    require(text, "$originalError = $_", "original uninstall error capture")
    require(text, "$filesRestored = $true", "file rollback state")
    require(text, "Move-Item -LiteralPath $quarantine -Destination $installFull -ErrorAction Stop", "quarantine rollback")
    require(text, "for ($index = $removedSnapshots.Count - 1; $index -ge 0; $index--)", "reverse registry rollback")
    require(text, "Restore-RegistryTreeSnapshot -Snapshot $removedSnapshots[$index]", "registry rollback execution")
    require(text, "skipped restore because the canonical install directory could not be restored", "avoid stale registration after file rollback failure")
    require(text, "QS3D uninstall rollback encountered error(s)", "rollback failure reporting")
    require(text, "throw $originalError", "original failure propagation")

    require(text, "Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop", "post-commit quarantine cleanup")
    require(text, "QS3D uninstall committed, but cleanup of quarantine", "post-commit cleanup warning")
    require(text, "DemandLoad is removed and the canonical install path is no longer active", "logical commit semantics")

    # File-removal ownership must remain fail closed even when -Force allows a custom path.
    require(text, "Refusing to remove a custom install directory outside the QS3D LocalAppData scope", "custom-path guard")
    require(text, "$metadataPath = Join-Path $installFull 'PACKAGE-METADATA.json'", "package metadata identity marker")
    require(text, "$pluginPath = Join-Path $installFull 'QS3D.BricsCAD.V25.dll'", "plugin identity marker")
    require(text, "$corePath = Join-Path $installFull 'QS3D.Core.dll'", "Core identity marker")
    require(text, "canonical QS3D package identity files", "required identity files refusal")
    require(text, "[Version]::Parse([string]$metadata.version)", "metadata AssemblyVersion parse")
    require(text, "[Reflection.AssemblyName]::GetAssemblyName($identityPath).Version", "managed DLL AssemblyVersion read")
    require(text, "[Diagnostics.FileVersionInfo]::GetVersionInfo($identityPath).ProductVersion", "managed DLL ProductVersion read")
    require(text, "[StringComparison]::Ordinal", "exact ProductVersion identity")
    require(text, "PACKAGE-METADATA/DLL identity is not a valid QS3D V25 installation", "strong identity refusal")
    if "if (-not $ForceDelete) {\n        $metadataPath" in text:
        raise AssertionError("-Force must never bypass uninstall package/DLL identity validation")

    policy_cases = (
        ({"default_scope": True, "force": False, "identity_valid": True}, True, "verified default install"),
        ({"default_scope": True, "force": True, "identity_valid": True}, True, "verified default install with force"),
        ({"default_scope": False, "force": False, "identity_valid": True}, False, "verified custom install without force"),
        ({"default_scope": False, "force": True, "identity_valid": True}, True, "verified custom install with force"),
        ({"default_scope": True, "force": False, "identity_valid": False}, False, "foreign default-scope directory"),
        ({"default_scope": True, "force": True, "identity_valid": False}, False, "forced foreign default-scope directory"),
        ({"default_scope": False, "force": True, "identity_valid": False}, False, "forced foreign custom directory"),
    )
    for kwargs, expected, label in policy_cases:
        actual = file_removal_allowed(**kwargs)
        if actual is not expected:
            raise AssertionError(f"uninstall removal policy mismatch for {label}: expected {expected}, got {actual}")

    lock_pos = text.find("$updateMutex = Enter-Qs3dUpdateMutex")
    identity_pos = text.find("Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory")
    identity_files_pos = text.find("$metadataPath = Join-Path $installFull 'PACKAGE-METADATA.json'")
    identity_dll_pos = text.find("[Reflection.AssemblyName]::GetAssemblyName($identityPath).Version")
    plan_pos = text.find("$registryPlan = @()")
    snapshot_pos = text.find("$snapshot = Get-RegistryTreeSnapshot -Path $target.AppKey")
    stage_pos = text.find("Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop")
    tracked_pos = text.find("$removedSnapshots += $entry.Snapshot")
    remove_registry_pos = text.find("Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop")
    rollback_file_pos = text.find("Move-Item -LiteralPath $quarantine -Destination $installFull -ErrorAction Stop")
    rollback_registry_pos = text.find("Restore-RegistryTreeSnapshot -Snapshot $removedSnapshots[$index]")
    rethrow_pos = text.find("throw $originalError")
    cleanup_pos = text.find("Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop")
    release_pos = text.rfind("Exit-Qs3dUpdateMutex -Mutex $updateMutex")
    positions = (
        lock_pos, identity_pos, identity_files_pos, identity_dll_pos, plan_pos, snapshot_pos, stage_pos, tracked_pos,
        remove_registry_pos, rollback_file_pos, rollback_registry_pos, rethrow_pos,
        cleanup_pos, release_pos,
    )
    if min(positions) < 0 or not (
        identity_files_pos < identity_dll_pos < lock_pos < identity_pos < plan_pos < snapshot_pos < stage_pos < tracked_pos < remove_registry_pos < cleanup_pos < release_pos
    ):
        raise AssertionError("uninstall identity definition -> lock/validate -> plan+snapshot -> quarantine stage -> registry mutation -> post-commit cleanup -> release ordering is required")
    if not (remove_registry_pos < rollback_file_pos < rollback_registry_pos < rethrow_pos):
        raise AssertionError("failure path must restore canonical files before registry snapshots and then rethrow the original failure")

    require(text, "if (-not $KeepFiles)", "KeepFiles preservation")
    require(text, "if (Get-Process -Name bricscad -ErrorAction SilentlyContinue)", "all-BricsCAD closed precondition")
    require(text, "$UpdateMutexPrefix = 'Global\\QS3D-BricsCAD-V25-Update-'", "shared update mutex")
    if "Stop-Process" in text or "taskkill" in text or ".Kill(" in text:
        raise AssertionError("uninstaller must never force-terminate BricsCAD/processes")

    print(
        "PASS: uninstall keeps package/DLL ownership fail closed even under -Force, uses force only for intentional custom-path scope, stages verified files before registry mutation, rolls back pre-commit failures, and treats post-commit quarantine deletion as residue cleanup."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
