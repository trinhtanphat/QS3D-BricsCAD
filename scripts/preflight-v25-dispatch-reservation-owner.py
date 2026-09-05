#!/usr/bin/env python3
"""Guard immutable prior-owner handling in the V25 post-main dispatcher."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        'reservation_owner_source=""',
        'dispatch_fence_owner_source=""',
        'reservation_owner_conflict=0',
        'dispatch_fence_owner_conflict=0',
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        '[[ -z "${reservation_owner_source}" || -z "${dispatch_fence_owner_source}" || "${reservation_owner_source}" != "${dispatch_fence_owner_source}" ]]',
        'git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"',
        'already belongs to earlier protected-main source ${reservation_owner_source}',
        'will not reassign or duplicate-dispatch that ordinal',
        'protected main ProductVersion must advance before the next automatic preview dispatch',
    )
    for token in required:
        if token not in source:
            failures.append(f"prior-owner reservation contract missing token: {token}")

    owner_block_start = source.find(
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then'
    )
    exact_retry_start = source.find('if (( exact_dispatch_fence_run_id > 0 )); then')
    first_reservation_write = source.find('if (( exact_reservation == 0 )); then')
    downstream_dispatch = source.find('gh workflow run release-v25-cloud.yml')
    if min(owner_block_start, exact_retry_start, first_reservation_write, downstream_dispatch) < 0:
        failures.append("could not bound prior-owner reconciliation, exact retry, and durable side effects")
    elif not owner_block_start < exact_retry_start < first_reservation_write < downstream_dispatch:
        failures.append("prior-owner reconciliation must happen before exact-source retry handling and every durable side effect")
    else:
        owner_block = source[owner_block_start:exact_retry_start]
        if 'exit 0' not in owner_block:
            failures.append("legitimate earlier owner must stop the newer dispatcher neutrally")
        if 'exit 1' not in owner_block:
            failures.append("ambiguous/mismatched prior ownership must remain fail-closed")
        if 'gh api --method POST' in owner_block or 'gh workflow run' in owner_block:
            failures.append("prior-owner reconciliation must not mutate the ledger or dispatch a duplicate release")

    scan_start = source.find('while IFS= read -r reservation; do')
    scan_end = source.find('done < <(gh api --paginate', scan_start)
    if scan_start < 0 or scan_end < 0:
        failures.append("could not bound reservation ledger scan")
    else:
        scan = source[scan_start:scan_end]
        for token in (
            'reservation_owner_source="${reserved_source}"',
            'dispatch_fence_owner_source="${reserved_dispatch_source}"',
            'reservation_owner_conflict=1',
            'dispatch_fence_owner_conflict=1',
        ):
            if token not in scan:
                failures.append(f"ledger scan does not preserve unique prior owner: {token}")

    for stale in (
        'echo "Committed preview ordinal is already reserved for a different source SHA: ${committed_preview_ordinal}." >&2\n            exit 1',
        'echo "Committed preview ordinal already has a dispatch fence for a different source SHA: ${committed_preview_ordinal}." >&2\n            exit 1',
    ):
        if stale in source:
            failures.append("dispatcher still hard-fails the expected earlier-owner state instead of reconciling it")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 dispatcher preserves a prior preview owner, fails closed on ambiguous ownership, and leaves newer main neutral until ProductVersion advances")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
