#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGER = ROOT / "scripts" / "package-v26.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require("[DateTime]::UtcNow" not in text,
            "V26 package metadata must not depend on wall-clock UtcNow.")
    require("Compress-Archive" not in text,
            "V26 release ZIP must not use timestamp/order-sensitive Compress-Archive.")
    require("Add-Type -AssemblyName System.IO.Compression" in text,
            "V26 deterministic ZipArchive use must explicitly load System.IO.Compression.")
    require("function Get-SourceGitCommit {" in text,
            "V26 package provenance must bind to one exact source commit.")
    require("function Get-SourceGitTimestampUtc {" in text,
            "V26 package timestamps must derive from the exact source commit.")
    require("git -C $root show -s --format=%cI $Commit" in text,
            "V26 deterministic timestamp must be exact-commit bound.")
    require("function New-DeterministicPackageZip {" in text,
            "V26 packaging must use a deterministic ZIP writer.")
    require("[Array]::Sort($entryNames, [StringComparer]::Ordinal)" in text,
            "V26 ZIP entry order must be ordinal and culture-independent.")
    require("[IO.Compression.CompressionLevel]::NoCompression" in text,
            "V26 ZIP entries must avoid runtime-specific deflate drift.")
    require("$entry.LastWriteTime = $SourceTimestamp" in text,
            "V26 ZIP entry timestamps must be source-bound.")
    require("[Array]::Sort($commands, [StringComparer]::Ordinal)" in text,
            "V26 COMMANDS.txt ordering must be ordinal.")
    require("[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)" in text,
            "V26 SHA256SUMS.txt ordering must be ordinal.")
    require("gitCommit = $gitCommit" in text,
            "V26 PACKAGE-METADATA must record the exact source commit.")
    require("generatedUtc = $sourceTimestampUtc.ToString('o')" in text,
            "V26 PACKAGE-METADATA generatedUtc must be source-bound.")
    require("New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc" in text,
            "V26 final ZIP creation must route through the deterministic writer.")


text = PACKAGER.read_text(encoding="utf-8")
validate(text)

for marker in (
    "Add-Type -AssemblyName System.IO.Compression",
    "function Get-SourceGitCommit {",
    "function Get-SourceGitTimestampUtc {",
    "git -C $root show -s --format=%cI $Commit",
    "function New-DeterministicPackageZip {",
    "[Array]::Sort($entryNames, [StringComparer]::Ordinal)",
    "[IO.Compression.CompressionLevel]::NoCompression",
    "$entry.LastWriteTime = $SourceTimestamp",
    "[Array]::Sort($commands, [StringComparer]::Ordinal)",
    "[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)",
    "gitCommit = $gitCommit",
    "generatedUtc = $sourceTimestampUtc.ToString('o')",
    "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V26 byte-reproducible package fence")
