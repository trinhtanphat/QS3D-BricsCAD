#!/usr/bin/env python3
"""Require V25 held-copy destinations to remain generation-pinned through publication."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"


def main() -> int:
    source = HELPER.read_text(encoding="utf-8")
    failures: list[str] = []

    copy_start = source.find("'Copy' {")
    if copy_start < 0:
        failures.append("could not locate held-file Copy operation")
        copy_body = ""
    else:
        copy_end = source.find("\n        }\n    }", copy_start)
        copy_body = source[copy_start:copy_end] if copy_end > copy_start else source[copy_start:]

    required_global = (
        "function Assert-NoReparseAncestor",
        "function Open-HeldDestinationDirectoryChain",
        "OpenDirectoryNoFollow",
        "FILE_FLAG_OPEN_REPARSE_POINT",
        "FILE_SHARE_READ | FILE_SHARE_WRITE",
        "Held destination directory is a reparse point",
    )
    for token in required_global:
        if token not in source:
            failures.append(f"destination generation admission regressed; missing: {token}")

    forbidden_global = (
        "FILE_SHARE_DELETE",
        "Remove-Item -LiteralPath $destinationFull",
    )
    for token in forbidden_global:
        if token in source:
            failures.append(f"destination generation safety regressed; forbidden: {token}")

    destination_canonical = copy_body.find("$destinationFull = Get-CanonicalFullPath -LiteralPath $Destination")
    destination_holds = copy_body.find("$destinationHolds = Open-HeldDestinationDirectoryChain -ParentPath $parent")
    source_digest = copy_body.find("$sourceDigest = Get-HeldStreamSha256 -Stream $held.Stream")
    create_new = copy_body.find("[IO.FileMode]::CreateNew")
    exclusive = copy_body.find("[IO.FileShare]::None")
    durable_flush = copy_body.find("$output.Flush($true)")
    destination_digest = copy_body.find("$destinationDigest = Get-HeldStreamSha256 -Stream $output")
    digest_compare = copy_body.find("[string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)")
    publish = copy_body.find("Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $sourceDigest")
    hold_dispose = copy_body.find("$destinationHolds[$i].Dispose()", destination_holds)

    positions = (
        destination_canonical,
        destination_holds,
        source_digest,
        create_new,
        durable_flush,
        destination_digest,
        digest_compare,
        publish,
        hold_dispose,
    )
    if min(positions) < 0 or exclusive < 0:
        failures.append("held-copy destination generation safety contract is incomplete")
    elif list(positions) != sorted(positions):
        failures.append(
            "destination generations must be pinned before CreateNew and held through durable exact-stream digest equality/publication"
        )

    if "continue-on-error" in source.lower():
        failures.append("held-file helper must not hide destination safety failures")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: V25 held-copy destination pins no-follow ancestor generations through exclusive CreateNew, "
        "durable destination-stream digest equality and digest publication"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
