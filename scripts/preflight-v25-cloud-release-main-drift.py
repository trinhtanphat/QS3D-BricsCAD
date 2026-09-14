#!/usr/bin/env python3
"""Guard V25 cloud release against stale release-relevant protected-main drift."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"

RELEASE_RELEVANT_PATHS = (
    "src/",
    "tests/",
    "scripts/",
    "samples/generated/",
    "external/QS3D-Platform",
    ".gitmodules",
    "Directory.Build.props",
    "QS3D.sln",
    "QS3D.V26.sln",
    ".github/workflows/release-v25-cloud.yml",
    ".github/workflows/dispatch-v25-cloud-after-main-integration.yml",
)


class GuardFailure(RuntimeError):
    pass


def _section(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        raise GuardFailure(f"missing workflow section start: {start}")
    end_index = text.find(end, start_index + len(start))
    if end_index < 0:
        raise GuardFailure(f"missing workflow section end after {start}: {end}")
    return text[start_index:end_index]


def _require_release_drift_gate(section: str, label: str) -> None:
    required_fragments = (
        "$releaseRelevantPathspecs = @(",
        'if ($sourceSha -ne $currentMain)',
        '& git diff --quiet --no-ext-diff "$sourceSha..$currentMain" -- @releaseRelevantPathspecs',
        "$releaseDriftStatus = $LASTEXITCODE",
        "if ($releaseDriftStatus -eq 1)",
        "if ($releaseDriftStatus -ne 0)",
    )
    for fragment in required_fragments:
        if fragment not in section:
            raise GuardFailure(f"{label} is missing fail-closed release-relevant drift contract: {fragment}")

    for path in RELEASE_RELEVANT_PATHS:
        if section.count(f"'{path}'") != 1:
            raise GuardFailure(f"{label} must admit release-relevant path exactly once: {path}")

    stale_block = re.search(
        r"if \(\$releaseDriftStatus -eq 1\) \{(?P<body>.*?)\n\s*\}",
        section,
        flags=re.DOTALL,
    )
    if stale_block is None or "throw " not in stale_block.group("body"):
        raise GuardFailure(f"{label} must terminally reject release-relevant protected-main drift")

    error_block = re.search(
        r"if \(\$releaseDriftStatus -ne 0\) \{(?P<body>.*?)\n\s*\}",
        section,
        flags=re.DOTALL,
    )
    if error_block is None or "throw " not in error_block.group("body"):
        raise GuardFailure(f"{label} must fail closed when release-relevant drift inspection errors")


def validate(text: str) -> None:
    source_admission = _section(
        text,
        "- name: Classify trusted current-main release source",
        "  release:\n",
    )
    release_readmission = _section(
        text,
        "- name: Validate cloud prerelease request",
        "      - name: Prepare exact release source commit",
    )
    _require_release_drift_gate(source_admission, "source-admission")
    _require_release_drift_gate(release_readmission, "release-job re-admission")


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    validate(text)

    # Mutation self-checks: removing either semantic terminal rejection must be
    # detected so this guard cannot silently become a lexical false positive.
    mutations = (
        text.replace("if ($releaseDriftStatus -eq 1) {", "if ($releaseDriftStatus -eq 2) {", 1),
        text[::-1].replace("{ )1 qe- sutatStfirDesaeler$( fi"[::-1], "{ )2 qe- sutatStfirDesaeler$( fi"[::-1], 1)[::-1],
    )
    for index, mutation in enumerate(mutations, start=1):
        try:
            validate(mutation)
        except GuardFailure:
            continue
        raise GuardFailure(f"mutation self-check {index} was not rejected")

    print("PASS: V25 cloud release rejects stale release-relevant protected-main drift in both admission phases.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (GuardFailure, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
