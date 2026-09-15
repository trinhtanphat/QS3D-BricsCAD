#!/usr/bin/env python3
"""Fail closed unless V26 provenance success is proved on the same owned generation."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v26-candidate-provenance.ps1"

def fail(message: str) -> None:
    print(f"ERROR: V26 provenance post-publication pin preflight failed closed: {message}", file=sys.stderr)
    raise SystemExit(1)

def main() -> None:
    source = TARGET.read_text(encoding="utf-8")
    required = {
        "staging identity captured before publication": "$attemptIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration",
        "handle-owned rename": "[Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration",
        "same-handle identity after rename": "$renamedIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration",
        "same-handle byte verification": "Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration -ExpectedBytes $provenanceBytes",
        "read/delete owned handle": "GenericRead | GenericWrite | DeleteAccess",
        "owned handle denies write/delete sharing": "FileShareRead",
        "reparse-open suppression": "FileFlagOpenReparsePoint",
        "same-handle identity primitive": "GetFileInformationByHandle(handle",
        "commit after proof": "$publicationCommitted = $true",
        "close after commit": "Close-OwnedProvenanceGeneration -Generation $tempGeneration",
        "rollback through owned handle": "Remove-OwnedProvenanceGeneration -Generation $tempGeneration",
    }
    for label, token in required.items():
        if token not in source:
            fail(f"missing {label}: {token}")

    start = source.find("$tempGeneration = New-OwnedProvenanceGeneration")
    end = source.find("[pscustomobject]@{ SourceCommit", start)
    if start < 0 or end < 0:
        fail("could not isolate publication transaction")
    publication = source[start:end]
    attempt = publication.find("$attemptIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration")
    rename = publication.find("[Qs3dProvenanceGenerationNative]::RenameOwnedProvenanceGeneration")
    renamed = publication.find("$renamedIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration")
    verify = publication.find("Assert-PinnedPublishedProvenanceBytes -Generation $tempGeneration -ExpectedBytes $provenanceBytes")
    commit = publication.find("$publicationCommitted = $true")
    close = publication.find("Close-OwnedProvenanceGeneration -Generation $tempGeneration")
    rollback = publication.find("Remove-OwnedProvenanceGeneration -Generation $tempGeneration")
    if min(attempt, rename, renamed, verify, commit, close, rollback) < 0:
        fail("could not prove attempt/rename/identity/verify/commit/close/rollback lifecycle")
    if not attempt < rename < renamed < verify < commit < close:
        fail("success must capture identity, rename same handle, re-prove identity/bytes, commit, then close")
    if rollback < rename:
        fail("rollback must remain available after handle-owned rename")
    forbidden = (
        "[IO.File]::Replace($tempPath",
        "[IO.File]::Move($tempPath",
        "Open-PinnedPublishedProvenanceGeneration -Path",
        "$publishedGeneration =",
    )
    present = [token for token in forbidden if token in publication]
    if present:
        fail("publication reintroduces pathname/close-reopen authority: " + ", ".join(present))

    print("PASS: V26 provenance publication verifies exact identity and bytes on the same handle-owned generation before commit.")

if __name__ == "__main__":
    main()
