#!/usr/bin/env python3
"""Guard complete and identity-bound GitHub Actions state admission for V25 dispatch."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
DISPATCH_PATH = ".github/workflows/dispatch-v25-cloud-after-main-integration.yml"


def has_active_release_run(statuses: tuple[str, ...]) -> bool:
    """All pages must participate; any non-completed release run keeps the lane busy."""
    return any(status != "completed" for status in statuses)


def prior_attempt_identity_is_admissible(
    *,
    repository: str,
    expected_repository: str,
    workflow_path: str,
    event: str,
    head_branch: str,
    head_sha: str,
    fenced_source_sha: str,
    workflow_run_head_is_ancestor: bool,
) -> bool:
    if repository != expected_repository:
        return False
    if workflow_path != DISPATCH_PATH:
        return False
    if head_branch != "main":
        return False
    if event not in ("push", "workflow_dispatch", "workflow_run"):
        return False
    if event == "workflow_run":
        return workflow_run_head_is_ancestor
    return head_sha == fenced_source_sha


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    failures: list[str] = []

    required_complete_scan = (
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/actions/workflows/release-v25-cloud.yml/runs?per_page=100"',
        'select(.status != "completed")',
        "active_release_query_status=$?",
        "Could not enumerate the complete V25 cloud release-run set",
        "A V25 cloud release is already queued or running",
    )
    for token in required_complete_scan:
        if token not in source:
            failures.append(f"active release-run admission is not complete; missing: {token}")
    if 'release-v25-cloud.yml/runs?per_page=30' in source:
        failures.append("active release-run admission must not stop at the first 30 workflow runs")

    required_identity = (
        "prior_dispatch_run_json=",
        "prior_dispatch_query_status=$?",
        "'.status | strings'",
        "'.conclusion // \"\" | strings'",
        "'.path | strings'",
        "'.repository.full_name | strings'",
        "'.event | strings'",
        "'.head_branch | strings'",
        "'.head_sha | strings | ascii_downcase'",
        'dispatch-v25-cloud-after-main-integration.yml',
        "Prior dispatch fence does not reference the canonical dispatcher workflow",
        "Prior dispatch fence source provenance is not admissible",
        'git merge-base --is-ancestor',
        "Prior completed dispatch-fence run has no terminal conclusion",
    )
    for token in required_identity:
        if token not in source:
            failures.append(f"prior dispatch-fence run identity is not fully rebound; missing: {token}")

    if source.count('actions/runs/${exact_dispatch_fence_run_id}') != 1:
        failures.append("prior dispatch-fence state must come from exactly one admitted workflow-run API snapshot")

    fetch_index = source.find("prior_dispatch_run_json=")
    identity_index = source.find("Prior dispatch fence does not reference the canonical dispatcher workflow", fetch_index)
    provenance_index = source.find("Prior dispatch fence source provenance is not admissible", identity_index)
    terminal_identity_index = source.find("Prior completed dispatch-fence run has no terminal conclusion", provenance_index)
    status_index = source.find('if [[ "${prior_dispatch_status}" != "completed" ]]; then', terminal_identity_index)
    if min(fetch_index, identity_index, provenance_index, terminal_identity_index, status_index) < 0 or not (
        fetch_index < identity_index < provenance_index < terminal_identity_index < status_index
    ):
        failures.append("prior run must be fetched once, identity/provenance-admitted, then allowed to control retry state")

    statuses = ("completed",) * 30 + ("in_progress",)
    if not has_active_release_run(statuses):
        failures.append("an active release run beyond the first 30 results must keep the release lane busy")
    if has_active_release_run(("completed",) * 50):
        failures.append("an all-terminal complete release-run fleet must not be reported active")

    sha_a = "a" * 40
    sha_b = "b" * 40
    if not prior_attempt_identity_is_admissible(
        repository="trinhtanphat/QS3D-BricsCAD",
        expected_repository="trinhtanphat/QS3D-BricsCAD",
        workflow_path=DISPATCH_PATH,
        event="push",
        head_branch="main",
        head_sha=sha_a,
        fenced_source_sha=sha_a,
        workflow_run_head_is_ancestor=False,
    ):
        failures.append("exact push dispatcher identity/source must be admissible")
    if prior_attempt_identity_is_admissible(
        repository="trinhtanphat/QS3D-BricsCAD",
        expected_repository="trinhtanphat/QS3D-BricsCAD",
        workflow_path=".github/workflows/other.yml",
        event="push",
        head_branch="main",
        head_sha=sha_a,
        fenced_source_sha=sha_a,
        workflow_run_head_is_ancestor=False,
    ):
        failures.append("a fence pointing at another workflow must fail closed")
    if prior_attempt_identity_is_admissible(
        repository="trinhtanphat/QS3D-BricsCAD",
        expected_repository="trinhtanphat/QS3D-BricsCAD",
        workflow_path=DISPATCH_PATH,
        event="push",
        head_branch="main",
        head_sha=sha_b,
        fenced_source_sha=sha_a,
        workflow_run_head_is_ancestor=False,
    ):
        failures.append("a push dispatcher run from another source must fail closed")
    if not prior_attempt_identity_is_admissible(
        repository="trinhtanphat/QS3D-BricsCAD",
        expected_repository="trinhtanphat/QS3D-BricsCAD",
        workflow_path=DISPATCH_PATH,
        event="workflow_run",
        head_branch="main",
        head_sha=sha_b,
        fenced_source_sha=sha_a,
        workflow_run_head_is_ancestor=True,
    ):
        failures.append("workflow_run dispatch may bind a later current-main source only when trigger head ancestry is proven")

    if "continue-on-error" in source:
        failures.append("run-state admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 dispatch run-state admission is complete and identity-bound")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
