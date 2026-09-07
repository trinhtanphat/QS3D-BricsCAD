#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGER = ROOT / "scripts" / "package-v25.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require("[DateTime]::UtcNow" not in text,
            "V25 package metadata must not depend on wall-clock UtcNow.")
    require("Compress-Archive" not in text,
            "V25 release ZIP must not use timestamp/order-sensitive Compress-Archive.")
    require("Add-Type -AssemblyName System.IO.Compression" in text,
            "Direct ZipArchive use must explicitly load System.IO.Compression on Windows PowerShell 5.1.")
    require("function Get-SourceGitTimestampUtc {" in text,
            "V25 packaging must derive one deterministic timestamp from the exact source commit.")
    require("git -C $root show -s --format=%cI $Commit" in text,
            "Deterministic package timestamp must be bound to the exact source commit.")
    require("function New-DeterministicPackageZip {" in text,
            "V25 packaging must use an explicit deterministic ZIP writer.")
    require("[IO.Compression.ZipArchiveMode]::Create" in text,
            "Deterministic ZIP writer must create entries through ZipArchive.")
    require("$entry.LastWriteTime = $SourceTimestamp" in text,
            "Every ZIP entry must receive the source-bound deterministic timestamp.")
    require("[Array]::Sort($entryNames, [StringComparer]::Ordinal)" in text,
            "ZIP entry names must be sorted with ordinal semantics, independent of host culture.")
    require(".Replace([IO.Path]::DirectorySeparatorChar, '/')" in text,
            "ZIP entry names must be normalized to forward slashes across Windows/Linux path semantics.")
    require("[IO.Compression.CompressionLevel]::NoCompression" in text,
            "ZIP entries must use stored bytes to avoid runtime-specific deflate output drift.")
    require("[Array]::Sort($commands, [StringComparer]::Ordinal)" in text,
            "COMMANDS.txt ordering must be ordinal and culture-independent.")
    require("[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)" in text,
            "SHA256SUMS.txt ordering must be ordinal and culture-independent.")
    require("New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc" in text,
            "Release packaging must route final ZIP creation through the deterministic writer.")
    require("generatedUtc = $sourceTimestampUtc.ToString('o')" in text,
            "PACKAGE-METADATA generatedUtc must use the source-bound timestamp.")


text = PACKAGER.read_text(encoding="utf-8")
validate(text)

for marker in (
    "Add-Type -AssemblyName System.IO.Compression",
    "function Get-SourceGitTimestampUtc {",
    "git -C $root show -s --format=%cI $Commit",
    "function New-DeterministicPackageZip {",
    "$entry.LastWriteTime = $SourceTimestamp",
    "[Array]::Sort($entryNames, [StringComparer]::Ordinal)",
    ".Replace([IO.Path]::DirectorySeparatorChar, '/')",
    "[IO.Compression.CompressionLevel]::NoCompression",
    "[Array]::Sort($commands, [StringComparer]::Ordinal)",
    "[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)",
    "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
    "generatedUtc = $sourceTimestampUtc.ToString('o')",
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 byte-reproducible package fence")
