#!/usr/bin/env python3
"""Fail closed unless V26 provenance success is pinned to exact published bytes."""

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
        "creator closed before restrictive pin": "Close-OwnedProvenanceGeneration -Generation $tempGeneration",
        "restrictive pin open": "Open-PinnedPublishedProvenanceGeneration -Path $outputFull -ExpectedIdentity $attemptIdentity",
        "read/delete access without write": "GenericRead | DeleteAccess",
        "pin denies write/delete sharing": "FileShareRead",
        "reparse-open suppression": "FileFlagOpenReparsePoint",
        "same-handle identity proof": "GetFileInformationByHandle(handle",
        "held byte verification": "Assert-PinnedPublishedProvenanceBytes -Generation $publishedGeneration -ExpectedBytes $provenanceBytes",
        "commit after proof": "$publicationCommitted = $true",
    }
    for label, token in required.items():
        if token not in source:
            fail(f"missing {label}: {token}")

    publish_replace = source.find("[IO.File]::Replace($tempPath, $outputFull, $null)")
    publish_move = source.find("[IO.File]::Move($tempPath, $outputFull)")
    identity = source.find("$attemptIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration")
    close = source.find("Close-OwnedProvenanceGeneration -Generation $tempGeneration", max(publish_replace, publish_move))
    pin = source.find("Open-PinnedPublishedProvenanceGeneration -Path $outputFull -ExpectedIdentity $attemptIdentity", close)
    verify = source.find("Assert-PinnedPublishedProvenanceBytes -Generation $publishedGeneration -ExpectedBytes $provenanceBytes", pin)
    commit = source.find("$publicationCommitted = $true", verify)
    if min(publish_replace, publish_move, identity, close, pin, verify, commit) < 0:
        fail("could not prove publish/close/pin/verify/commit lifecycle")
    if not identity < min(publish_replace, publish_move):
        fail("attempt identity must be captured before File.Replace/Move")
    if not max(publish_replace, publish_move) < close < pin < verify < commit:
        fail("success must close writable creator, pin exact output, verify bytes, then commit")

    old = "[IO.File]::Move($tempPath, $outputFull)\n        }\n        $publicationCommitted = $true"
    if old in source:
        fail("publication still commits immediately after pathname publish")

    print("PASS: V26 provenance publication is exact-generation pinned and byte-verified before commit.")


if __name__ == "__main__":
    main()
