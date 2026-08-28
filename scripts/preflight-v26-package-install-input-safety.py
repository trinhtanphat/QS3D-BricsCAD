#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "test-v26-package-install-lifecycle.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL v26 package install input safety: missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"FAIL v26 package install input safety: forbidden {label}: {token}")


def require_order(text: str, first: str, second: str, label: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        raise SystemExit(f"FAIL v26 package install input safety: ordering violated for {label}")


def validate(text: str) -> None:
    for token, label in (
        ("function Assert-NoReparseAncestors", "ancestor reparse admission"),
        ("function Resolve-OrdinaryNonReparseDirectory", "ordinary directory admission"),
        ("function Resolve-OrdinaryNonReparseFile", "ordinary file admission"),
        ("function Get-StreamingSha256", "streaming SHA-256"),
        ("[IO.File]::Open($File.FullName", "stream-backed hash read"),
        ("function Get-StableFileState", "stable generation capture"),
        ("$secondHash = Get-StreamingSha256", "second fingerprint capture"),
        ("function Assert-StableFileState", "post-consumption generation recheck"),
        ("function Read-BoundedStrictUtf8State", "bounded strict UTF-8 read"),
        ("[Text.UTF8Encoding]::new($false, $true)", "throwing UTF-8 decoder"),
        ("function Get-SafeFiles", "reparse-safe explicit traversal"),
        ("[Collections.Generic.Stack[string]]::new()", "explicit traversal stack"),
        ("if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw", "active reparse predicate"),
        ("contains a reparse-backed entry", "fail-closed reparse rejection"),
        ("$manifestState = Get-StableFileState", "manifest generation binding"),
        ("$metadataState = Get-StableFileState", "metadata generation binding"),
        ("$zipState = Get-StableFileState", "ZIP generation binding"),
        ("$state = Get-StableFileState -Path $payloadPath", "package payload generation binding"),
        ("$null = Assert-StableFileState -Expected $manifestState -Label 'V26 hash manifest'", "manifest post-read recheck"),
        ("Assert-PackageStates $packageEvidence", "whole-package pre/post consumption recheck"),
        ("$installerState = $packageEvidence.Manifest.States['install-v26-autoload.ps1']", "installer generation binding"),
        ("$installer = (Assert-StableFileState -Expected $installerState -Label 'V26 installer').FullName", "installer state assertion"),
        ("$uninstallerState = $packageEvidence.Manifest.States['uninstall-v26-autoload.ps1']", "uninstaller generation binding"),
        ("$uninstaller = (Assert-StableFileState -Expected $uninstallerState -Label 'V26 uninstaller').FullName", "uninstaller state assertion"),
        ("$state = Get-StableFileState -Path (Join-Path $install.FullName $name)", "installed payload generation binding"),
        ("Read-BoundedStrictUtf8State -State $installedStates['PACKAGE-METADATA.json']", "installed metadata bound read"),
        ("Read-BoundedStrictUtf8State -State $installedStates['QS3D.BricsCAD.V26.runtimeconfig.json']", "installed runtimeconfig bound read"),
        ("Refused unsafe disposable install cleanup", "fail-closed cleanup boundary"),
    ):
        require(text, token, label)

    for token, label in (
        ("Get-ChildItem -LiteralPath $packageDir -Recurse", "recursive package traversal"),
        ("Get-FileHash -LiteralPath $payload", "path-reopening package hash"),
        ("Get-FileHash -LiteralPath $installed", "path-reopening installed hash"),
        ("Get-Content -LiteralPath (Join-Path $packageDir 'PACKAGE-METADATA.json')", "unbound package metadata read"),
        ("Get-Content -LiteralPath (Join-Path $installDir 'PACKAGE-METADATA.json')", "unbound installed metadata read"),
        ("Get-Content -LiteralPath (Join-Path $installDir 'QS3D.BricsCAD.V26.runtimeconfig.json')", "unbound installed runtimeconfig read"),
    ):
        forbid(text, token, label)

    require_order(text, "$manifestState = Get-StableFileState", "$manifestText = Read-BoundedStrictUtf8State", "manifest capture before read")
    require_order(text, "$metadataState = Get-StableFileState", "$metadataText = Read-BoundedStrictUtf8State", "metadata capture before read")
    require_order(text, "Assert-PackageStates $packageEvidence", "$installerState = $packageEvidence.Manifest.States['install-v26-autoload.ps1']", "package recheck before installer")
    require_order(text, "& $installer -PackageDirectory", "Assert-PackageStates $packageEvidence\n    Assert-RegistrationIdentity", "package recheck after installer")


def mutation_lock(text: str) -> None:
    mutations = {
        "second fingerprint": text.replace("$secondHash = Get-StreamingSha256 -File $second -Label $Label", "$secondHash = $firstHash", 1),
        "reparse rejection": text.replace("if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw", "if ($false) { throw", 1),
        "manifest recheck": text.replace("$null = Assert-StableFileState -Expected $manifestState -Label 'V26 hash manifest'", "$null = $manifestState", 1),
        "installer binding": text.replace("$installer = (Assert-StableFileState -Expected $installerState -Label 'V26 installer').FullName", "$installer = $installerState.Path", 1),
        "uninstaller binding": text.replace("$uninstaller = (Assert-StableFileState -Expected $uninstallerState -Label 'V26 uninstaller').FullName", "$uninstaller = $uninstallerState.Path", 1),
        "installed metadata bound read": text.replace("Read-BoundedStrictUtf8State -State $installedStates['PACKAGE-METADATA.json']", "Get-Content -LiteralPath (Join-Path $installDir 'PACKAGE-METADATA.json') -Raw", 1),
    }
    for label, mutated in mutations.items():
        try:
            validate(mutated)
        except SystemExit:
            continue
        raise SystemExit(f"FAIL v26 package install input safety: mutation escaped guard: {label}")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    validate(text)
    mutation_lock(text)
    print("PASS v26 package install input safety")


if __name__ == "__main__":
    main()
