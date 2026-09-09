#!/usr/bin/env python3
"""Cross-guard V26 checksum publication atomicity under generation-owned rollback semantics."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "write-v26-package-checksum.ps1"


def validate(text: str) -> None:
    required = (
        "$script:MaxChecksumBytes = 1024",
        "$originalOutputBytes = Read-BoundedChecksumBytes",
        "$publicationStarted = $false", "$publicationCommitted = $false",
        "$tempGeneration = New-OwnedChecksumGeneration",
        "$originalOutputGeneration = Open-OwnedChecksumGeneration",
        "$publicationStarted = $true",
        "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)",
        "[IO.File]::Move($tempPath, $outputFullPath)",
        "$publishedAttemptIdentity = $tempGeneration.Identity",
        "Close-OwnedChecksumGeneration -Generation $tempGeneration",
        "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
        "publishedGeneration.Identity, $publishedAttemptIdentity",
        "Published V26 checksum bytes do not match the computed canonical record.",
        "$publicationCommitted = $true",
        "if ($publicationStarted -and -not $publicationCommitted)",
        "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "rollbackPublishedGeneration.Identity, $publishedAttemptIdentity",
        "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
        "$backupProof = Open-OwnedChecksumGeneration -Path $backupPath",
        "backupProof.Identity, $originalOutputGeneration.Identity",
        "[IO.File]::Move($backupPath, $outputFullPath)",
        "$restoredOutput = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "restoredOutput.Identity, $originalOutputGeneration.Identity",
        "V26 checksum rollback unchanged-destination proof",
        "the original destination cannot be proven unchanged",
        "Remove-OwnedChecksumGeneration -Generation $originalOutputGeneration",
        "V26 checksum staging residue remains", "V26 checksum backup residue remains",
    )
    missing = [t for t in required if t not in text]
    if missing:
        raise AssertionError("missing generation-aware checksum atomicity token(s): " + ", ".join(missing))
    forbidden = (
        "Remove-Item -LiteralPath $outputFullPath", "Remove-SafeChecksumLeaf -Path $backupPath",
        "Remove-SafeChecksumLeaf -Path $tempPath", "$published = $true",
        "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue",
    )
    present = [t for t in forbidden if t in text]
    if present:
        raise AssertionError("unsafe/legacy pathname rollback token(s) returned: " + ", ".join(present))

    started = text.index("$publicationStarted = $true")
    replace = text.index("[IO.File]::Replace($tempPath", started)
    move = text.index("[IO.File]::Move($tempPath", started)
    published_identity = text.index("$publishedAttemptIdentity = $tempGeneration.Identity", max(replace, move))
    creator_close = text.index("Close-OwnedChecksumGeneration -Generation $tempGeneration", published_identity)
    pin = text.index("$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath", creator_close)
    proof = text.index("publishedGeneration.Identity, $publishedAttemptIdentity", pin)
    byte_check = text.index("Published V26 checksum bytes do not match the computed canonical record.", proof)
    commit = text.index("$publicationCommitted = $true", byte_check)
    catch = text.index("catch {", commit)
    rollback_open = text.index("$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath", catch)
    rollback_proof = text.index("rollbackPublishedGeneration.Identity, $publishedAttemptIdentity", rollback_open)
    rollback_delete = text.index("Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration", rollback_proof)
    if not (started < replace < published_identity and started < move < published_identity < creator_close < pin < proof < byte_check < commit < catch < rollback_open < rollback_proof < rollback_delete):
        raise AssertionError("invalid generation-owned publication/verification/rollback ordering")


def mutation_probe(source: str, token: str) -> None:
    if token not in source:
        raise AssertionError("mutation source token missing: " + token)
    mutated = source.replace(token, "__C05_MUTATION_REMOVED__")
    try:
        validate(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("mutation escaped generation-aware checksum atomicity guard: " + token)


def main() -> int:
    if not TARGET.is_file():
        print("ERROR: missing target", TARGET)
        return 1
    source = TARGET.read_text(encoding="utf-8")
    try:
        validate(source)
        for token in (
            "$publicationStarted = $true", "$publishedAttemptIdentity = $tempGeneration.Identity",
            "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
            "publishedGeneration.Identity, $publishedAttemptIdentity",
            "$publicationCommitted = $true",
            "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
            "rollbackPublishedGeneration.Identity, $publishedAttemptIdentity",
            "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
            "backupProof.Identity, $originalOutputGeneration.Identity",
        ):
            mutation_probe(source, token)
    except (AssertionError, ValueError) as exc:
        print("ERROR:", exc)
        return 1
    print("PASS: V26 checksum publication is verification-gated, exact-generation pinned, and generation-owned during rollback/restoration.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
