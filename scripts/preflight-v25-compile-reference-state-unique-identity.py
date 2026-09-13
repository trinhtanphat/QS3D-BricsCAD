#!/usr/bin/env python3
"""Fail closed unless V25 compile-reference state proves JSON identity before parsing."""
from __future__ import annotations

import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-compile-reference-state.ps1"


def fail(message: str) -> None:
    print(f"ERROR: V25 compile-reference state unique-identity preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def object_pairs(text: str) -> list[tuple[str, object]]:
    decoder = json.JSONDecoder(object_pairs_hook=lambda pairs: pairs)
    value = decoder.decode(text)
    if not isinstance(value, list):
        fail("behavioral fixture root must be an object")
    return value


def count_equivalent(pairs: list[tuple[str, object]], name: str) -> int:
    folded = name.casefold()
    return sum(1 for key, _ in pairs if str(key).casefold() == folded)


def assert_fixture_semantics() -> None:
    fixtures = {
        "literal duplicate": '{"schemaVersion":1,"schemaVersion":2,"references":[]}',
        "case-variant duplicate": '{"schemaVersion":1,"SCHEMAVERSION":2,"references":[]}',
        "escaped-equivalent duplicate": '{"schemaVersion":1,"schema\\u0056ersion":2,"references":[]}',
    }
    for label, raw in fixtures.items():
        if count_equivalent(object_pairs(raw), "schemaVersion") != 2:
            fail(f"{label} fixture did not expose two equivalent root names")

    nested = object_pairs('{"schemaVersion":1,"references":[],"extension":{"schemaVersion":9}}')
    if count_equivalent(nested, "schemaVersion") != 1:
        fail("nested extension key must not count as duplicate root identity")

    record = object_pairs('{"name":"a","path":"p","length":1,"lastWriteUtcTicks":2,"sha256":"x","extension":{"name":"ignored"}}')
    if count_equivalent(record, "name") != 1:
        fail("nested reference extension key must not count as duplicate record identity")


def require_before(source: str, earlier: str, parse_marker: str, label: str) -> None:
    a = source.find(earlier)
    b = source.find(parse_marker)
    if a < 0:
        fail(f"missing {label} marker: {earlier}")
    if b < 0:
        fail(f"missing document parse marker: {parse_marker}")
    if a >= b:
        fail(f"{label} must execute before document JSON parse")


def main() -> None:
    assert_fixture_semantics()
    source = TARGET.read_text(encoding="utf-8")

    required = [
        "function Get-JsonPropertyOccurrenceCount",
        "function Assert-JsonPropertyOccursExactlyOnce",
        "function Get-JsonTopLevelArrayObjectTexts",
        "[StringComparison]::OrdinalIgnoreCase",
        "@('schemaVersion', 'bricsCadDir', 'references')",
        "@('name', 'path', 'length', 'lastWriteUtcTicks', 'sha256')",
        "$rawReferenceRecords",
    ]
    for marker in required:
        if marker not in source:
            fail(f"production validator is missing required uniqueness marker {marker!r}")

    # Property-name decoding legitimately uses ConvertFrom-Json inside the lexical helper.
    # Bind ordering specifically to the full-document parse, not to those token decodes.
    parse_marker = "try { $state = $raw | ConvertFrom-Json }"
    require_before(
        source,
        "Assert-JsonPropertyOccursExactlyOnce -JsonText $raw -PropertyName $propertyName",
        parse_marker,
        "root identity admission",
    )
    require_before(
        source,
        "Get-JsonTopLevelArrayObjectTexts -JsonText $raw -ArrayPropertyName 'references'",
        parse_marker,
        "reference-record extraction",
    )
    require_before(
        source,
        "Assert-JsonPropertyOccursExactlyOnce -JsonText $rawReferenceRecords[$recordIndex]",
        parse_marker,
        "reference-record identity admission",
    )

    # Ensure the old unsafe direct parse cannot appear before the admission block.
    parse_pos = source.find(parse_marker)
    raw_ready = source.find("try { $raw = $utf8.GetString($rawBytes) }")
    root_admission = source.find("foreach ($propertyName in @('schemaVersion', 'bricsCadDir', 'references'))")
    if not (0 <= raw_ready < root_admission < parse_pos):
        fail("materialized raw JSON must flow through root uniqueness admission before document parse")

    print("PASS: V25 compile-reference state requires path-aware unique JSON identity before parse")


if __name__ == "__main__":
    main()
