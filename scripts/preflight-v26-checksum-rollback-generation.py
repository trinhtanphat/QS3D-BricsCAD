#!/usr/bin/env python3
"""Fail closed unless V26 checksum publication/rollback stays bound to owned generations."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_PATH = ROOT / "scripts" / "write-v26-package-checksum.ps1"


def validate_source(source: str) -> None:
    required = (
        "GetFileInformationByHandle",
        "SetFileInformationByHandle",
        "FileDispositionInfo",
        "Open-OwnedChecksumGeneration",
        "Open-PinnedChecksumGeneration",
        "Get-OwnedChecksumGenerationIdentity",
        "Remove-OwnedChecksumGeneration",
    )
    missing = [token for token in required if token not in source]
    if missing:
        raise AssertionError("V26 checksum transaction lacks generation-owned primitive(s): " + ", ".join(missing))

    forbidden = (
        "Remove-Item -LiteralPath $outputFullPath",
        "Remove-SafeChecksumLeaf -Path $tempPath",
        "Remove-SafeChecksumLeaf -Path $backupPath",
        "$publishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 checksum transaction still permits pathname/replacement race primitive(s): " + ", ".join(present))

    publication = source.index("$publicationStarted = $true")
    rollback = source.index("catch {", publication)
    owned_remove = source.rfind("Remove-OwnedChecksumGeneration")
    if publication >= rollback:
        raise AssertionError("V26 checksum publication intent must precede rollback handling")
    if owned_remove <= rollback:
        raise AssertionError("V26 checksum rollback must invoke generation-owned cleanup inside/after rollback handling")

    for token in (
        "$tempGeneration = New-OwnedChecksumGeneration",
        "$originalOutputGeneration = Open-OwnedChecksumGeneration",
        "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
        "publishedGeneration.Identity, $tempGeneration.Identity",
        "backupProof.Identity, $originalOutputGeneration.Identity",
    ):
        if token not in source:
            raise AssertionError("V26 checksum generation ownership is not bound across publication/rollback: " + token)

    pinned_open = source.index("$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath")
    published_read = source.index("$publishedText = [IO.File]::ReadAllText", pinned_open)
    committed = source.index("$publicationCommitted = $true", published_read)
    pinned_close = source.index("Close-OwnedChecksumGeneration -Generation $publishedGeneration", committed)
    if not (pinned_open < published_read < committed < pinned_close):
        raise AssertionError(
            "published checksum generation must remain replacement-pinned across pathname byte verification and publication commit"
        )


def expect_mutation_failure(source: str, token: str) -> None:
    mutated = source.replace(token, "MUTATED_" + token)
    if mutated == source:
        raise AssertionError("mutation token not present in checksum rollback source: " + token)
    try:
        validate_source(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("mutation unexpectedly passed after removing checksum transaction primitive: " + token)


source = SOURCE_PATH.read_text(encoding="utf-8")
validate_source(source)

# Mutation-lock semantic primitives independently so declaration/call-site drift cannot pass silently.
for token in (
    "GetFileInformationByHandle",
    "SetFileInformationByHandle",
    "FileDispositionInfo",
    "Open-OwnedChecksumGeneration",
    "Open-PinnedChecksumGeneration",
    "Get-OwnedChecksumGenerationIdentity",
    "Remove-OwnedChecksumGeneration",
):
    expect_mutation_failure(source, token)

print("PASS V26 checksum publication/rollback is generation-owned and replacement-pinned through commit")
