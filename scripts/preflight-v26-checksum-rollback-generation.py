#!/usr/bin/env python3
"""Fail closed unless V26 checksum publication/rollback stays bound to owned generations."""
from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_PATH = ROOT / "scripts" / "write-v26-package-checksum.ps1"
PINNED_NATIVE_OPEN = "return OpenCore(path, GenericRead | FileReadAttributes, OpenExisting, FileShareRead);"


def validate_source(source: str) -> None:
    required = (
        "GetFileInformationByHandle", "SetFileInformationByHandle", "FileDispositionInfo",
        "Open-OwnedChecksumGeneration", "Open-PinnedChecksumGeneration",
        "Get-OwnedChecksumGenerationIdentity", "Remove-OwnedChecksumGeneration", PINNED_NATIVE_OPEN,
        "$tempGeneration = New-OwnedChecksumGeneration",
        "$originalOutputGeneration = Open-OwnedChecksumGeneration",
        "$publishedAttemptIdentity = $tempGeneration.Identity",
        "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
        "publishedGeneration.Identity, $publishedAttemptIdentity",
        "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "rollbackPublishedGeneration.Identity, $publishedAttemptIdentity",
        "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
        "backupProof.Identity, $originalOutputGeneration.Identity",
    )
    missing = [token for token in required if token not in source]
    if missing:
        raise AssertionError("V26 checksum transaction lacks generation-owned/pinned primitive(s): " + ", ".join(missing))

    forbidden = (
        "Remove-Item -LiteralPath $outputFullPath", "Remove-SafeChecksumLeaf -Path $tempPath",
        "Remove-SafeChecksumLeaf -Path $backupPath",
        "$publishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "return OpenCore(path, GenericRead | FileReadAttributes, OpenExisting, FileShareRead | FileShareWrite);",
        "return OpenCore(path, GenericRead | FileReadAttributes, OpenExisting, FileShareRead | FileShareDelete);",
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 checksum transaction still permits pathname/replacement/write race primitive(s): " + ", ".join(present))

    publication = source.index("$publicationStarted = $true")
    rollback = source.index("catch {", publication)
    publish_calls = [source.find("[IO.File]::Replace($tempPath", publication), source.find("[IO.File]::Move($tempPath", publication)]
    if min(publish_calls) < 0:
        raise AssertionError("V26 checksum publication must retain Replace and Move paths")
    publish_done = max(publish_calls)
    identity_capture = source.index("$publishedAttemptIdentity = $tempGeneration.Identity", publish_done)
    creator_close = source.index("Close-OwnedChecksumGeneration -Generation $tempGeneration", identity_capture)
    creator_clear = source.index("$tempGeneration = $null", creator_close)
    pinned_open = source.index("$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath", creator_clear)
    pinned_compare = source.index("publishedGeneration.Identity, $publishedAttemptIdentity", pinned_open)
    published_read = source.index("$publishedText = [IO.File]::ReadAllText", pinned_compare)
    committed = source.index("$publicationCommitted = $true", published_read)
    pinned_close = source.index("Close-OwnedChecksumGeneration -Generation $publishedGeneration", committed)
    if not (publication < publish_done < identity_capture < creator_close < creator_clear < pinned_open < pinned_compare < published_read < committed < pinned_close < rollback):
        raise AssertionError("published checksum lifecycle must close the write/delete creator before read-only pinning and keep the exact-generation pin through byte verification and commit")

    rollback_reopen = source.index("$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath", rollback)
    rollback_compare = source.index("rollbackPublishedGeneration.Identity, $publishedAttemptIdentity", rollback_reopen)
    rollback_remove = source.index("Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration", rollback_compare)
    if not (rollback < rollback_reopen < rollback_compare < rollback_remove):
        raise AssertionError("rollback must identity-prove the pathname before handle-bound deletion of the published attempt generation")


def expect_mutation_failure(source: str, token: str) -> None:
    if token not in source:
        raise AssertionError("mutation token not present in checksum rollback source: " + token)
    mutated = source.replace(token, "__C05_MUTATION_REMOVED__")
    try:
        validate_source(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("mutation unexpectedly passed after removing checksum transaction primitive: " + token)


source = SOURCE_PATH.read_text(encoding="utf-8")
validate_source(source)
for token in (
    "GetFileInformationByHandle", "SetFileInformationByHandle", "FileDispositionInfo",
    "Open-OwnedChecksumGeneration", "Open-PinnedChecksumGeneration",
    "Get-OwnedChecksumGenerationIdentity", "Remove-OwnedChecksumGeneration", PINNED_NATIVE_OPEN,
    "$publishedAttemptIdentity = $tempGeneration.Identity",
    "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
    "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
):
    expect_mutation_failure(source, token)
print("PASS V26 checksum publication/rollback is generation-owned, read-only pinned through commit, identity-bound on rollback, and mutation-locked")
