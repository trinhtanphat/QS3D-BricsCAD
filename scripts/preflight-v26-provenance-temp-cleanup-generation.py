#!/usr/bin/env python3
"""Fail closed unless V26 provenance temp cleanup is bound to the generation this attempt created."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"


def validate(source: str) -> None:
    required = (
        "GetFileInformationByHandle",
        "SetFileInformationByHandle",
        "FileDispositionInfo",
        "Open-OwnedProvenanceGeneration",
        "Get-OwnedProvenanceGenerationIdentity",
        "Remove-OwnedProvenanceGeneration",
        "$tempGeneration = Open-OwnedProvenanceGeneration",
    )
    missing = [token for token in required if token not in source]
    if missing:
        raise AssertionError("V26 provenance cleanup lacks generation-owned primitive(s): " + ", ".join(missing))

    forbidden = (
        "Remove-Item -LiteralPath $tempPath",
        "Remove-Item $tempPath",
    )
    present = [token for token in forbidden if token in source]
    if present:
        raise AssertionError("V26 provenance cleanup still pathname-deletes mutable temp generation(s): " + ", ".join(present))

    create = source.index("[IO.File]::Open($tempPath, [IO.FileMode]::CreateNew")
    own = source.index("$tempGeneration = Open-OwnedProvenanceGeneration", create)
    publish = min(
        p for p in (
            source.find("[IO.File]::Replace($tempPath", own),
            source.find("[IO.File]::Move($tempPath", own),
        ) if p >= 0
    )
    cleanup = source.index("Remove-OwnedProvenanceGeneration", publish)
    if not (create < own < publish < cleanup):
        raise AssertionError("owned provenance generation must be captured before publication and used by failure cleanup")


def expect_mutation_failure(source: str, token: str) -> None:
    mutated = source.replace(token, "MUTATED_" + token)
    if mutated == source:
        raise AssertionError("mutation token missing from provenance source: " + token)
    try:
        validate(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("mutation unexpectedly passed after removing provenance cleanup primitive: " + token)


text = SOURCE.read_text(encoding="utf-8")
validate(text)
for token in (
    "GetFileInformationByHandle",
    "SetFileInformationByHandle",
    "FileDispositionInfo",
    "Open-OwnedProvenanceGeneration",
    "Get-OwnedProvenanceGenerationIdentity",
    "Remove-OwnedProvenanceGeneration",
):
    expect_mutation_failure(text, token)

print("PASS V26 provenance temp cleanup is generation-owned and cannot pathname-delete replacement generations")
