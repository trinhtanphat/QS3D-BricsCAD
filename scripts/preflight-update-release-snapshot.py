#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATE = ROOT / "scripts" / "update-v25.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing updater script: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    text = read(UPDATE)

    require(text, "function Get-OfficialGitHubReleaseSnapshot", "official manifest snapshot parser")
    require(text, "function Assert-OfficialGitHubPackageSnapshot", "official package snapshot validator")
    require(text, "'/trinhtanphat/QS3D-BricsCAD/releases/download/'", "canonical repository release path")
    require(text, "QS3D-BricsCAD-V25.update.json", "canonical manifest asset")
    require(text, "QS3D-BricsCAD-V25.zip", "canonical package asset")
    require(text, "[Uri]::UnescapeDataString", "decoded release tag/asset validation")
    require(text, "Official GitHub release tag", "strict SemVer derivation from frozen tag")
    require(text, "$officialReleaseSnapshot = Get-OfficialGitHubReleaseSnapshot -ManifestAddress $manifestAddress", "snapshot derivation from immutable ManifestUri")
    require(text, "does not match scheduled GitHub release", "manifest productVersion snapshot mismatch rejection")
    require(text, "Assert-OfficialGitHubPackageSnapshot -PackageAddress $packageAddress -Snapshot $officialReleaseSnapshot", "same-tag package path binding")

    manifest_parse = text.find("$manifestAddress = Convert-ToSafeHttpsUri")
    snapshot_parse = text.find("$officialReleaseSnapshot = Get-OfficialGitHubReleaseSnapshot", manifest_parse)
    manifest_download = text.find("Invoke-WebRequest -Uri $manifestAddress.AbsoluteUri", snapshot_parse)
    target_product = text.find("$targetProductVersion = Convert-ToStrictSemVer", manifest_download)
    product_snapshot_gate = text.find("does not match scheduled GitHub release", target_product)
    package_parse = text.find("$packageAddress = Convert-ToSafeHttpsUri", product_snapshot_gate)
    package_snapshot_gate = text.find("Assert-OfficialGitHubPackageSnapshot -PackageAddress $packageAddress", package_parse)
    package_download = text.find("Invoke-WebRequest -Uri $packageAddress.AbsoluteUri", package_snapshot_gate)
    installer_call = text.find("& $installer @arguments", package_download)

    ordered = (
        manifest_parse,
        snapshot_parse,
        manifest_download,
        target_product,
        product_snapshot_gate,
        package_parse,
        package_snapshot_gate,
        package_download,
        installer_call,
    )
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        raise AssertionError(
            "official release snapshot must be derived before manifest fetch and must bind productVersion/package path before ZIP download/install"
        )

    # Official path must reject URL ambiguity and same-publisher cross-release mix-and-match.
    for needle in (
        "$ManifestAddress.UserInfo -or $ManifestAddress.Query -or $ManifestAddress.Fragment",
        "$PackageAddress.UserInfo -or $PackageAddress.Query -or $PackageAddress.Fragment",
        "does not match scheduled release tag",
        "does not belong to trinhtanphat/QS3D-BricsCAD release downloads",
    ):
        require(text, needle, "official URL identity guard")

    # Preserve independent security/atomicity gates.
    require(text, "$updateMutex = Enter-Qs3dUpdateMutex", "cross-entry mutex")
    require(text, "Exit-Qs3dUpdateMutex -Mutex $updateMutex", "cross-entry mutex release")
    require(text, "Update manifest signerThumbprint does not match ExpectedSignerThumbprint", "manifest signer binding")
    require(text, "Downloaded package SHA-256 does not match the update manifest", "ZIP hash binding")
    require(text, "Assert-PackageRoot -Directory $extractRoot -ExpectedSigner $expectedSigner", "downloaded signed payload verification")
    require(text, "Refusing product-version downgrade", "monotonic product SemVer")
    require(text, "productVersion changed during update preparation", "installed-state stale recheck")
    require(text, "ExpectedSignerThumbprint = $expectedSigner", "signed installer handoff")

    if "Stop-Process" in text or "taskkill" in text or ".Kill(" in text:
        raise AssertionError("secure updater PowerShell must not force-terminate processes")

    print(
        "PASS: final official GitHub update fetch is bound to the release tag frozen in ManifestUri; "
        "re-fetched productVersion and package URL cannot switch repo/tag/asset before ZIP download/install."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
