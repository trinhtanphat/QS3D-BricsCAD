from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
QUALIFIER = ROOT / "scripts" / "run-local-v25-qualification.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"forbidden {label}: {token}")


def is_canonical_semver(value: str) -> bool:
    if not value or value != value.strip():
        return False
    match = re.fullmatch(
        r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
        r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
        r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?",
        value,
    )
    if not match:
        return False
    prerelease = match.group(4)
    if prerelease:
        for identifier in prerelease.split("."):
            if identifier.isdigit() and len(identifier) > 1 and identifier.startswith("0"):
                return False
    return True


def release_tag_matches(tag: str, product_version: str) -> bool:
    return is_canonical_semver(product_version) and tag == f"v{product_version}"


def main() -> None:
    package = PACKAGE.read_text(encoding="utf-8")
    qualifier = QUALIFIER.read_text(encoding="utf-8")

    require(
        package,
        "$version = $versions[0]",
        "raw project Version capture",
    )
    require(
        package,
        "Project Version must be canonical without surrounding whitespace",
        "project Version canonicality guard",
    )
    require(
        package,
        "$text = $Value",
        "strict SemVer raw-text validation",
    )
    require(
        package,
        "$Label must be canonical without surrounding whitespace.",
        "strict SemVer canonicality guard",
    )
    require(
        package,
        "[string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::Ordinal)",
        "exact RELEASE_TAG comparison",
    )
    forbid(
        package,
        "$env:RELEASE_TAG.Trim()",
        "RELEASE_TAG normalization",
    )
    forbid(
        package,
        "return $versions[0].Trim()",
        "project Version normalization",
    )
    forbid(
        package,
        "$text = $Value.Trim()",
        "SemVer normalization",
    )

    require(
        qualifier,
        "$env:RELEASE_TAG = $ReleaseTag",
        "qualification exact ReleaseTag forwarding",
    )
    forbid(
        qualifier,
        "$env:RELEASE_TAG = $ReleaseTag.Trim()",
        "qualification ReleaseTag normalization",
    )

    positive_versions = [
        "0.1.0",
        "1.2.3",
        "1.2.3-preview.1",
        "1.2.3-preview-x+build.7",
    ]
    for version in positive_versions:
        if not is_canonical_semver(version):
            raise SystemExit(f"canonical SemVer control rejected: {version!r}")
        tag = f"v{version}"
        if not release_tag_matches(tag, version):
            raise SystemExit(f"canonical release-tag control rejected: {tag!r}")

    negative_versions = [
        " 1.2.3",
        "1.2.3 ",
        "\t1.2.3",
        "1.2.3\n",
        "01.2.3",
        "1.02.3",
        "1.2.03",
        "1.2.3-01",
        "",
    ]
    for version in negative_versions:
        if is_canonical_semver(version):
            raise SystemExit(f"noncanonical SemVer control accepted: {version!r}")

    canonical_version = "1.2.3-preview.4"
    for tag in [
        " v1.2.3-preview.4",
        "v1.2.3-preview.4 ",
        "\tv1.2.3-preview.4",
        "v1.2.3-preview.4\n",
        "V1.2.3-preview.4",
        "v1.2.3-preview.5",
        "",
    ]:
        if release_tag_matches(tag, canonical_version):
            raise SystemExit(f"noncanonical/mismatched release tag accepted: {tag!r}")

    print("V25 package provenance preflight passed")


if __name__ == "__main__":
    main()
