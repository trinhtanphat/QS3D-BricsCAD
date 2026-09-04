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
        "reservation_conflict=0",
        "reserved_ordinal == committed_preview_ordinal",
        'if [[ "${reserved_source}" == "${source_sha}" ]]; then',
        'elif [[ "${reserved_source}" != "${source_sha}" ]]; then',
        "Committed preview ordinal is already reserved for a different source SHA",
        'reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher is not bound fail-closed to one committed V25 ProductVersion/source reservation; missing: "
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
    conflict_guard = source.find("if (( reservation_conflict != 0 )); then", reservation_loop_end)
    reservation_write = source.find('-f body="${reservation}"', conflict_guard)
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml", reservation_write)
    indexes = (
        committed_index,
        baseline_guard,
        reservation_loop,
        reservation_loop_end,
        conflict_guard,
        reservation_write,
        dispatch_index,
    )
    if min(indexes) < 0 or not (
        committed_index
        < baseline_guard
        < reservation_loop
        < reservation_loop_end
        < conflict_guard
        < reservation_write
        < dispatch_index
    ):
        failures.append(
            "dispatcher must validate committed ProductVersion against published baseline, inspect the complete reservation set, reject conflicts, reserve, then dispatch"
        )

    exact_guard = source.find("if (( exact_reservation == 0 )); then", conflict_guard)
    if exact_guard < 0 or not (conflict_guard < exact_guard < reservation_write):
        failures.append(
            "dispatcher must reject conflicting committed ordinals before deciding whether to create or reuse an exact reservation"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 dispatch is bound to committed protected-main ProductVersion and one source reservation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
