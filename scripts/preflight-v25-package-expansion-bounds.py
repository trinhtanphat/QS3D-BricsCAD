#!/usr/bin/env python3
"""Guard bounded ZIP expansion work in the V25 package verifier."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VERIFIER = ROOT / "scripts" / "verify-v25-package.ps1"
TESTS = ROOT / "scripts" / "test-v25-package-verifier.ps1"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def main() -> int:
    verifier = VERIFIER.read_text(encoding="utf-8")
    tests = TESTS.read_text(encoding="utf-8")

    required_verifier = (
        "$MaxArchiveEntries = 4096",
        "$MaxEntryUncompressedBytes = 67108864",
        "$MaxTotalUncompressedBytes = 268435456",
        "V25 package archive entry count exceeds the maximum",
        "V25 package entry exceeds the maximum uncompressed size",
        "V25 package total uncompressed size exceeds the maximum",
        "$entryUncompressedBytes = [long]$entry.Length",
        "$remainingUncompressedBytes = $MaxTotalUncompressedBytes - $totalUncompressedBytes",
    )
    for token in required_verifier:
        if token not in verifier:
            fail(f"V25 package verifier expansion-bound contract missing token: {token}")

    enumerate_index = verifier.find("foreach ($entry in $archive.Entries)")
    per_entry_index = verifier.find("$entryUncompressedBytes = [long]$entry.Length", enumerate_index)
    total_index = verifier.find("$remainingUncompressedBytes = $MaxTotalUncompressedBytes - $totalUncompressedBytes", per_entry_index)
    manifest_open_index = verifier.find("$manifestStream = $manifestEntry.Open()", total_index)
    payload_hash_index = verifier.find("$entryStream = $record.Entry.Open()", manifest_open_index)
    if min(enumerate_index, per_entry_index, total_index, manifest_open_index, payload_hash_index) < 0 or not (
        enumerate_index < per_entry_index < total_index < manifest_open_index < payload_hash_index
    ):
        fail("ZIP expansion metadata must be admitted before manifest or payload streams are opened")

    required_tests = (
        "archive entry-count expansion bound",
        "single-entry uncompressed-size bound",
        "aggregate uncompressed-size bound",
    )
    for token in required_tests:
        if token not in tests:
            fail(f"V25 package verifier regression coverage missing fixture: {token}")

    for unsafe in (
        "continue-on-error",
        "$MaxArchiveEntries = [int]::MaxValue",
        "$MaxEntryUncompressedBytes = [long]::MaxValue",
        "$MaxTotalUncompressedBytes = [long]::MaxValue",
    ):
        if unsafe in verifier:
            fail(f"V25 package verifier expansion guard became fail-open: {unsafe}")

    print("PASS: V25 package verifier bounds entry count and uncompressed expansion before payload hashing")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
