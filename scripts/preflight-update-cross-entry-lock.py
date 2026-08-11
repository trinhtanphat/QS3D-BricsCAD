#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATE = ROOT / "scripts" / "update-v25.ps1"
INSTALL = ROOT / "scripts" / "install-v25-autoload.ps1"
PREFIX = "$UpdateMutexPrefix = 'Global\\QS3D-BricsCAD-V25-Update-'"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing script: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def assert_common(text: str, label: str) -> None:
    require(text, PREFIX, f"{label} shared mutex prefix")
    require(text, "[System.Security.Principal.WindowsIdentity]::GetCurrent()", f"{label} Windows identity")
    require(text, "$identity.User.Value", f"{label} Windows SID binding")
    require(text, "$mutexName = $UpdateMutexPrefix + $sid", f"{label} SID-namespaced mutex name")
    require(text, "[System.Threading.Mutex]::new($false, $mutexName)", f"{label} named mutex open")
    require(text, "$mutex.WaitOne(0)", f"{label} nonblocking ownership attempt")
    require(text, "catch [System.Threading.AbandonedMutexException] { $ownsMutex = $true }", f"{label} abandoned mutex recovery")
    require(text, "if (-not $ownsMutex)", f"{label} contention fail closed")
    require(text, "Another QS3D install/update is already active for this Windows user", f"{label} actionable contention error")
    require(text, "function Exit-Qs3dUpdateMutex", f"{label} deterministic release helper")
    require(text, "$Mutex.ReleaseMutex()", f"{label} release one ownership level")
    require(text, "$Mutex.Dispose()", f"{label} mutex handle disposal")
    require(text, "$updateMutex = Enter-Qs3dUpdateMutex", f"{label} entry lock acquisition")
    require(text, "Exit-Qs3dUpdateMutex -Mutex $updateMutex", f"{label} outer-finally release")


def main() -> int:
    update = read(UPDATE)
    install = read(INSTALL)
    assert_common(update, "secure updater")
    assert_common(install, "installer")

    # Both direct entry points must refuse a live CAD host before entering mutation/network work.
    update_cad = update.find("if (Get-Process -Name bricscad -ErrorAction SilentlyContinue)")
    update_lock = update.find("$updateMutex = Enter-Qs3dUpdateMutex")
    update_manifest = update.find("$manifestAddress = Convert-ToSafeHttpsUri")
    update_network = update.find("Invoke-WebRequest -Uri $manifestAddress.AbsoluteUri")
    update_installer = update.find("& $installer @arguments")
    update_release = update.rfind("Exit-Qs3dUpdateMutex -Mutex $updateMutex")
    if min(update_cad, update_lock, update_manifest, update_network, update_installer, update_release) < 0 or not (
        update_cad < update_lock < update_manifest < update_network < update_installer < update_release
    ):
        raise AssertionError("secure updater must refuse live CAD, acquire cross-entry lock, then hold it through manifest/package preparation and nested installer")

    install_cad = install.find("$runningBricsCAD = @(Get-RunningBricsCADProcessDetails)")
    install_lock = install.find("$updateMutex = Enter-Qs3dUpdateMutex")
    install_integrity = install.find("$commands = Assert-PackageIntegrity")
    install_identity = install.find("Assert-PackageIdentity -Directory $package")
    install_snapshots = install.find("$registrySnapshots = @(")
    install_stage = install.find("New-Item -ItemType Directory -Path $stage -Force")
    install_rollback = install.find("throw $originalError")
    install_release = install.rfind("Exit-Qs3dUpdateMutex -Mutex $updateMutex")
    if min(install_cad, install_lock, install_integrity, install_identity, install_snapshots, install_stage, install_rollback, install_release) < 0 or not (
        install_cad < install_lock < install_integrity < install_identity < install_snapshots < install_stage < install_rollback < install_release
    ):
        raise AssertionError("installer must refuse live CAD, acquire cross-entry lock before package/registry state, and hold it through commit/rollback")

    # Preserve the independent security/atomicity boundaries this lane must not weaken.
    for text, label in ((update, "secure updater"), (install, "installer")):
        if "Stop-Process" in text or "taskkill" in text or ".Kill(" in text:
            raise AssertionError(f"{label} must never force-terminate BricsCAD/processes")
    require(update, "Assert-SafeArchive", "secure updater archive gate")
    require(update, "ExpectedSignerThumbprint = $expectedSigner", "secure updater signed installer handoff")
    require(update, "Installed QS3D productVersion changed during update preparation", "secure updater stale-state recheck")
    require(install, "Duplicate SHA256SUMS payload entry", "installer complete hash-manifest integrity")
    require(install, "Unhashed package payload", "installer unhashed-file rejection")
    require(install, "Assert-PackageIdentity -Directory $package", "installer package identity binding")
    require(install, "Unblock-File -LiteralPath $destination -ErrorAction Stop", "installer MOTW clearing after verified copy")
    require(install, "Restore-DemandLoadSnapshot", "installer DemandLoad rollback")
    require(install, "throw $originalError", "installer original failure propagation")

    print(
        "PASS: detached/manual secure update and direct install share the same per-user Windows mutex; "
        "manual entry points fail fast on contention and hold recursive ownership through nested update->installer completion/rollback."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
