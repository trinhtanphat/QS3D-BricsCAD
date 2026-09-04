#!/usr/bin/env python3
"""Fail closed if automatic V25 dispatch can invent a tag not committed on main."""

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
    )
    missing = [token for token in required if token not in source]
    if missing:
        failures.append(
            "dispatcher is not bound to the committed V25 ProductVersion before release dispatch; missing: "
            + ", ".join(missing)
        )

    committed_index = source.find("committed_product_version=")
    reservation_index = source.find("reservation_issue=1441")
    dispatch_index = source.find("gh workflow run release-v25-cloud.yml")
    if committed_index < 0 or reservation_index < 0 or dispatch_index < 0:
        failures.append("dispatcher committed-version/reservation/dispatch ordering signals are incomplete")
    elif not (committed_index < reservation_index < dispatch_index):
        failures.append(
            "dispatcher must validate committed ProductVersion before reservation and workflow dispatch"
        )

    if "preview=$((max_preview + 1))" in source:
        failures.append(
            "dispatcher still derives a newer preview ordinal from reservations instead of using committed ProductVersion"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: automatic V25 dispatch is bound to committed protected-main ProductVersion")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
