#!/usr/bin/env python3
"""Guard final protected-main admission before irreversible V25 dispatch side effects."""

from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"


def main() -> int:
    source = DISPATCH.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        "final_main=\"$(gh api \"repos/${GITHUB_REPOSITORY}/commits/main\" --jq '.sha')\"",
        'final_main="${final_main,,}"',
        'if [[ ! "${final_main}" =~ ^[0-9a-f]{40}$ ]]; then',
        'if [[ "${final_main}" != "${source_sha}" ]]; then',
        'git merge-base --is-ancestor "${source_sha}" "${final_main}"',
        'git diff --quiet --no-ext-diff "${source_sha}..${final_main}" -- "${release_relevant_pathspecs[@]}"',
        'final_release_drift_status=$?',
        'if (( final_release_drift_status == 1 )); then',
        'if (( final_release_drift_status != 0 )); then',
        'newer release-relevant main integration owns the next release decision',
        'main advanced only through non-release paths during final admission',
    )
    for token in required:
        if token not in source:
            failures.append(f"final protected-main dispatch admission is incomplete; missing: {token}")

    prior_state = source.find('if [[ "${prior_dispatch_status}" != "completed" ]]; then')
    final_fetch = source.find('final_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main"')
    first_reservation_write = source.find('if (( exact_reservation == 0 )); then')
    dispatch_fence_write = source.find('dispatch_fence="${dispatch_prefix}')
    downstream_dispatch = source.find('gh workflow run release-v25-cloud.yml')
    if min(prior_state, final_fetch, first_reservation_write, dispatch_fence_write, downstream_dispatch) < 0:
        failures.append("could not bound final source admission and irreversible dispatcher side effects")
    elif not (prior_state < final_fetch < first_reservation_write < dispatch_fence_write < downstream_dispatch):
        failures.append("final protected-main admission must occur after prior-attempt reconciliation and before every durable reservation/fence/dispatch side effect")

    final_block_end = source.find('if (( exact_reservation == 0 )); then', final_fetch)
    if final_fetch >= 0 and final_block_end > final_fetch:
        final_block = source[final_fetch:final_block_end]
        if 'exit 0' not in final_block:
            failures.append("release-relevant final drift must stop the superseded dispatcher before side effects")
        if 'exit "${final_release_drift_status}"' not in final_block:
            failures.append("ambiguous final drift inspection must fail closed")
        if '[[ "${final_main}" != "${source_sha}" ]]' in final_block:
            pre_movement = final_block.split('[[ "${final_main}" != "${source_sha}" ]]', 1)[0]
            if 'exit 0' in pre_movement:
                failures.append("final admission must not reject all main movement before classifying release-relevant drift")

    if "continue-on-error" in source:
        failures.append("final source admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 dispatcher revalidates protected main immediately before irreversible side effects")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
