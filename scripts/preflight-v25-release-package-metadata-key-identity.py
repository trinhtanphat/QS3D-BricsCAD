#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-release-package-identity.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"preflight-v25-release-package-metadata-key-identity: {message}")


def validate(text: str) -> None:
    required = (
        "function Assert-UniqueTopLevelJsonPropertyNames",
        "[StringComparer]::OrdinalIgnoreCase",
        "Duplicate top-level JSON property name",
        "V25 package metadata root must be one JSON object.",
        "Assert-UniqueTopLevelJsonPropertyNames -Text $text",
        "$metadata = $text | ConvertFrom-Json -ErrorAction Stop",
    )
    for token in required:
        if token not in text:
            fail(f"missing fail-closed metadata-key contract token: {token}")

    unique_call = text.find("Assert-UniqueTopLevelJsonPropertyNames -Text $text")
    materialize = text.find("$metadata = $text | ConvertFrom-Json -ErrorAction Stop")
    if unique_call < 0 or materialize < 0 or unique_call >= materialize:
        fail("unique top-level metadata-key admission must precede ConvertFrom-Json materialization")

    # The lexical scanner must decode a candidate JSON property token before
    # inserting it into an ordinal-ignore-case set, otherwise escaped spellings
    # such as product\\u0056ersion can bypass duplicate-key detection.
    if "ConvertFrom-Json -ErrorAction Stop" not in text[text.find("function Assert-UniqueTopLevelJsonPropertyNames"):unique_call]:
        fail("metadata-key scanner must JSON-decode property tokens before uniqueness comparison")

    if "[StringComparer]::Ordinal" in text and "[StringComparer]::OrdinalIgnoreCase" not in text:
        fail("metadata-key duplicate identity must not be case-sensitive")


def assert_rejects_mutation(text: str, old: str, new: str, label: str) -> None:
    if old not in text:
        fail(f"guard self-check could not find mutation anchor: {label}")
    mutant = text.replace(old, new, 1)
    try:
        validate(mutant)
    except SystemExit:
        return
    fail(f"mutation unexpectedly passed: {label}")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    validate(text)
    assert_rejects_mutation(
        text,
        "[StringComparer]::OrdinalIgnoreCase",
        "[StringComparer]::Ordinal",
        "case-sensitive duplicate-key admission",
    )
    assert_rejects_mutation(
        text,
        "Assert-UniqueTopLevelJsonPropertyNames -Text $text",
        "# unique top-level metadata-key admission removed",
        "unique-key admission removed",
    )
    print("PASS V25 package metadata unique top-level identity-key guard")


if __name__ == "__main__":
    main()
