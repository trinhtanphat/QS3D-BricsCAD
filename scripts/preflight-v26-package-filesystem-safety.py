#!/usr/bin/env python3
"""Fail closed if V26 packaging loses repository-contained filesystem safety."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v26.ps1"
ZIP_BINDING = "$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'"
ZIP_EXISTS_REFUSAL = "if (Test-Path -LiteralPath $zip) { throw 'V26 package ZIP destination already exists; refusing destructive pathname replacement.' }"


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
        "zip binding": ZIP_BINDING,
        "zip existing-generation refusal": ZIP_EXISTS_REFUSAL,
        "safe manifest walk": "foreach ($file in Get-SafePackageFiles -PackageRoot $dist)",
        "manifest path map": "$manifestHashes = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::Ordinal)",
        "ordinal manifest ordering": "[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)",
        "pre-archive safe walk": "$null = Get-SafePackageFiles -PackageRoot $dist",
        "deterministic zip builder": "function New-DeterministicPackageZip",
        "deterministic zip invocation": "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
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
        ZIP_BINDING,
        ZIP_EXISTS_REFUSAL,
        "zip trust before existing-generation refusal",
    )
    before(
        text,
        ZIP_EXISTS_REFUSAL,
        "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
        "existing ZIP refusal before deterministic archive",
    )
    before(
        text,
        "$null = Get-SafePackageFiles -PackageRoot $dist",
        "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
        "safe recursive package walk before deterministic archive",
    )

    forbidden = (
        "$hashLines = Get-ChildItem -LiteralPath $dist -Recurse -File",
        "foreach ($file in Get-ChildItem -LiteralPath $dist -Recurse -File)",
        "Remove-Item -LiteralPath $dist -Recurse -Force -ErrorAction SilentlyContinue",
        "Remove-Item -LiteralPath $zip -Force",
        "[IO.File]::Move($temporary, $destination)",
    )
    for token in forbidden:
        if token in text:
            raise SystemExit(f"V26 package filesystem safety regressed to unsafe legacy path: {token}")


def mutation_probe(source: str, token: str, replacement: str, label: str) -> None:
    if token not in source:
        raise SystemExit(f"mutation setup missing {label}")
    mutated = source.replace(token, replacement, 1)
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
        ("foreach ($file in Get-SafePackageFiles -PackageRoot $dist)", "foreach ($file in Get-ChildItem -LiteralPath $dist -Recurse -File)", "safe manifest enumeration"),
        ("$null = Get-SafePackageFiles -PackageRoot $dist", "$null = @()", "pre-archive traversal"),
        ("New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc", "Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal", "deterministic archive path"),
        ("$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'", "$root = [IO.Path]::GetFullPath($root)", "repository root trust"),
        (ZIP_EXISTS_REFUSAL, "if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }", "existing ZIP destructive replacement refusal"),
    )
    for token, replacement, label in probes:
        mutation_probe(source, token, replacement, label)
    print("PASS V26 package filesystem safety")


if __name__ == "__main__":
    main()
