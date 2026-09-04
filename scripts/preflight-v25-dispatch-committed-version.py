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
        "committed_product_version=",
        "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
        "committed_preview_ordinal=",
        'tag="${series_prefix}${committed_preview_ordinal}"',
        "protected main ProductVersion must be advanced before automatic preview dispatch",
        "Refusing to reserve or dispatch an uncommitted preview tag",
        "reservation_conflict=0",
        "reserved_ordinal == committed_preview_ordinal",
        'reserved_source != "${source_sha}"',
        "Committed preview ordinal is already reserved for a different source SHA",
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher is not bound fail-closed to one committed V25 ProductVersion/source reservation; missing: "
            + ", ".join(missing)
        )

    committed_index = source.find("committed_product_version=")
    reservation_index = source.find("reservation_issue=1441")
    conflict_index = source.find("reservation_conflict=0")
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml")
    if min(committed_index, reservation_index, conflict_index, dispatch_index) < 0:
        failures.append("dispatcher committed-version/reservation/conflict/dispatch ordering signals are incomplete")
    elif not (committed_index < reservation_index <= conflict_index < dispatch_index):
        failures.append(
            "dispatcher must validate committed ProductVersion and reservation conflicts before workflow dispatch"
        )

    if "preview=$((max_preview + 1))" in source:
        failures.append(
            "dispatcher still derives a newer preview ordinal from reservations instead of using committed ProductVersion"
        )

    conflict_guard = source.find("if (( reservation_conflict != 0 )); then")
    post_loop = source.find("done < <(gh api --paginate")
    post_reservation = source.find("if (( exact_reservation == 0 )); then")
    if min(conflict_guard, post_loop, post_reservation) < 0 or not (post_loop < conflict_guard < post_reservation):
        failures.append(
            "dispatcher must inspect the complete reservation set and reject a conflicting committed ordinal before creating/reusing a reservation"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 dispatch is bound to committed protected-main ProductVersion and one source reservation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
