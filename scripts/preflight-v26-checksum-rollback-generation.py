#!/usr/bin/env python3
"""Fail closed unless V26 checksum rollback is generation-owned rather than pathname-owned."""
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
        "Get-OwnedChecksumGenerationIdentity",
        "Remove-OwnedChecksumGeneration",
    )
    missing = [token for token in required if token not in source]
    if missing:
        raise AssertionError("V26 checksum rollback lacks generation-owned primitive(s): " + ", ".join(missing))

    forbidden = (
        "Remove-Item -LiteralPath $outputFullPath",
        "Remove-SafeChecksumLeaf -Path $tempPath",
        "Remove-SafeChecksumLeaf -Path $backupPath",
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 checksum rollback/cleanup still pathname-deletes mutable generations: " + ", ".join(present))

    publication = source.index("$publicationStarted = $true")
    rollback = source.index("catch {")
    owned_remove = source.rfind("Remove-OwnedChecksumGeneration")
    if publication >= rollback:
        raise AssertionError("V26 checksum publication intent must precede rollback handling")
    if owned_remove <= rollback:
        raise AssertionError("V26 checksum rollback must invoke generation-owned cleanup inside/after rollback handling")


source = SOURCE_PATH.read_text(encoding="utf-8")
validate_source(source)

# Mutation-lock the semantic primitives independently so declaration/call-site drift cannot pass silently.
for token in (
    "GetFileInformationByHandle",
    "SetFileInformationByHandle",
    "FileDispositionInfo",
    "Open-OwnedChecksumGeneration",
    "Get-OwnedChecksumGenerationIdentity",
    "Remove-OwnedChecksumGeneration",
):
    mutated = source.replace(token, "MUTATED_" + token, 1)
    try:
        validate_source(mutated)
    except AssertionError:
        pass
    else:
        raise AssertionError("mutation unexpectedly passed after removing checksum rollback primitive: " + token)

print("PASS V26 checksum rollback is generation-owned and rejects pathname-delete regression")
