#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "scripts" / "finalize-v25-signed-package.ps1"
V26 = ROOT / "scripts" / "finalize-v26-signed-package.ps1"


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return pos


def require_before(text: str, left: str, right: str, label: str) -> None:
    left_pos = require(text, left, label + " left")
    right_pos = require(text, right, label + " right")
    if left_pos >= right_pos:
        raise SystemExit(f"FAIL: {label} ordering is unsafe")


def main() -> None:
    v25 = V25.read_text(encoding="utf-8")
    v26 = V26.read_text(encoding="utf-8")

    for token, label in (
        ("function Get-CanonicalFullPath", "canonical path helper"),
        ("function Assert-NoReparseDirectoryChain", "ancestor reparse guard"),
        ("function Assert-SafeDirectory", "directory guard"),
        ("function Assert-SafeFile", "file guard"),
        ("function Assert-SafeOptionalFileTarget", "ZIP target guard"),
        ("function Get-SafePackageFiles", "bounded traversal helper"),
        ("[IO.FileAttributes]::ReparsePoint", "reparse-point refusal"),
        ("[IO.Path]::GetPathRoot($fullPath)", "filesystem-root identity"),
        ("must not be a filesystem root", "filesystem-root refusal"),
        ("Assert-SafeDirectory -Path $PackageDirectory", "package-root validation"),
        ("Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip'", "ZIP validation"),
        ("Assert-SafeFile -Path (Join-Path $package 'PACKAGE-METADATA.json')", "metadata validation"),
        ("Assert-SafeFile -Path (Join-Path $package $name)", "signed payload validation"),
        ("Get-SafePackageFiles -PackageRoot $package", "safe package traversal"),
        ("Assert-NoReparseDirectoryChain -Path $zipParent", "ZIP parent validation"),
    ):
        require(v25, token, label)

    require_before(
        v25,
        "[IO.Path]::GetPathRoot($fullPath)",
        "Assert-NoReparseDirectoryChain -Path $fullPath -Label $Label",
        "filesystem-root refusal must precede directory traversal",
    )
    require_before(
        v25,
        "must not be a filesystem root",
        "Assert-NoReparseDirectoryChain -Path $fullPath -Label $Label",
        "filesystem-root refusal must precede reparse traversal",
    )
    require_before(
        v25,
        "Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip'",
        "$PSCmdlet.ShouldProcess",
        "ZIP safety validation must precede mutation gate",
    )
    require_before(
        v25,
        "Assert-SafeFile -Path (Join-Path $package 'PACKAGE-METADATA.json')",
        "$PSCmdlet.ShouldProcess",
        "metadata safety validation must precede mutation gate",
    )
    require_before(
        v25,
        "Get-SafePackageFiles -PackageRoot $package",
        "$metadata | ConvertTo-Json",
        "package-tree validation must precede metadata mutation",
    )
    require_before(
        v25,
        "Assert-NoReparseDirectoryChain -Path $zipParent",
        "Remove-Item -LiteralPath $zip -Force",
        "ZIP parent validation must precede ZIP removal",
    )
    require_before(
        v25,
        "$zip = Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip'",
        "Remove-Item -LiteralPath $zip -Force",
        "ZIP target validation must precede ZIP removal",
    )

    if "Get-ChildItem -LiteralPath $package -Recurse -File" in v25:
        raise SystemExit("FAIL: finalizer still recursively enumerates package files without the safe traversal helper")

    for token, label in (
        ("new-v26-script-from-v25.ps1", "V26 generation helper"),
        ("-SourceScript 'finalize-v25-signed-package.ps1'", "V26 source finalizer"),
        ("if ($generated -match '(?i)v25')", "V26 stale-token guard"),
        ("& $tempScript @forward", "V26 generated finalizer execution"),
    ):
        require(v26, token, label)

    print("PASS: signed-package finalizer validates reparse/path/root boundaries before destructive mutation")


if __name__ == "__main__":
    main()
