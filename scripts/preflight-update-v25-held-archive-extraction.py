#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UPDATER = ROOT / "scripts" / "update-v25.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def function_block(text: str, name: str) -> str:
    marker = f"function {name} {{\n"
    start = text.find(marker)
    require(start >= 0, f"Updater must define {name}.")
    next_function = text.find("\nfunction ", start + len(marker))
    require(next_function > start, f"Updater {name} function must be lexically bounded.")
    return text[start:next_function]


text = UPDATER.read_text(encoding="utf-8")
block = function_block(text, "Expand-VerifiedHeldArchive")

require("[IO.File]::Open($ZipPath" in block, "Held archive extraction must open the admitted ZIP explicitly.")
require("[IO.FileShare]::Read" in block, "Held archive extraction must deny write/delete sharing while admitted.")
require("ComputeHash($zipStream)" in block, "Manifest SHA-256 must be computed from the held ZIP stream.")
require("[IO.Compression.ZipArchive]::new($zipStream" in block, "Archive validation/extraction must consume the same held ZIP stream.")
require("$zipStream.Position = 0" in block, "Held ZIP stream must be rewound between digest and ZIP consumption.")
require("HashSet[string]" in block and "OrdinalIgnoreCase" in block, "Extraction must reject case-insensitive duplicate destinations.")
require("CreateNew" in block, "Held extraction must not overwrite an already-created destination leaf.")
require("$entry.Open()" in block, "Held extraction must stream entry bytes from the admitted ZipArchive.")
require("Expand-Archive" not in text, "Updater must not reopen the admitted ZIP via Expand-Archive.")
require("Get-FileHash -LiteralPath $zipPath" not in text, "Updater must not hash the ZIP through a separate pathname reopen.")

call_marker = "Expand-VerifiedHeldArchive -ZipPath $zipPath"
require(text.count(call_marker) == 1, "Updater must invoke held archive extraction exactly once for the downloaded ZIP.")
call_start = text.index(call_marker)
call_window = text[call_start:call_start + 700]
for marker in (
    "-ExpectedSha256 $expectedZipHash",
    "-DestinationRoot $extractRoot",
    "-MaxExpandedBytes $maxExpandedBytes",
    "-MaxEntries $MaxArchiveEntries",
):
    require(marker in call_window, f"Held archive invocation is missing {marker}.")

# Mutation probes: each essential generation-binding marker must be independently required.
for marker in (
    "ComputeHash($zipStream)",
    "[IO.Compression.ZipArchive]::new($zipStream",
    "[IO.FileShare]::Read",
    "CreateNew",
    "$entry.Open()",
):
    mutated = text.replace(marker, marker + "_BROKEN", 1)
    mutated_block = function_block(mutated, "Expand-VerifiedHeldArchive")
    if marker in mutated_block:
        raise SystemExit(f"Mutation probe unexpectedly retained required marker: {marker}")

print("PASS V25 updater held archive generation extraction fence")
