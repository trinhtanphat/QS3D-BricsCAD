#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-release-package-identity.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"preflight-v25-release-package-source-commit-canonicality: {message}")


def validate(text: str) -> None:
    required = (
        "$metadataSource = [string]$metadata.gitCommit",
        "[string]::Equals($metadataSource, $metadataSource.Trim(), [StringComparison]::Ordinal)",
        "$metadataSource -notmatch '^[0-9A-Fa-f]{40}$'",
        "$metadataSource.ToLowerInvariant(), $expectedSource, [StringComparison]::Ordinal",
    )
    for token in required:
        if token not in text:
            fail(f"missing canonical source-commit identity token: {token}")

    forbidden = (
        "$metadataSource = ([string]$metadata.gitCommit).Trim()",
        "$metadataSource = ([string]$metadata.gitCommit).Trim().ToLowerInvariant()",
    )
    for token in forbidden:
        if token in text:
            fail("PACKAGE-METADATA gitCommit must not be normalized before canonical admission")

    raw = text.find("$metadataSource = [string]$metadata.gitCommit")
    canonical = text.find("[string]::Equals($metadataSource, $metadataSource.Trim(), [StringComparison]::Ordinal)", raw)
    regex = text.find("$metadataSource -notmatch '^[0-9A-Fa-f]{40}$'", raw)
    compare = text.find("$metadataSource.ToLowerInvariant(), $expectedSource, [StringComparison]::Ordinal", raw)
    if min(raw, canonical, regex, compare) < 0 or not (raw < canonical < compare and raw < regex < compare):
        fail("raw gitCommit canonicality and 40-hex admission must precede expected-source comparison")


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
        "$metadataSource = [string]$metadata.gitCommit",
        "$metadataSource = ([string]$metadata.gitCommit).Trim()",
        "whitespace-normalized source identity",
    )
    assert_rejects_mutation(
        text,
        "[string]::Equals($metadataSource, $metadataSource.Trim(), [StringComparison]::Ordinal)",
        "$true",
        "raw canonicality check removed",
    )
    print("PASS V25 package source-commit canonical identity guard")


if __name__ == "__main__":
    main()
