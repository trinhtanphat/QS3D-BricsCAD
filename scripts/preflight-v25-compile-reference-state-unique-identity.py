#!/usr/bin/env python3
"""Fail closed unless V25 compile-reference state proves JSON identity before parsing.

This is intentionally source-oriented: the production validator is PowerShell/Windows-facing,
while this guard is auto-discovered on Linux and Windows CI. Behavioral duplicate fixtures are
kept here so later refactors cannot regress literal/case/escaped-name admission semantics.
"""
from __future__ import annotations

import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-compile-reference-state.ps1"


def fail(message: str) -> None:
    print(f"ERROR: V25 compile-reference state unique-identity preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def decoded_name(raw: str) -> str:
    return json.loads('"' + raw + '"')


def top_level_property_names(text: str) -> list[str]:
    decoder = json.JSONDecoder(object_pairs_hook=lambda pairs: pairs)
    value = decoder.decode(text)
    if not isinstance(value, list):
        fail("test fixture root must decode through object_pairs_hook")
    return [str(k) for k, _ in value]


def assert_fixture_semantics() -> None:
    fixtures = {
        'literal duplicate': '{"schemaVersion":1,"schemaVersion":2,"references":[]}',
        'case-variant duplicate': '{"schemaVersion":1,"SCHEMAVERSION":2,"references":[]}',
        'escaped-equivalent duplicate': '{"schemaVersion":1,"schema\\u0056ersion":2,"references":[]}',
    }
    for label, raw in fixtures.items():
        names = top_level_property_names(raw)
        count = sum(1 for name in names if name.casefold() == "schemaversion")
        if count != 2:
            fail(f"{label} behavioral fixture did not expose two equivalent names")

    nested = '{"schemaVersion":1,"references":[],"extension":{"schemaVersion":9}}'
    names = top_level_property_names(nested)
    if sum(1 for name in names if name.casefold() == "schemaversion") != 1:
        fail("nested extension key must not count as duplicate root identity")


def require_before(source: str, earlier: str, later: str, label: str) -> None:
    a = source.find(earlier)
    b = source.find(later)
    if a < 0:
        fail(f"missing {label} marker: {earlier}")
    if b < 0:
        fail(f"missing parse marker: {later}")
    if a >= b:
        fail(f"{label} must execute before JSON parse")


def main() -> None:
    assert_fixture_semantics()
    source = TARGET.read_text(encoding="utf-8")

    # Contract: production must keep raw materialized JSON available and perform path-aware,
    # decoded-name, case-insensitive uniqueness admission before ConvertFrom-Json.
    required = [
        "Assert-JsonPropertyOccursExactlyOnce",
        "Get-JsonTopLevelArrayObjectTexts",
        "OrdinalIgnoreCase",
        "schemaVersion",
        "bricsCadDir",
        "references",
        "lastWriteUtcTicks",
        "sha256",
    ]
    for marker in required:
        if marker not in source:
            fail(f"production validator is missing required uniqueness marker {marker!r}")

    parse_marker = "ConvertFrom-Json"
    require_before(source, "Assert-JsonPropertyOccursExactlyOnce", parse_marker, "raw root identity admission")
    require_before(source, "Get-JsonTopLevelArrayObjectTexts", parse_marker, "reference-record extraction")

    # Reject the old unsafe shape where Get-MaterializedJson itself parses before callers can
    # prove uniqueness over raw text.
    materialized = re.search(r"function\s+Get-MaterializedJson\b(?P<body>.*?)(?=\nfunction\s+|\Z)", source, re.S | re.I)
    if not materialized:
        fail("cannot locate Get-MaterializedJson")
    if "ConvertFrom-Json" in materialized.group("body"):
        fail("Get-MaterializedJson still parses JSON before identity uniqueness admission")

    print("PASS: V25 compile-reference state requires path-aware unique JSON identity before parse")


if __name__ == "__main__":
    main()
