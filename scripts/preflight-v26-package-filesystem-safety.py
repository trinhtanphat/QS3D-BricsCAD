#!/usr/bin/env python3
"""Fail closed if V26 packaging loses repository-contained filesystem safety."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v26.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"V26 package filesystem safety missing {label}: {token}")


def before(text: str, left: str, right: str, label: str) -> None:
    require(text, left, label + " left")
    require(text, right, label + " right")
    if text.index(left) >= text.index(right):
        raise SystemExit(f"V26 package filesystem safety ordering failed: {label}")


def validate(text: str) -> None:
    required = {
        "canonical full path": "function Get-CanonicalFullPath",
        "repository containment": "function Test-PathEqualOrContained",
        "ordinary directory": "function Assert-OrdinaryDirectory",
        "safe directory target": "function Assert-SafeOutputDirectoryTarget",
        "safe file target": "function Assert-SafeOutputFileTarget",
        "safe package walk": "function Get-SafePackageFiles",
        "reparse attribute": "[IO.FileAttributes]::ReparsePoint",
        "package reparse refusal": "Package staging contains a reparse-backed entry",
        "package non-regular refusal": "Package staging contains a non-regular filesystem entry",
        "dist root binding": "$distRoot = Assert-SafeOutputDirectoryTarget -Path $distRoot",
        "staging binding": "$dist = Assert-SafeOutputDirectoryTarget -Path $dist",
        "zip binding": "$zip = Assert-SafeOutputFileTarget -Path $zip",
        "safe hash walk": "$hashLines = Get-SafePackageFiles -PackageRoot $dist",
        "pre-compress safe walk": "$null = Get-SafePackageFiles -PackageRoot $dist",
        "V26 zip": "QS3D-BricsCAD-V26.zip",
        "hash manifest": "SHA256SUMS.txt",
    }
    for label, token in required.items():
        require(text, token, label)

    before(
        text,
        "$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'",
        "if (Test-Path -LiteralPath $dist) { Remove-Item -LiteralPath $dist -Recurse -Force }",
        "repository trust before recursive staging cleanup",
    )
    before(
        text,
        "$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory' -MayBeMissing",
        "if (Test-Path -LiteralPath $dist) { Remove-Item -LiteralPath $dist -Recurse -Force }",
        "staging trust before recursive cleanup",
    )
    before(
        text,
        "$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'",
        "if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }",
        "zip trust before removal",
    )
    before(
        text,
        "$null = Get-SafePackageFiles -PackageRoot $dist",
        "Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal",
        "safe recursive package walk before compression",
    )

    forbidden = (
        "$hashLines = Get-ChildItem -LiteralPath $dist -Recurse -File",
        "Remove-Item -LiteralPath $dist -Recurse -Force -ErrorAction SilentlyContinue",
        "Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue",
    )
    for token in forbidden:
        if token in text:
            raise SystemExit(f"V26 package filesystem safety regressed to unsafe legacy path: {token}")


def mutation_probe(source: str, token: str, replacement: str, label: str) -> None:
    if token not in source:
        raise SystemExit(f"mutation setup missing {label}")
    mutated = source.replace(token, replacement)
    try:
        validate(mutated)
    except SystemExit:
        return
    raise SystemExit(f"mutation unexpectedly passed: {label}")


def main() -> None:
    source = PACKAGE.read_text(encoding="utf-8")
    validate(source)
    probes = (
        ("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Hidden", "reparse contract"),
        ("function Get-SafePackageFiles", "function Get-UnsafePackageFiles", "safe walker"),
        ("$hashLines = Get-SafePackageFiles -PackageRoot $dist", "$hashLines = Get-ChildItem -LiteralPath $dist -Recurse -File", "safe hash enumeration"),
        ("$null = Get-SafePackageFiles -PackageRoot $dist", "$null = @()", "pre-compress traversal"),
        ("$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'", "$root = [IO.Path]::GetFullPath($root)", "repository root trust"),
    )
    for token, replacement, label in probes:
        mutation_probe(source, token, replacement, label)
    print("PASS V26 package filesystem safety")


if __name__ == "__main__":
    main()
