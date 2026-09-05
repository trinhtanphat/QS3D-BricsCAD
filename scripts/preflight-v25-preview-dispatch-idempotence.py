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
    """Model immutable prior ownership plus durable exact-attempt recovery."""
    exact_reservation = any(
        item_ordinal == ordinal and item_source == source_sha
        for item_ordinal, item_source in reservation_rows
    )
    exact_dispatch_rows = [
        run_id
        for item_ordinal, item_source, run_id in dispatch_rows
        if item_ordinal == ordinal and item_source == source_sha
    ]
    prior_reservation_owners = {
        item_source
        for item_ordinal, item_source in reservation_rows
        if item_ordinal == ordinal and item_source != source_sha
    }
    prior_dispatch_owners = {
        item_source
        for item_ordinal, item_source, _ in dispatch_rows
        if item_ordinal == ordinal and item_source != source_sha
    }

    if len(prior_reservation_owners) > 1 or len(prior_dispatch_owners) > 1:
        return "owner-conflict"

    if prior_reservation_owners or prior_dispatch_owners:
        if exact_reservation or exact_dispatch_rows:
            return "exact-prior-conflict"
        if (
            not prior_reservation_owners
            or not prior_dispatch_owners
            or prior_reservation_owners != prior_dispatch_owners
        ):
            return "ownership-mismatch"
        return "prior-owner-neutral"

    if not exact_dispatch_rows:
        return "dispatch"

    latest_run_id = max(exact_dispatch_rows)
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
        'dispatch_fence_owner_source=""',
        "dispatch_fence_owner_conflict=0",
        'reservation_owner_source=""',
        "reservation_owner_conflict=0",
        "reserved_dispatch_ordinal == committed_preview_ordinal",
        "reserved_dispatch_run_id=$((10#${BASH_REMATCH[3]}))",
        "reserved_dispatch_run_id > exact_dispatch_fence_run_id",
        'dispatch_fence_owner_source="${reserved_dispatch_source}"',
        "Committed preview ordinal has multiple prior owners/fences",
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        'git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"',
        "will not reassign or duplicate-dispatch that ordinal",
        'prior_dispatch_run_json="$(gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${exact_dispatch_fence_run_id}")"',
        "prior_dispatch_query_status=$?",
        "Prior dispatch fence does not reference the canonical dispatcher workflow",
        "Prior dispatch fence source provenance is not admissible",
        'prior_dispatch_status="$(jq -er \'.status | strings\' <<< "${prior_dispatch_run_json}")"',
        'prior_dispatch_conclusion="$(jq -er \'.conclusion // "" | strings\' <<< "${prior_dispatch_run_json}")"',
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
            "dispatcher lacks immutable prior-owner reconciliation or recoverable exact tag/source attempt fencing; missing: "
            + ", ".join(missing)
        )

    if source.count('actions/runs/${exact_dispatch_fence_run_id}') != 1:
        failures.append("dispatcher must admit prior exact-attempt state from exactly one workflow-run API snapshot")

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
    multi_owner_index = source.find(
        "if (( reservation_owner_conflict != 0 || dispatch_fence_owner_conflict != 0 )); then",
        scan_index,
    )
    prior_owner_index = source.find(
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        multi_owner_index,
    )
    prior_snapshot_index = source.find("prior_dispatch_run_json=", prior_owner_index)
    prior_identity_index = source.find(
        "Prior dispatch fence does not reference the canonical dispatcher workflow",
        prior_snapshot_index,
    )
    prior_provenance_index = source.find(
        "Prior dispatch fence source provenance is not admissible",
        prior_identity_index,
    )
    prior_status_index = source.find("prior_dispatch_status=", prior_snapshot_index)
    active_index = source.find(
        'if [[ "${prior_dispatch_status}" != "completed" ]]; then',
        prior_provenance_index,
    )
    retry_index = source.find(
        "Dispatcher completion proves only that the dispatch request attempt ended, not that downstream publication succeeded",
        active_index,
    )
    reservation_write_index = source.find('-f body="${reservation}"', retry_index)
    fence_write_index = source.find('-f body="${dispatch_fence}"', max(retry_index, reservation_write_index))
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml", fence_write_index)
    indexes = (
        scan_index,
        multi_owner_index,
        prior_owner_index,
        prior_snapshot_index,
        prior_identity_index,
        prior_provenance_index,
        prior_status_index,
        active_index,
        retry_index,
        reservation_write_index,
        fence_write_index,
        dispatch_index,
    )
    if min(indexes) < 0 or not (
        scan_index
        < multi_owner_index
        < prior_owner_index
        < prior_snapshot_index
        < prior_status_index
        < prior_identity_index
        < prior_provenance_index
        < active_index
        < retry_index
        < reservation_write_index
        < fence_write_index
        < dispatch_index
    ):
        failures.append(
            "dispatcher must scan the ledger, reject ambiguous ownership, reconcile a legitimate prior owner before exact retry, bind one exact prior-run snapshot, stop active attempts, permit terminal recovery, reserve, fence, then dispatch"
        )

    prior_owner_end = source.find("if (( exact_dispatch_fence_run_id > 0 )); then", prior_owner_index)
    if prior_owner_end < 0:
        failures.append("could not bound prior-owner reconciliation before exact-attempt recovery")
    else:
        prior_owner_block = source[prior_owner_index:prior_owner_end]
        if "exit 0" not in prior_owner_block:
            failures.append("legitimate prior ownership must make newer main neutral")
        if 'gh api --method POST' in prior_owner_block or "gh workflow run" in prior_owner_block:
            failures.append("prior-owner reconciliation must not create a reservation/fence or dispatch")

    if "continue-on-error" in source:
        failures.append("dispatcher idempotence must not use continue-on-error")

    ordinal = 10304
    source_sha = "a" * 40
    other_sha = "b" * 40
    third_sha = "c" * 40
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
        reservation_rows=((ordinal, other_sha),),
        dispatch_rows=((ordinal, other_sha, 100),),
        run_states={},
    ) != "prior-owner-neutral":
        failures.append("one matching prior reservation/fence owner must stop a newer source neutrally")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, other_sha),),
        dispatch_rows=(),
        run_states={},
    ) != "ownership-mismatch":
        failures.append("a prior reservation without a matching dispatch fence must fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=(),
        dispatch_rows=((ordinal, other_sha, 100),),
        run_states={},
    ) != "ownership-mismatch":
        failures.append("a prior dispatch fence without a matching reservation must fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, other_sha),),
        dispatch_rows=((ordinal, third_sha, 100),),
        run_states={},
    ) != "ownership-mismatch":
        failures.append("mismatched prior reservation/fence owners must fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, other_sha), (ordinal, third_sha)),
        dispatch_rows=((ordinal, other_sha, 100),),
        run_states={},
    ) != "owner-conflict":
        failures.append("multiple prior owners for one ordinal must fail closed")

    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, source_sha), (ordinal, other_sha)),
        dispatch_rows=((ordinal, other_sha, 100),),
        run_states={},
    ) != "exact-prior-conflict":
        failures.append("an exact current-source reservation plus a prior owner must fail closed")

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

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: automatic V25 preview dispatch preserves immutable prior ownership, fences exact attempts, permits terminal retry, and prevents duplicate publication"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
