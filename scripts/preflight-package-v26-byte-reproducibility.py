#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGER = ROOT / "scripts" / "package-v26.ps1"
NORMALIZED_ZIP_ENTRY = "$entryName = $fullName.Substring($packagePrefix.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')"
ZIP_ENTRY_BACKSLASH_REJECTION = "$entryName.Contains('\\')"
MANIFEST_BACKSLASH_REJECTION = "$relativePath.Contains('\\')"
OWNED_CREATE = "$destinationStream = Open-OwnedPackageOutput -Path $destination"
NATIVE_CREATE_DECL = "private static extern SafeFileHandle CreateFileW("
NATIVE_CREATE_CALL = "SafeFileHandle handle = CreateFileW("
SET_DISPOSITION_CALL = "if (!SetFileInformationByHandle(handle, FileDispositionInfo, ref info,"
DELETE_ACCESS = "GENERIC_READ | GENERIC_WRITE | DELETE"
ARM_DELETE = "Set-PackageOutputDeleteDisposition -Stream $destinationStream -Delete $true"
ZIP_CREATE = "$archive = [IO.Compression.ZipArchive]::new($destinationStream, [IO.Compression.ZipArchiveMode]::Create, $true)"
DURABLE_FLUSH = "$destinationStream.Flush($true)"
COMMIT_DELETE = "Set-PackageOutputDeleteDisposition -Stream $destinationStream -Delete $false"


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
    require(NORMALIZED_ZIP_ENTRY in text,
            "V26 ZIP entry names must use canonical forward-slash separators.")
    require(ZIP_ENTRY_BACKSLASH_REJECTION in text,
            "V26 ZIP entry admission must reject literal backslashes after normalization.")
    require(MANIFEST_BACKSLASH_REJECTION in text,
            "V26 checksum-manifest path admission must reject literal backslashes.")
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

    # Publication safety: create the final pathname as one fresh exclusive
    # generation with DELETE access, arm deletion on that same handle before any
    # archive bytes are written, and cancel deletion only after durable flush.
    require("function Open-OwnedPackageOutput {" in text,
            "V26 deterministic ZIP publication must expose an owned output opener.")
    require("function Set-PackageOutputDeleteDisposition {" in text,
            "V26 deterministic ZIP publication must expose handle-bound rollback/commit disposition.")
    require("FILE_DISPOSITION_INFO" in text,
            "V26 deterministic ZIP publication must define native exact-generation disposition state.")
    require(NATIVE_CREATE_DECL in text and NATIVE_CREATE_CALL in text and "CREATE_NEW" in text,
            "V26 deterministic ZIP publication must create the final generation through native fresh-only CreateFileW.")
    require(SET_DISPOSITION_CALL in text,
            "V26 deterministic ZIP publication must apply native FileDispositionInfo to the owned handle.")
    require(DELETE_ACCESS in text,
            "V26 deterministic ZIP output handle must request DELETE together with read/write access before FileDispositionInfo rollback is armed.")
    require(OWNED_CREATE in text,
            "V26 deterministic ZIP must create the final destination fresh-only through the owned output handle.")
    require(ARM_DELETE in text,
            "V26 deterministic ZIP must arm exact-generation deletion before archive bytes are written.")
    require(ZIP_CREATE in text,
            "V26 deterministic ZIP must write through the owned final-destination stream.")
    require(DURABLE_FLUSH in text,
            "V26 deterministic ZIP must durably flush the owned final generation before commit.")
    require(COMMIT_DELETE in text,
            "V26 deterministic ZIP must clear deletion only after successful durable construction.")
    require("$temporary =" not in text and "[IO.File]::Move($temporary, $destination)" not in text,
            "V26 deterministic ZIP must not publish by closing/reopening a temporary pathname generation.")

    create_pos = text.find(OWNED_CREATE)
    arm_pos = text.find(ARM_DELETE, create_pos + 1)
    zip_pos = text.find(ZIP_CREATE, arm_pos + 1)
    flush_pos = text.find(DURABLE_FLUSH, zip_pos + 1)
    commit_pos = text.find(COMMIT_DELETE, flush_pos + 1)
    require(min(create_pos, arm_pos, zip_pos, flush_pos, commit_pos) >= 0 and
            create_pos < arm_pos < zip_pos < flush_pos < commit_pos,
            "V26 exact output generation must be delete-armed before ZIP writes and committed only after durable flush.")


text = PACKAGER.read_text(encoding="utf-8")
validate(text)

for marker in (
    "Add-Type -AssemblyName System.IO.Compression",
    "function Get-SourceGitCommit {",
    "function Get-SourceGitTimestampUtc {",
    "git -C $root show -s --format=%cI $Commit",
    "function New-DeterministicPackageZip {",
    "[Array]::Sort($entryNames, [StringComparer]::Ordinal)",
    NORMALIZED_ZIP_ENTRY,
    ZIP_ENTRY_BACKSLASH_REJECTION,
    MANIFEST_BACKSLASH_REJECTION,
    "[IO.Compression.CompressionLevel]::NoCompression",
    "$entry.LastWriteTime = $SourceTimestamp",
    "[Array]::Sort($commands, [StringComparer]::Ordinal)",
    "[Array]::Sort($manifestEntryNames, [StringComparer]::Ordinal)",
    "gitCommit = $gitCommit",
    "generatedUtc = $sourceTimestampUtc.ToString('o')",
    "New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
    "function Open-OwnedPackageOutput {",
    "function Set-PackageOutputDeleteDisposition {",
    NATIVE_CREATE_DECL,
    NATIVE_CREATE_CALL,
    SET_DISPOSITION_CALL,
    DELETE_ACCESS,
    OWNED_CREATE,
    ARM_DELETE,
    ZIP_CREATE,
    DURABLE_FLUSH,
    COMMIT_DELETE,
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
