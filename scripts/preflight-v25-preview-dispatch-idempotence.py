#!/usr/bin/env python3
"""Fail closed if automatic V25 preview dispatch can repeat unsafe publication side effects."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
RELEASE = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def decide_dispatch(
    *,
    ordinal: int,
    source_sha: str,
    reservation_rows: tuple[tuple[int, str], ...],
    dispatch_rows: tuple[tuple[int, str, int], ...],
    run_states: dict[int, tuple[str, str]],
) -> str:
    """Model durable attempt fencing plus terminal recovery for one tag/source tuple."""
    if any(item_ordinal == ordinal and item_source != source_sha for item_ordinal, item_source in reservation_rows):
        return "reservation-conflict"
    if any(
        item_ordinal == ordinal and item_source != source_sha
        for item_ordinal, item_source, _ in dispatch_rows
    ):
        return "dispatch-conflict"

    exact_run_ids = [
        run_id
        for item_ordinal, item_source, run_id in dispatch_rows
        if item_ordinal == ordinal and item_source == source_sha
    ]
    if not exact_run_ids:
        return "dispatch"

    latest_run_id = max(exact_run_ids)
    state = run_states.get(latest_run_id)
    if state is None:
        return "status-unknown"
    status, _conclusion = state
    if status != "completed":
        return "attempt-active"
    return "retry"


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    release = RELEASE.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        "cancel-in-progress: true",
        "release_workflow='.github/workflows/release-v25-cloud.yml'",
        "Downstream release duplicate-admission safety contract is missing",
        'dispatch_prefix="QS3D_V25_PREVIEW_DISPATCH_FENCE"',
        'dispatch_regex="^${dispatch_prefix} ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{40}) run_id=([1-9][0-9]*)$"',
        "exact_dispatch_fence_run_id=0",
        "dispatch_fence_conflict=0",
        "reserved_dispatch_ordinal == committed_preview_ordinal",
        "reserved_dispatch_run_id=$((10#${BASH_REMATCH[3]}))",
        "reserved_dispatch_run_id > exact_dispatch_fence_run_id",
        "Committed preview ordinal already has a dispatch fence for a different source SHA",
        'prior_dispatch_status="$(gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${exact_dispatch_fence_run_id}" --jq \'.status\')"',
        'prior_dispatch_conclusion="$(gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${exact_dispatch_fence_run_id}" --jq \'.conclusion // ""\')"',
        'if [[ "${prior_dispatch_status}" != "completed" ]]; then',
        "Dispatcher completion proves only that the dispatch request attempt ended, not that downstream publication succeeded",
        "this terminal attempt does not suppress a safe retry",
        "The serialized downstream release lane and existing-tag admission prevent duplicate publication",
        'dispatch_fence="${dispatch_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
        '-f body="${dispatch_fence}"',
        "Persisted automatic preview dispatch attempt fence",
        "gh workflow run release-v25-cloud.yml",
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher lacks recoverable exact tag/source dispatch-attempt fencing; missing: " + ", ".join(missing)
        )

    for token in (
        "group: qs3d-cloud-v25-preview-release",
        "cancel-in-progress: false",
        "if (git tag --list $env:RELEASE_TAG)",
    ):
        if token not in release:
            failures.append(f"downstream release duplicate-admission contract missing: {token}")
        if token not in source:
            failures.append(f"dispatcher does not pin downstream retry safety token: {token}")

    if 'if [[ "${prior_dispatch_conclusion}" == "success" ]]; then' in source:
        failures.append(
            "dispatcher success must not be treated as durable publication success; downstream release may fail after request acceptance"
        )

    scan_index = source.find("exact_dispatch_fence_run_id=0")
    conflict_index = source.find("if (( dispatch_fence_conflict != 0 )); then", scan_index)
    prior_status_index = source.find("prior_dispatch_status=", conflict_index)
    active_index = source.find('if [[ "${prior_dispatch_status}" != "completed" ]]; then', prior_status_index)
    retry_index = source.find(
        "Dispatcher completion proves only that the dispatch request attempt ended, not that downstream publication succeeded",
        active_index,
    )
    reservation_write_index = source.find('-f body="${reservation}"', retry_index)
    fence_write_index = source.find('-f body="${dispatch_fence}"', max(retry_index, reservation_write_index))
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml", fence_write_index)
    indexes = (
        scan_index,
        conflict_index,
        prior_status_index,
        active_index,
        retry_index,
        reservation_write_index,
        fence_write_index,
        dispatch_index,
    )
    if min(indexes) < 0 or not (
        scan_index
        < conflict_index
        < prior_status_index
        < active_index
        < retry_index
        < reservation_write_index
        < fence_write_index
        < dispatch_index
    ):
        failures.append(
            "dispatcher must scan the ledger, reject conflicts, inspect the latest exact attempt, stop active attempts, permit terminal recovery, reserve, fence the new attempt, then dispatch"
        )

    if "continue-on-error" in source:
        failures.append("dispatcher idempotence must not use continue-on-error")

    ordinal = 10304
    source_sha = "a" * 40
    other_sha = "b" * 40
    exact_reservation = ((ordinal, source_sha),)

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=exact_reservation,
        dispatch_rows=(),
        run_states={},
    ) != "dispatch":
        failures.append("an exact reservation without a dispatch-attempt fence must admit the first dispatch")

    for status in ("queued", "in_progress"):
        if decide_dispatch(
            ordinal=ordinal,
            source_sha=source_sha,
            reservation_rows=exact_reservation,
            dispatch_rows=((ordinal, source_sha, 100),),
            run_states={100: (status, "")},
        ) != "attempt-active":
            failures.append(f"a replacement must stop while the latest exact dispatch attempt is {status}")

    for conclusion in ("success", "cancelled", "failure", "timed_out"):
        if decide_dispatch(
            ordinal=ordinal,
            source_sha=source_sha,
            reservation_rows=exact_reservation,
            dispatch_rows=((ordinal, source_sha, 100),),
            run_states={100: ("completed", conclusion)},
        ) != "retry":
            failures.append(
                f"a terminal dispatcher attempt with conclusion {conclusion} must remain retryable because dispatcher completion is not downstream publication evidence"
            )

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=exact_reservation,
        dispatch_rows=((ordinal, source_sha, 100), (ordinal, source_sha, 101)),
        run_states={100: ("completed", "cancelled"), 101: ("in_progress", "")},
    ) != "attempt-active":
        failures.append("the newest exact attempt fence must govern recovery when an older attempt is terminal")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=exact_reservation,
        dispatch_rows=((ordinal, source_sha, 100), (ordinal, source_sha, 101)),
        run_states={100: ("in_progress", ""), 101: ("completed", "success")},
    ) != "retry":
        failures.append("the newest exact terminal attempt must permit recovery even if an older stale attempt record appears active")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=exact_reservation,
        dispatch_rows=((ordinal, source_sha, 100),),
        run_states={},
    ) != "status-unknown":
        failures.append("missing prior-run status must remain fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=exact_reservation,
        dispatch_rows=((ordinal, other_sha, 100),),
        run_states={100: ("completed", "failure")},
    ) != "dispatch-conflict":
        failures.append("same ordinal with a different dispatch-fence source must fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, other_sha),),
        dispatch_rows=(),
        run_states={},
    ) != "reservation-conflict":
        failures.append("same ordinal with a different reservation source must remain fail closed")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 preview dispatch is attempt-fenced, terminal-retryable, and duplicate-publication safe")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
