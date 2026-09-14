#!/usr/bin/env python3
"""Fail closed unless V26 provenance staging stays one owned generation through publish/cleanup."""
from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"

def validate(source: str) -> None:
    required = (
        "GetFileInformationByHandle",
        "SetFileInformationByHandle",
        "FileRenameInfo",
        "FileDispositionInfo",
        "WriteFile",
        "FlushFileBuffers",
        "CreateOwnedProvenanceGeneration",
        "RenameOwnedProvenanceGeneration",
        "New-OwnedProvenanceGeneration",
        "Get-OwnedProvenanceGenerationIdentity",
        "Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration",
        "Remove-OwnedProvenanceGeneration",
        "$tempGeneration = New-OwnedProvenanceGeneration",
    )
    missing = [token for token in required if token not in source]
    if missing:
        raise AssertionError("V26 provenance transaction lacks creator-owned primitive(s): " + ", ".join(missing))

    forbidden = (
        "Remove-Item -LiteralPath $tempPath",
        "Remove-Item $tempPath",
        "[IO.File]::Open($tempPath, [IO.FileMode]::CreateNew",
        "$tempGeneration = Open-OwnedProvenanceGeneration",
        "[IO.File]::Replace($tempPath",
        "[IO.File]::Move($tempPath",
        "Open-PinnedPublishedProvenanceGeneration -Path",
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 provenance transaction still has pathname/reopen ownership race primitive(s): " + ", ".join(present))

    create = source.index("$tempGeneration = New-OwnedProvenanceGeneration")
    rename = source.index("[Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration", create)
    identity = source.index("$renamedIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration", rename)
    verify = source.index("Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration", identity)
    commit = source.index("$publicationCommitted = $true", verify)
    cleanup = source.index("Remove-OwnedProvenanceGeneration -Generation $tempGeneration", rename)
    if not (create < rename < identity < verify < commit):
        raise AssertionError("owned provenance generation must be renamed and re-proved on the same handle before commit")
    if cleanup < rename:
        raise AssertionError("creator-owned generation must remain available for failure cleanup after rename")

    native_create = source.index("CreateOwnedProvenanceGeneration")
    native_write = source.index("WriteFile", native_create)
    native_flush = source.index("FlushFileBuffers", native_write)
    native_rename = source.index("RenameOwnedProvenanceGeneration", native_flush)
    native_remove = source.index("RemoveOwnedProvenanceGeneration", native_rename)
    if not (native_create < native_write < native_flush < native_rename < native_remove < create):
        raise AssertionError("native owned lifecycle must create/write/flush/rename/remove before PowerShell transaction use")

def expect_mutation_failure(source: str, token: str) -> None:
    mutated = source.replace(token, "<REMOVED_PROVENANCE_PRIMITIVE>")
    if mutated == source:
        raise AssertionError("mutation token missing from provenance source: " + token)
    try:
        validate(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("mutation unexpectedly passed after removing provenance transaction primitive: " + token)

text = SOURCE.read_text(encoding="utf-8")
validate(text)
for token in (
    "GetFileInformationByHandle",
    "SetFileInformationByHandle",
    "FileDispositionInfo",
    "WriteFile",
    "FlushFileBuffers",
    "CreateOwnedProvenanceGeneration",
    "RenameOwnedProvenanceGeneration",
    "$tempGeneration = New-OwnedProvenanceGeneration",
    "$renamedIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration",
    "Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration",
    "Remove-OwnedProvenanceGeneration -Generation $tempGeneration",
):
    expect_mutation_failure(text, token)

print("PASS: V26 provenance staging stays creator-owned from CREATE_NEW through handle rename, verification, and rollback cleanup")
