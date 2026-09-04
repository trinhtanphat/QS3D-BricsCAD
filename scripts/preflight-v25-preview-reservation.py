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
    conflict = False
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
        else:
            conflict = True
    return exact, conflict


def expect(label, actual, expected):
    if actual != expected:
        errors.append(f"{label}: expected {expected}, got {actual}")


source = "a" * 40
expect("empty ledger", reservation_state([], 10303, source), (False, False))
expect(
    "exact reservation is reusable",
    reservation_state(
        [f"{RESERVATION_PREFIX} ordinal=10303 source_sha={source} run_id=1"],
        10303,
        source,
    ),
    (True, False),
)
expect(
    "same ordinal different source conflicts",
    reservation_state(
        [f"{RESERVATION_PREFIX} ordinal=10303 source_sha={'b' * 40} run_id=2"],
        10303,
        source,
    ),
    (False, True),
)
expect(
    "complete ledger preserves conflict even with exact entry",
    reservation_state(
        [
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={source} run_id=1",
            f"{RESERVATION_PREFIX} ordinal=10303 source_sha={'c' * 40} run_id=2",
        ],
        10303,
        source,
    ),
    (True, True),
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
    (False, False),
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
        "reservation_conflict=0",
        "reserved_ordinal == committed_preview_ordinal",
        'reserved_source == "${source_sha}"',
        'reserved_source != "${source_sha}"',
        'gh api --paginate "repos/${GITHUB_REPOSITORY}/issues/${reservation_issue}/comments"',
        'if (( reservation_conflict != 0 )); then',
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
    conflict_index = text.find("if (( reservation_conflict != 0 )); then", loop_end)
    reserve_index = text.find('-f body="${reservation}"', conflict_index)
    dispatch_index = text.find("gh workflow run release-v25-cloud.yml", reserve_index)
    indexes = (loop_index, loop_end, conflict_index, reserve_index, dispatch_index)
    if min(indexes) < 0 or not (
        loop_index < loop_end < conflict_index < reserve_index < dispatch_index
    ):
        errors.append(
            "dispatcher must scan the complete reservation ledger, reject conflicts, then durably reserve before downstream dispatch"
        )

print("QS3D V25 preview reservation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: automatic V25 dispatch binds reservation identity to committed ProductVersion, "
    "reuses only an exact source reservation, rejects same-ordinal conflicts after complete ledger scan, "
    "records a new reservation before dispatch, and keeps main contents read-only."
)
