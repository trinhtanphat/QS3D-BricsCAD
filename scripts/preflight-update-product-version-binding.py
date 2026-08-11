#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "scripts" / "new-v25-update-manifest.ps1"
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
    manifest = read(MANIFEST)
    updater = read(UPDATER)

    require(manifest, "function Convert-ToStrictSemVerText", "manifest strict SemVer validation")
    require(manifest, "PACKAGE-METADATA is missing productVersion", "manifest package productVersion requirement")
    require(manifest, "Read-PluginProductVersion", "signed plugin product-version read")
    require(manifest, "PACKAGE-METADATA productVersion $productVersion does not match signed QS3D plugin product version", "manifest signed product-version binding")
    require(manifest, "schemaVersion = 2", "update manifest schema 2")
    require(manifest, "productVersion = $signedPluginProductVersion", "manifest productVersion emission")
    reject(manifest, "schemaVersion = 1", "legacy assembly-only manifest generation")

    require(updater, "function Convert-ToStrictSemVer", "updater strict SemVer parser")
    require(updater, "function Compare-StrictSemVer", "updater SemVer precedence")
    require(updater, "function Read-InstalledProductVersion", "installed product-version authority")
    require(updater, "FileVersionInfo]::GetVersionInfo", "signed DLL product-version source")
    require(updater, "schemaVersion -ne 2", "schema 2 enforcement")
    require(updater, "Secure auto-update requires schemaVersion 2 with productVersion binding", "legacy schema fail-closed message")
    require(updater, "Update manifest productVersion", "manifest target product SemVer parse")
    require(updater, "Compare-StrictSemVer -Left $targetProductVersion -Right $installedProductVersion", "target-vs-installed SemVer comparison")
    require(updater, "Refusing product-version downgrade", "product downgrade rejection")
    require(updater, "-AllowSameVersion never authorizes product-version replay or repair", "same AssemblyVersion cannot authorize product replay")
    require(updater, "Downloaded PACKAGE-METADATA.json is missing productVersion", "downloaded metadata product requirement")
    require(updater, "Downloaded PACKAGE-METADATA productVersion", "downloaded package strict SemVer parse")
    require(updater, "Downloaded signed QS3D plugin product version", "downloaded signed DLL product version")
    require(updater, "does not match signed plugin product version", "package-to-signed-DLL product binding")
    require(updater, "does not match manifest productVersion", "package-to-manifest product binding")
    require(updater, "is not newer than installed productVersion", "downloaded package monotonicity recheck")
    require(updater, "Installed QS3D productVersion changed during update preparation", "concurrent updater product-state guard")

    # AssemblyVersion remains an independent binary/package check. The compatibility switch
    # may allow an equal assembly version, but it must not bypass product SemVer monotonicity.
    require(updater, "if ($targetVersion -lt $installedVersion)", "assembly downgrade guard")
    require(updater, "if ($targetVersion -eq $installedVersion -and -not $AllowSameVersion)", "same AssemblyVersion compatibility gate")
    require(updater, "if ($signedPluginVersion -ne $targetVersion)", "signed DLL assembly binding")
    require(updater, "if ($packageVersion -ne $targetVersion)", "package metadata assembly binding")

    print("PASS: update manifests and updater bind signed package identity to monotonically newer product SemVer.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
