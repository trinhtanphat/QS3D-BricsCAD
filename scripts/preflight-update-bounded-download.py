#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing updater source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    updater = read(UPDATER)

    require(updater, "function Invoke-BoundedHttpsDownload", "shared bounded download helper")
    require(updater, "[System.Net.WebRequest]::CreateHttp($Address.AbsoluteUri)", "synchronous HttpWebRequest creation")
    require(updater, "$request.AllowAutoRedirect = $true", "HTTPS redirect support")
    require(updater, "$request.MaximumAutomaticRedirections = 5", "bounded redirect count")
    require(updater, "$request.Timeout = $TimeoutMilliseconds", "response timeout")
    require(updater, "$request.ReadWriteTimeout = $TimeoutMilliseconds", "stream read/write timeout")
    require(updater, "$request.AutomaticDecompression", "bounded transfer decompression")
    require(updater, "$finalUri = $response.ResponseUri", "final response URI inspection")
    require(updater, "$finalUri.Scheme -ne [Uri]::UriSchemeHttps", "final HTTPS fail-closed gate")
    require(updater, "$finalUri.UserInfo", "final credential-bearing URI rejection")
    require(updater, "$response.ContentLength -gt $MaxBytes", "known-length pre-copy bound")
    require(updater, "$total -gt ($MaxBytes - [int64]$read)", "streaming byte-count bound")
    require(updater, "$total += [int64]$read", "stream byte accounting")
    require(updater, "if ($total -le 0)", "empty download rejection")
    require(updater, "if (-not $completed -and (Test-Path -LiteralPath $DestinationPath))", "partial-output cleanup gate")
    require(updater, "Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction SilentlyContinue", "partial-output deletion")

    manifest_call = "Invoke-BoundedHttpsDownload -Address $manifestAddress -DestinationPath $manifestPath -MaxBytes 65536 -TimeoutMilliseconds 30000 -Label 'Update manifest'"
    package_call = "Invoke-BoundedHttpsDownload -Address $packageAddress -DestinationPath $zipPath -MaxBytes $maxBytes -TimeoutMilliseconds 120000 -Label 'Update package'"
    require(updater, manifest_call, "64 KiB / 30s final manifest transfer")
    require(updater, "$maxBytes = [int64]$MaxPackageSizeMB * 1MB", "configured package byte bound")
    require(updater, package_call, "bounded final package transfer")

    reject(updater, "Invoke-WebRequest -Uri $manifestAddress.AbsoluteUri -OutFile $manifestPath", "unbounded manifest OutFile transfer")
    reject(updater, "Invoke-WebRequest -Uri $packageAddress.AbsoluteUri -OutFile $zipPath", "unbounded package OutFile transfer")

    helper = updater.find("function Invoke-BoundedHttpsDownload")
    mutex = updater.find("$updateMutex = Enter-Qs3dUpdateMutex")
    manifest = updater.find(manifest_call)
    manifest_parse = updater.find("$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json")
    snapshot_package = updater.find("Assert-OfficialGitHubPackageSnapshot -PackageAddress $packageAddress")
    package = updater.find(package_call)
    package_hash = updater.find("Get-FileHash -LiteralPath $zipPath -Algorithm SHA256")
    archive = updater.find("Assert-SafeArchive -ZipPath $zipPath")
    signed_root = updater.find("Assert-PackageRoot -Directory $extractRoot -ExpectedSigner $expectedSigner")
    installer = updater.find("& $installer @arguments")
    release = updater.rfind("Exit-Qs3dUpdateMutex -Mutex $updateMutex")
    positions = (helper, mutex, manifest, manifest_parse, snapshot_package, package, package_hash, archive, signed_root, installer, release)
    if min(positions) < 0 or not (
        helper < mutex < manifest < manifest_parse < snapshot_package < package < package_hash < archive < signed_root < installer < release
    ):
        raise AssertionError(
            "bounded transfer ordering must preserve mutex -> manifest -> release snapshot -> package -> hash/archive/signer -> installer"
        )

    require(updater, "Update manifest must be between 1 byte and 64 KiB.", "post-download manifest defense in depth")
    require(updater, "Downloaded package size", "post-download package defense in depth")
    require(updater, "Downloaded package SHA-256 does not match the update manifest.", "package hash binding")
    require(updater, "Installed QS3D productVersion changed during update preparation", "stale installed-state recheck")
    require(updater, "ExpectedSignerThumbprint = $expectedSigner", "signed installer handoff")
    if "Stop-Process" in updater or "taskkill" in updater or ".Kill(" in updater:
        raise AssertionError("PowerShell updater must not force-terminate BricsCAD/processes")

    print(
        "PASS: final updater manifest/package transfers are HTTPS, timeout/redirect/stream bounded, clean partial files on failure, "
        "and retain release-snapshot/hash/archive/signer/install ordering."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
