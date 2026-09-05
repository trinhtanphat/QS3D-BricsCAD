#!/usr/bin/env python3
"""Require V25 held-copy destinations to reject reparse-backed ancestors before creation."""

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
        "[IO.FileShare]::Read",
        "Held V25 release input traverses a reparse-point ancestor",
    )
    for token in required_global:
        if token not in source:
            failures.append(f"source generation admission regressed; missing: {token}")

    destination_canonical = copy_body.find("$destinationFull = Get-CanonicalFullPath -LiteralPath $Destination")
    destination_guard = copy_body.find("Assert-NoReparseAncestor -LiteralPath $destinationFull")
    create_new = copy_body.find("[IO.FileMode]::CreateNew")
    exclusive = copy_body.find("[IO.FileShare]::None")
    durable_flush = copy_body.find("$output.Flush($true)")

    if min(destination_canonical, destination_guard, create_new, exclusive, durable_flush) < 0:
        failures.append("held-copy destination safety contract is incomplete")
    elif not (destination_canonical < destination_guard < create_new < durable_flush):
        failures.append("destination ancestor validation must occur after canonicalization and before CreateNew/flush")

    if "continue-on-error" in source.lower():
        failures.append("held-file helper must not hide destination safety failures")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 held-copy destination rejects reparse-backed ancestors before exclusive CreateNew")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
