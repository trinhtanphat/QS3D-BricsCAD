#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
RESERVATION_PREFIX = "QS3D_V25_PREVIEW_RESERVATION"
RESERVATION_ISSUE = 1441
RESERVATION_RE = re.compile(
    rf"^{RESERVATION_PREFIX} ordinal=([1-9][0-9]*) source_sha=([0-9a-f]{{40}}) run_id=([1-9][0-9]*)$"
)
errors = []


def reservation_state(comments, committed_ordinal, source_sha):
    exact = False
    prior_owner = None
    owner_conflict = False
    for body in comments:
        match = RESERVATION_RE.fullmatch(body)
        if not match:
            continue
        ordinal = int(match.group(1), 10)
        owner = match.group(2)
        if ordinal != committed_ordinal:
            continue
        if owner == source_sha:
            exact = True
        elif prior_owner is None:
            prior_owner = owner
        elif prior_owner != owner:
            owner_conflict = True
    return exact, prior_owner, owner_conflict


def expect(label, actual, expected):
    if actual != expected:
        errors.append(f"{label}: expected {expected}, got {actual}")


source = "a" * 40
other = "b" * 40
third = "c" * 40
expect("empty ledger", reservation_state([], 10303, source), (False, None, False))
expect(
    "exact reservation is reusable",
    reservation_state(
        [f"{RESERVATION_PREFIX} ordinal=10303 source_sha={source} run_id=1"],
        10303,
        source,
    ),
    (True, None, False),
)
expect(
    "single earlier owner is preserved rather than overwritten",
    reservation_state(
        [f"{RESERVATION_PREFIX} ordinal=10303 source_sha={other} run_id=2"],
        10303,
        source,
    ),
    (False, other, False),
)
expect(
    "multiple earlier owners are ambiguous",
    reservation_state(
        [
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={other} run_id=2",
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={third} run_id=3",
        ],
        10303,
        source,
    ),
    (False, other, True),
)
expect(
    "exact plus earlier owner remains visible for fail-closed reconciliation",
    reservation_state(
        [
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={source} run_id=1",
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={other} run_id=2",
        ],
        10303,
        source,
    ),
    (True, other, False),
)
expect(
    "other ordinals and human notes are ignored",
    reservation_state(
        [
            "human note",
            f"{RESERVATION_PREFIX} ordinal=10302 source_sha={'d' * 40} run_id=3",
        ],
        10303,
        source,
    ),
    (False, None, False),
)

if not WORKFLOW.is_file():
    errors.append("missing V25 post-main dispatcher workflow")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "contents: read",
        "actions: write",
        "issues: write",
        f"reservation_issue={RESERVATION_ISSUE}",
        f'reservation_prefix="{RESERVATION_PREFIX}"',
        "committed_preview_ordinal=",
        "exact_reservation=0",
        'reservation_owner_source=""',
        "reservation_owner_conflict=0",
        "reserved_ordinal == committed_preview_ordinal",
        'if [[ "${reserved_source}" == "${source_sha}" ]]; then',
        'reservation_owner_source="${reserved_source}"',
        "reservation_owner_conflict=1",
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
        "Committed preview ordinal has multiple prior owners/fences",
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        '[[ -z "${reservation_owner_source}" || -z "${dispatch_fence_owner_source}" || "${reservation_owner_source}" != "${dispatch_fence_owner_source}" ]]',
        'git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"',
        "will not reassign or duplicate-dispatch that ordinal",
        'reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
        'gh api --method POST',
        '"repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
        '-f body="${reservation}"',
        'gh workflow run release-v25-cloud.yml',
        '-f source_sha="${source_sha}"',
        '-f release_tag="${tag}"',
        "cancel-in-progress: true",
    )
    for token in required:
        if token not in text:
            errors.append("dispatcher committed reservation contract missing token: " + token)

    for forbidden in (
        "contents: write",
        "max_preview=",
        "preview=$((max_preview + 1))",
        'reservation="${reservation_prefix} ordinal=${preview}',
    ):
        if forbidden in text:
            errors.append("dispatcher must not use legacy reservation allocator token: " + forbidden)

    loop_index = text.find("while IFS= read -r reservation; do")
    loop_end = text.find("done < <(gh api --paginate", loop_index)
    multi_owner_index = text.find(
        "if (( reservation_owner_conflict != 0 || dispatch_fence_owner_conflict != 0 )); then",
        loop_end,
    )
    prior_owner_index = text.find(
        'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
        multi_owner_index,
    )
    reserve_index = text.find('-f body="${reservation}"', prior_owner_index)
    dispatch_index = text.find("gh workflow run release-v25-cloud.yml", reserve_index)
    indexes = (loop_index, loop_end, multi_owner_index, prior_owner_index, reserve_index, dispatch_index)
    if min(indexes) < 0 or not (
        loop_index
        < loop_end
        < multi_owner_index
        < prior_owner_index
        < reserve_index
        < dispatch_index
    ):
        errors.append(
            "dispatcher must scan the complete ledger, reject ambiguous ownership, reconcile a legitimate prior owner before side effects, then reserve before downstream dispatch"
        )

    prior_owner_end = text.find("if (( exact_dispatch_fence_run_id > 0 )); then", prior_owner_index)
    if prior_owner_end < 0:
        errors.append("could not bound prior-owner reconciliation before exact retry handling")
    else:
        prior_owner_block = text[prior_owner_index:prior_owner_end]
        if "exit 0" not in prior_owner_block:
            errors.append("legitimate earlier reservation/fence ownership must stop newer main neutrally")
        if "exit 1" not in prior_owner_block:
            errors.append("incomplete, mismatched, non-ancestor, or exact-plus-prior ownership must fail closed")
        if 'gh api --method POST' in prior_owner_block or "gh workflow run" in prior_owner_block:
            errors.append("prior-owner reconciliation must not mutate the reservation ledger or dispatch")

print("QS3D V25 preview reservation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: automatic V25 dispatch binds reservation identity to committed ProductVersion, preserves one immutable legitimate prior owner neutrally, rejects ambiguous ownership, records a new exact reservation before dispatch, and keeps main contents read-only."
)
