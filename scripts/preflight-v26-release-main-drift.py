#!/usr/bin/env python3
"""Guard V26 manual release against stale release-relevant protected-main drift."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"
HELPER = ROOT / "scripts" / "assert-v26-release-main-drift.ps1"

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
    ".github/workflows/release-v26.yml",
)


class GuardFailure(RuntimeError):
    pass


def _section(text: str, start: str, end: str | None = None) -> str:
    start_index = text.find(start)
    if start_index < 0:
        raise GuardFailure(f"missing workflow section start: {start}")
    if end is None:
        return text[start_index:]
    end_index = text.find(end, start_index + len(start))
    if end_index < 0:
        raise GuardFailure(f"missing workflow section end after {start}: {end}")
    return text[start_index:end_index]


def _require_order(section: str, before: str, after: str, label: str) -> None:
    before_index = section.find(before)
    after_index = section.find(after)
    if before_index < 0:
        raise GuardFailure(f"{label} is missing required main-drift admission: {before}")
    if after_index < 0:
        raise GuardFailure(f"{label} is missing authority boundary: {after}")
    if before_index >= after_index:
        raise GuardFailure(f"{label} main-drift admission must precede authority boundary: {after}")


def _require_helper(helper: str) -> None:
    required = (
        "param(",
        "$SourceSha",
        "$CurrentMainSha",
        "$releaseRelevantPathspecs = @(",
        '& git diff --quiet --no-ext-diff "$SourceSha..$CurrentMainSha" -- @releaseRelevantPathspecs',
        "$releaseDriftStatus = $LASTEXITCODE",
        "if ($releaseDriftStatus -eq 1)",
        "if ($releaseDriftStatus -ne 0)",
    )
    for fragment in required:
        if fragment not in helper:
            raise GuardFailure(f"V26 release-main-drift helper missing fail-closed contract: {fragment}")
    for path in RELEASE_RELEVANT_PATHS:
        if helper.count(f"'{path}'") != 1:
            raise GuardFailure(f"V26 release-main-drift helper must admit path exactly once: {path}")
    for condition in ("if ($releaseDriftStatus -eq 1) {", "if ($releaseDriftStatus -ne 0) {"):
        block = re.search(re.escape(condition) + r"(?P<body>.*?)\n\s*\}", helper, flags=re.DOTALL)
        if block is None or "throw " not in block.group("body"):
            raise GuardFailure(f"V26 release-main-drift helper must terminally fail closed: {condition}")


def _require_main_readmission(section: str, source_expression: str, label: str) -> str:
    fetch = "git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'"
    resolve = "$currentMain = ([string](& git rev-parse --verify origin/main)).Trim().ToLowerInvariant()"
    ancestor = f"git merge-base --is-ancestor {source_expression} $currentMain"
    call = f".\\scripts\\assert-v26-release-main-drift.ps1 -SourceSha {source_expression} -CurrentMainSha $currentMain"
    for fragment in (fetch, resolve, ancestor, call):
        if section.count(fragment) != 1:
            raise GuardFailure(f"{label} must contain exact protected-main readmission once: {fragment}")
    _require_order(section, fetch, resolve, label)
    _require_order(section, resolve, ancestor, label)
    _require_order(section, ancestor, call, label)
    return call


def validate(workflow: str, helper: str) -> None:
    _require_helper(helper)

    qualification = _section(
        workflow,
        "      - name: Validate manual V26 release request",
        "      - name: Manual-only CI policy gate",
    )
    qualify_call = _require_main_readmission(qualification, "$env:GITHUB_SHA", "qualification")
    _require_order(qualification, qualify_call, "$localTag = @(", "qualification")

    publication = _section(workflow, "      - name: Verify and publish held V26 candidate after job boundary")
    publish_call = _require_main_readmission(publication, "$env:GITHUB_SHA", "publication")
    _require_order(
        publication,
        publish_call,
        "$candidateIdentity = & .\\scripts\\assert-v26-candidate-identity.ps1",
        "publication",
    )


def main() -> int:
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")
    validate(workflow, helper)
    print("PASS: V26 release re-admits exact workflow SHA against release-relevant protected-main drift before qualification and publication.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (GuardFailure, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
