#!/usr/bin/env python3
"""Guard V25 cloud release against stale release-relevant protected-main drift."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
HELPER = ROOT / "scripts" / "assert-v25-cloud-release-main-drift.ps1"

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


def _section_to_end(text: str, start: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        raise GuardFailure(f"missing workflow section start: {start}")
    return text[start_index:]


def _require_order(section: str, before: str, after: str, label: str) -> None:
    before_index = section.find(before)
    after_index = section.find(after)
    if before_index < 0:
        raise GuardFailure(f"{label} is missing required drift admission call: {before}")
    if after_index < 0:
        raise GuardFailure(f"{label} is missing authority boundary marker: {after}")
    if before_index >= after_index:
        raise GuardFailure(f"{label} drift admission must occur before authority boundary: {after}")


def _require_helper(helper: str) -> None:
    required_fragments = (
        "param(",
        "$SourceSha",
        "$CurrentMainSha",
        "$releaseRelevantPathspecs = @(",
        '& git diff --quiet --no-ext-diff "$SourceSha..$CurrentMainSha" -- @releaseRelevantPathspecs',
        "$releaseDriftStatus = $LASTEXITCODE",
        "if ($releaseDriftStatus -eq 1)",
        "if ($releaseDriftStatus -ne 0)",
    )
    for fragment in required_fragments:
        if fragment not in helper:
            raise GuardFailure(f"release-main-drift helper is missing fail-closed contract: {fragment}")

    for path in RELEASE_RELEVANT_PATHS:
        if helper.count(f"'{path}'") != 1:
            raise GuardFailure(f"release-main-drift helper must admit release-relevant path exactly once: {path}")

    stale_block = re.search(
        r"if \(\$releaseDriftStatus -eq 1\) \{(?P<body>.*?)\n\s*\}",
        helper,
        flags=re.DOTALL,
    )
    if stale_block is None or "throw " not in stale_block.group("body"):
        raise GuardFailure("release-main-drift helper must terminally reject release-relevant drift")

    error_block = re.search(
        r"if \(\$releaseDriftStatus -ne 0\) \{(?P<body>.*?)\n\s*\}",
        helper,
        flags=re.DOTALL,
    )
    if error_block is None or "throw " not in error_block.group("body"):
        raise GuardFailure("release-main-drift helper must fail closed when git diff errors")


def validate(workflow: str, helper: str) -> None:
    _require_helper(helper)

    source_admission = _section(
        workflow,
        "- name: Classify trusted current-main release source",
        "  release:\n",
    )
    source_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $sourceSha -CurrentMainSha $currentMain"
    if source_admission.count(source_call) != 1:
        raise GuardFailure("source-admission must invoke exact release-main-drift helper once")

    release_readmission = _section(
        workflow,
        "- name: Validate cloud prerelease request",
        "      - name: Prepare exact release source commit",
    )
    release_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $sourceSha -CurrentMainSha $currentMain"
    if release_readmission.count(release_call) != 1:
        raise GuardFailure("release-job re-admission must invoke exact release-main-drift helper once")

    publish = _section_to_end(workflow, "      - name: Publish GitHub prerelease")
    pre_mutation_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $env:SOURCE_SHA -CurrentMainSha $preMutationPublishMain"
    final_publish_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $env:SOURCE_SHA -CurrentMainSha $publishMain"
    if publish.count(pre_mutation_call) != 1:
        raise GuardFailure("pre-mutation admission must invoke exact release-main-drift helper once")
    if publish.count(final_publish_call) != 1:
        raise GuardFailure("final-publication admission must invoke exact release-main-drift helper once")

    _require_order(
        publish,
        pre_mutation_call,
        "$releaseCreatedByThisRun = $false",
        "pre-mutation admission",
    )
    _require_order(
        publish,
        final_publish_call,
        "$publishBody = @{ draft = $false } | ConvertTo-Json",
        "final-publication admission",
    )


def _remove_nth(text: str, token: str, occurrence: int) -> str:
    start = -1
    for _ in range(occurrence):
        start = text.find(token, start + 1)
        if start < 0:
            raise GuardFailure(f"mutation fixture could not find occurrence {occurrence}: {token}")
    return text[:start] + text[start + len(token):]


def main() -> int:
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")
    validate(workflow, helper)

    shared_early_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $sourceSha -CurrentMainSha $currentMain"
    pre_mutation_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $env:SOURCE_SHA -CurrentMainSha $preMutationPublishMain"
    final_publish_call = ".\\scripts\\assert-v25-cloud-release-main-drift.ps1 -SourceSha $env:SOURCE_SHA -CurrentMainSha $publishMain"
    mutations = (
        (_remove_nth(workflow, shared_early_call, 1), helper),
        (_remove_nth(workflow, shared_early_call, 2), helper),
        (_remove_nth(workflow, pre_mutation_call, 1), helper),
        (_remove_nth(workflow, final_publish_call, 1), helper),
        (workflow, helper.replace("if ($releaseDriftStatus -eq 1) {", "if ($releaseDriftStatus -eq 2) {", 1)),
        (workflow, helper.replace("if ($releaseDriftStatus -ne 0) {", "if ($releaseDriftStatus -eq 0) {", 1)),
    )
    for index, (mutated_workflow, mutated_helper) in enumerate(mutations, start=1):
        try:
            validate(mutated_workflow, mutated_helper)
        except GuardFailure:
            continue
        raise GuardFailure(f"mutation self-check {index} was not rejected")

    print("PASS: V25 cloud release rejects stale release-relevant protected-main drift at all four authority boundaries.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (GuardFailure, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
