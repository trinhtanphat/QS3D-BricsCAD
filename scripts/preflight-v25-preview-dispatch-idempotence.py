#!/usr/bin/env python3
"""Fail closed if automatic V25 preview dispatch can repeat one tag/source tuple."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"


def decide_dispatch(
    *,
    ordinal: int,
    source_sha: str,
    reservation_rows: tuple[tuple[int, str], ...],
    dispatch_rows: tuple[tuple[int, str], ...],
) -> str:
    """Model the durable ledger decision required before downstream dispatch."""
    if any(item_ordinal == ordinal and item_source != source_sha for item_ordinal, item_source in reservation_rows):
        return "reservation-conflict"
    if any(item_ordinal == ordinal and item_source != source_sha for item_ordinal, item_source in dispatch_rows):
        return "dispatch-conflict"
    if any(item_ordinal == ordinal and item_source == source_sha for item_ordinal, item_source in dispatch_rows):
        return "already-dispatched"
    return "dispatch"


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        "cancel-in-progress: true",
        'dispatch_prefix="QS3D_V25_PREVIEW_DISPATCH_FENCE"',
        'dispatch_regex="^${dispatch_prefix} ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{40}) run_id=([1-9][0-9]*)$"',
        "exact_dispatch_fence=0",
        "dispatch_fence_conflict=0",
        "reserved_dispatch_ordinal == committed_preview_ordinal",
        'if [[ "${reserved_dispatch_source}" == "${source_sha}" ]]; then',
        "Committed preview ordinal already has a dispatch fence for a different source SHA",
        "Exact automatic preview dispatch fence already exists",
        'dispatch_fence="${dispatch_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
        '-f body="${dispatch_fence}"',
        "Persisted automatic preview dispatch fence",
        "gh workflow run release-v25-cloud.yml",
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher lacks a durable exact tag/source dispatch fence; missing: " + ", ".join(missing)
        )

    scan_index = source.find("exact_dispatch_fence=0")
    dispatch_conflict_index = source.find("if (( dispatch_fence_conflict != 0 )); then", scan_index)
    exact_fence_index = source.find("if (( exact_dispatch_fence != 0 )); then", dispatch_conflict_index)
    reservation_write_index = source.find('-f body="${reservation}"', exact_fence_index)
    fence_write_index = source.find('-f body="${dispatch_fence}"', max(exact_fence_index, reservation_write_index))
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml", fence_write_index)
    indexes = (
        scan_index,
        dispatch_conflict_index,
        exact_fence_index,
        reservation_write_index,
        fence_write_index,
        dispatch_index,
    )
    if min(indexes) < 0 or not (
        scan_index
        < dispatch_conflict_index
        < exact_fence_index
        < reservation_write_index
        < fence_write_index
        < dispatch_index
    ):
        failures.append(
            "dispatcher must scan the durable ledger, reject dispatch conflicts, stop on an exact prior fence, reserve, persist the dispatch fence, then invoke the release workflow"
        )

    if "continue-on-error" in source:
        failures.append("dispatcher idempotence must not use continue-on-error")

    # Deterministic replacement/cancellation controls. The first admitted run may dispatch;
    # a replacement seeing its durable fence must not dispatch the same tuple again.
    ordinal = 10304
    source_sha = "a" * 40
    other_sha = "b" * 40
    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, source_sha),),
        dispatch_rows=(),
    ) != "dispatch":
        failures.append("an exact reservation without a dispatch fence must admit the first dispatch")
    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, source_sha),),
        dispatch_rows=((ordinal, source_sha),),
    ) != "already-dispatched":
        failures.append("a replacement run must stop when the exact tag/source dispatch fence already exists")
    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, source_sha),),
        dispatch_rows=((ordinal, other_sha),),
    ) != "dispatch-conflict":
        failures.append("same ordinal with a different dispatch-fence source must fail closed")
    if decide_dispatch(
        ordinal=ordinal,
        source_sha=source_sha,
        reservation_rows=((ordinal, other_sha),),
        dispatch_rows=(),
    ) != "reservation-conflict":
        failures.append("same ordinal with a different reservation source must remain fail closed")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 preview dispatch is idempotent for one committed tag/source tuple")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
