#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v26.ps1"

SEMVER = re.compile(
    r"^(0|[1-9][0-9]*)\."
    r"(0|[1-9][0-9]*)\."
    r"(0|[1-9][0-9]*)"
    r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)


def fail(message: str) -> None:
    raise SystemExit(f"ERROR: V26 package provenance preflight failed: {message}")


def strict_semver(value: str) -> bool:
    if not value or value != value.strip():
        return False
    match = SEMVER.fullmatch(value)
    if not match:
        return False
    prerelease = match.group(4)
    if prerelease:
        for identifier in prerelease.split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                return False
    return True


def exact_release_tag(version: str, tag: str | None) -> bool:
    if tag is None or tag == "":
        return True
    return tag == f"v{version}"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"forbidden {label}: {token}")


def main() -> int:
    text = PACKAGE.read_text(encoding="utf-8")

    require(
        text,
        "$value = [string]$versions[0]",
        "exact project Version capture",
    )
    require(
        text,
        "[string]::Equals($value, $value.Trim(), [StringComparison]::Ordinal)",
        "project Version whitespace rejection",
    )
    require(
        text,
        "$text = [string]$Value",
        "exact strict-SemVer input capture",
    )
    require(
        text,
        "[string]::Equals($text, $text.Trim(), [StringComparison]::Ordinal)",
        "strict-SemVer whitespace rejection",
    )
    require(
        text,
        "if (-not [string]::IsNullOrEmpty($env:RELEASE_TAG))",
        "RELEASE_TAG presence check that does not normalize whitespace-only input",
    )
    require(
        text,
        "[string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::Ordinal)",
        "ordinal exact RELEASE_TAG comparison",
    )

    forbid(
        text,
        "return $versions[0].Trim()",
        "project Version normalization",
    )
    forbid(
        text,
        "$text = $Value.Trim()",
        "strict-SemVer normalization",
    )
    forbid(
        text,
        "$env:RELEASE_TAG.Trim()",
        "RELEASE_TAG normalization",
    )
    forbid(
        text,
        "IsNullOrWhiteSpace($env:RELEASE_TAG)",
        "whitespace-only RELEASE_TAG bypass",
    )

    accepted = [
        "0.1.0",
        "1.2.3-preview.10213",
        "1.2.3-rc.1+build-7",
    ]
    rejected = [
        " 1.2.3",
        "1.2.3 ",
        "\t1.2.3",
        "1.2.3\n",
        "01.2.3",
        "1.02.3",
        "1.2.03",
        "1.2.3-01",
        "v1.2.3",
        "1.2",
    ]

    for value in accepted:
        if not strict_semver(value):
            fail(f"positive strict-SemVer control was rejected: {value!r}")
        if not exact_release_tag(value, f"v{value}"):
            fail(f"canonical RELEASE_TAG control was rejected: {value!r}")

    for value in rejected:
        if strict_semver(value):
            fail(f"negative strict-SemVer mutation was accepted: {value!r}")

    version = "0.1.0-preview.10213"
    bad_tags = [
        " ",
        f" v{version}",
        f"v{version} ",
        f"v{version}\n",
        version,
        "V" + version,
    ]
    for tag in bad_tags:
        if exact_release_tag(version, tag):
            fail(f"non-canonical RELEASE_TAG mutation was accepted: {tag!r}")

    if not exact_release_tag(version, None):
        fail("unset RELEASE_TAG control was rejected")
    if not exact_release_tag(version, ""):
        fail("empty RELEASE_TAG control was rejected")

    print("V26 package provenance preflight: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
