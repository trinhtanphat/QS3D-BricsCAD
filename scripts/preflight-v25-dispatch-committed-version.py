#!/usr/bin/env python3
"""Fail closed if automatic V25 dispatch can invent or ambiguously reserve a tag."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        'version_project="src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"',
        "committed_product_version=",
        "committed_preview_ordinal=",
        'tag="${series_prefix}${committed_preview_ordinal}"',
        "protected main ProductVersion must be advanced before automatic preview dispatch",
        "Refusing to reserve or dispatch an uncommitted preview tag",
        "exact_reservation=0",
        'reservation_owner_source=""',
        "reservation_owner_conflict=0",
        'dispatch_fence_owner_source=""',
        "dispatch_fence_owner_conflict=0",
        "reserved_ordinal == committed_preview_ordinal",
        'if [[ "${reserved_source}" == "${source_sha}" ]]; then',
        'reservation_owner_source="${reserved_source}"',
        "Committed preview ordinal has multiple prior owners/fences",
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        '[[ -z "${reservation_owner_source}" || -z "${dispatch_fence_owner_source}" || "${reservation_owner_source}" != "${dispatch_fence_owner_source}" ]]',
        'git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"',
        "already belongs to earlier protected-main source ${reservation_owner_source}",
        "will not reassign or duplicate-dispatch that ordinal",
        'reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher is not bound fail-closed to committed V25 ProductVersion plus immutable exact/prior ownership; missing: "
            + ", ".join(missing)
        )

    for forbidden in (
        "max_preview=",
        "preview=$((max_preview + 1))",
        'tag="${series_prefix}${preview}"',
        'reservation="${reservation_prefix} ordinal=${preview}',
        '-f release_tag="${series_prefix}${preview}"',
        '-f release_tag="${preview}"',
        "Next free preview candidate (diagnostic only)",
    ):
        if forbidden in source:
            failures.append(
                "dispatcher must not derive a next-free preview identity outside committed protected-main ProductVersion: "
                + forbidden
            )

    committed_index = source.find("committed_product_version=")
    baseline_guard = source.find("if (( committed_preview_ordinal <= published_preview_ordinal )); then", committed_index)
    reservation_loop = source.find("while IFS= read -r reservation; do", baseline_guard)
    reservation_loop_end = source.find("done < <(gh api --paginate", reservation_loop)
    multi_owner_guard = source.find(
        "if (( reservation_owner_conflict != 0 || dispatch_fence_owner_conflict != 0 )); then",
        reservation_loop_end,
    )
    prior_owner_guard = source.find(
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        multi_owner_guard,
    )
    reservation_write = source.find('-f body="${reservation}"', prior_owner_guard)
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml", reservation_write)
    indexes = (
        committed_index,
        baseline_guard,
        reservation_loop,
        reservation_loop_end,
        multi_owner_guard,
        prior_owner_guard,
        reservation_write,
        dispatch_index,
    )
    if min(indexes) < 0 or not (
        committed_index
        < baseline_guard
        < reservation_loop
        < reservation_loop_end
        < multi_owner_guard
        < prior_owner_guard
        < reservation_write
        < dispatch_index
    ):
        failures.append(
            "dispatcher must validate committed ProductVersion, scan the complete ledger, reject ambiguous ownership, reconcile a legitimate prior owner before side effects, then reserve and dispatch"
        )

    prior_owner_end = source.find("if (( exact_dispatch_fence_run_id > 0 )); then", prior_owner_guard)
    if prior_owner_end < 0:
        failures.append("dispatcher prior-owner reconciliation must finish before exact-source retry handling")
    else:
        prior_owner_block = source[prior_owner_guard:prior_owner_end]
        if "exit 0" not in prior_owner_block:
            failures.append("a legitimate earlier protected-main owner must stop the newer dispatcher neutrally")
        if "exit 1" not in prior_owner_block:
            failures.append("incomplete, mismatched, non-ancestor, or exact-plus-prior ownership must remain fail closed")
        if 'gh api --method POST' in prior_owner_block or "gh workflow run" in prior_owner_block:
            failures.append("prior-owner reconciliation must not mutate the ledger or dispatch")

    exact_guard = source.find("if (( exact_reservation == 0 )); then", prior_owner_guard)
    if exact_guard < 0 or not (prior_owner_guard < exact_guard < reservation_write):
        failures.append(
            "dispatcher must reconcile prior ownership before deciding whether to create or reuse an exact reservation"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: automatic V25 dispatch is bound to committed protected-main ProductVersion, preserves immutable prior ownership, and reserves only the exact current source"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
