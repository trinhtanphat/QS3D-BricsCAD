#!/usr/bin/env python3
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TAG_RE = re.compile(
    r"^v(?P<major>0|[1-9][0-9]*)\."
    r"(?P<minor>0|[1-9][0-9]*)\."
    r"(?P<patch>0|[1-9][0-9]*)-preview\."
    r"(?P<ordinal>[1-9][0-9]*)$"
)


def fail(message: str) -> None:
    raise SystemExit(message)


def validate_sequence(existing_tags: list[str], requested: str) -> None:
    match = TAG_RE.fullmatch(requested)
    if not match:
        raise ValueError(f"invalid requested preview tag: {requested}")

    series = (match["major"], match["minor"], match["patch"])
    prefix = f"v{series[0]}.{series[1]}.{series[2]}-preview."
    ordinals: list[int] = []
    for tag in existing_tags:
        if not tag.startswith(prefix):
            continue
        candidate = TAG_RE.fullmatch(tag)
        if not candidate:
            raise ValueError(f"malformed matching-series tag: {tag}")
        candidate_series = (
            candidate["major"],
            candidate["minor"],
            candidate["patch"],
        )
        if candidate_series != series:
            raise ValueError(f"series mismatch after prefix selection: {tag}")
        ordinal = int(candidate["ordinal"])
        if ordinal > 2**63 - 1:
            raise ValueError(f"ordinal outside Int64 range: {tag}")
        ordinals.append(ordinal)

    requested_ordinal = int(match["ordinal"])
    if requested_ordinal > 2**63 - 1:
        raise ValueError(f"requested ordinal outside Int64 range: {requested}")
    if ordinals and max(ordinals) == 2**63 - 1:
        raise ValueError("series exhausted Int64 ordinal range")
    expected = max(ordinals, default=0) + 1
    if requested_ordinal != expected:
        raise ValueError(
            f"expected {prefix}{expected}, got {requested}"
        )


def expect_ok(existing: list[str], requested: str) -> None:
    try:
        validate_sequence(existing, requested)
    except ValueError as exc:
        fail(f"preview sequence regression: expected PASS for {requested}: {exc}")


def expect_fail(existing: list[str], requested: str) -> None:
    try:
        validate_sequence(existing, requested)
    except ValueError:
        return
    fail(f"preview sequence regression: expected FAIL for {requested}")


def main() -> int:
    helper = (ROOT / "scripts" / "validate-preview-release-sequence.ps1").read_text(
        encoding="utf-8"
    )
    prepare = (ROOT / "scripts" / "prepare-v25-cloud-release.ps1").read_text(
        encoding="utf-8"
    )
    workflow = (ROOT / ".github" / "workflows" / "release-v25-cloud.yml").read_text(
        encoding="utf-8"
    )

    required_helper_tokens = (
        "git fetch --force --tags origin",
        "git tag --list",
        "Matching-series Git tag is not canonical",
        "ReleaseTag must use the next preview ordinal for its exact series",
        "[long]::MaxValue",
    )
    for token in required_helper_tokens:
        if token not in helper:
            fail(f"preview sequence helper is missing required guard token: {token}")

    gate_call = "validate-preview-release-sequence.ps1"
    sync_call = "sync-preview-release-version.ps1"
    if gate_call not in prepare:
        fail("release preparation does not invoke the preview sequence gate")
    if sync_call not in prepare:
        fail("release preparation no longer invokes preview source synchronization")
    if prepare.index(gate_call) > prepare.index(sync_call):
        fail("preview sequence validation must run before release source mutation")

    workflow_prepare = "prepare-v25-cloud-release.ps1"
    publish_step = "- name: Publish GitHub prerelease"
    if workflow_prepare not in workflow or publish_step not in workflow:
        fail("V25 release workflow no longer exposes the guarded prepare-before-publish path")
    if workflow.index(workflow_prepare) > workflow.index(publish_step):
        fail("V25 release workflow publishes before guarded release preparation")

    expect_ok([], "v0.1.1-preview.1")
    expect_ok(["v0.1.1-preview.1"], "v0.1.1-preview.2")
    expect_ok(
        ["v0.1.1-preview.1", "v0.1.1-preview.2"],
        "v0.1.1-preview.3",
    )
    expect_fail(["v0.1.1-preview.1", "v0.1.1-preview.2"], "v0.1.1-preview.2")
    expect_fail(["v0.1.1-preview.1", "v0.1.1-preview.2"], "v0.1.1-preview.1")
    expect_fail(["v0.1.1-preview.1", "v0.1.1-preview.2"], "v0.1.1-preview.4")

    expect_ok(["v0.1.0-preview.10014"], "v0.1.0-preview.10015")
    expect_fail(["v0.1.0-preview.10014"], "v0.1.0-preview.1")
    expect_fail(["v0.1.0-preview.10014"], "v0.1.0-preview.10014")
    expect_fail(["v0.1.0-preview.10014"], "v0.1.0-preview.10016")

    expect_ok(
        ["v0.1.0-preview.10014", "v0.2.0-preview.9", "v0.1.1-beta.7"],
        "v0.1.1-preview.1",
    )
    expect_fail(["v0.1.1-preview.01"], "v0.1.1-preview.1")
    expect_fail(["v0.1.1-preview.bad"], "v0.1.1-preview.1")

    print("release preview sequence preflight: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
