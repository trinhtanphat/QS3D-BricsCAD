#!/usr/bin/env python3
"""Fail closed unless V26 provenance staging is created and cleaned up as one owned generation."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"


def validate(source: str) -> None:
    required = (
        "GetFileInformationByHandle",
        "SetFileInformationByHandle",
        "FileDispositionInfo",
        "WriteFile",
        "FlushFileBuffers",
        "CreateOwnedProvenanceGeneration",
        "New-OwnedProvenanceGeneration",
        "Get-OwnedProvenanceGenerationIdentity",
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
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 provenance transaction still has pathname/reopen ownership race primitive(s): " + ", ".join(present))

    create = source.index("$tempGeneration = New-OwnedProvenanceGeneration")
    publish_positions = [
        p for p in (
            source.find("[IO.File]::Replace($tempPath", create),
            source.find("[IO.File]::Move($tempPath", create),
        ) if p >= 0
    ]
    if len(publish_positions) != 2:
        raise AssertionError("V26 provenance publication must retain both Replace and Move paths")
    publish = min(publish_positions)
    cleanup = source.index("Remove-OwnedProvenanceGeneration", max(publish_positions))
    if not (create < publish < cleanup):
        raise AssertionError("creator-owned provenance generation must exist before publication and remain available for failure cleanup")

    native_create = source.index("CreateOwnedProvenanceGeneration")
    native_write = source.index("WriteFile", native_create)
    native_flush = source.index("FlushFileBuffers", native_write)
    if not (native_create < native_write < native_flush < create):
        raise AssertionError("owned provenance creation must write and flush through the creator handle before publication")


def expect_mutation_failure(source: str, token: str) -> None:
    mutated = source.replace(token, "MUTATED_" + token)
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
    "$tempGeneration = New-OwnedProvenanceGeneration",
    "Remove-OwnedProvenanceGeneration",
):
    expect_mutation_failure(text, token)

print("PASS V26 provenance staging is creator-owned from CREATE_NEW through failure cleanup without pathname re-claim")
