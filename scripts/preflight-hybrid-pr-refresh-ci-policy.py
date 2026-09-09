#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"

try:
    text = WORKFLOW.read_text(encoding="utf-8", errors="strict")
except (OSError, UnicodeError) as exc:
    print(f"ERROR: cannot read hybrid PR coordinator safely: {exc}")
    sys.exit(1)

refresh_marker = "  refresh-branches:"
if refresh_marker not in text:
    print("ERROR: hybrid PR coordinator is missing refresh-branches job")
    sys.exit(1)

refresh = text.split(refresh_marker, 1)[1]
errors = []

required_tokens = (
    'actions/workflows/ci.yml',
    'shared_ci_workflow_id',
    'event=pull_request&head_sha=${current_head_sha}&per_page=100',
    'active_run_ids',
    '.status == "queued"',
    '.status == "in_progress"',
    '.status == "waiting"',
    '.status == "requested"',
    '.status == "pending"',
    '/actions/runs/${run_id}/cancel',
    'latest_completed_conclusion',
    'failure|timed_out|action_required|startup_failure',
    'exact-head Shared CI completed RED',
)
for token in required_tokens:
    if token not in refresh:
        errors.append(f"refresh-branches missing required CI refresh-policy token: {token}")

for forbidden in (
    'gh workflow run ci.yml',
    'repos/${GITHUB_REPOSITORY}/pulls/${number}/merge',
):
    if forbidden in refresh:
        errors.append(f"refresh-branches contains forbidden CI refresh-policy token: {forbidden}")

active_index = refresh.find("active_run_ids")
cancel_index = refresh.find('/actions/runs/${run_id}/cancel')
red_index = refresh.find("latest_completed_conclusion")
hold_index = refresh.find("exact-head Shared CI completed RED")
rebase_index = refresh.find('rebase_output=""')
if min(active_index, cancel_index, red_index, hold_index, rebase_index) >= 0:
    if not (active_index < cancel_index < red_index < hold_index < rebase_index):
        errors.append(
            "refresh-branches must snapshot/cancel active exact-head CI, then hold completed RED, before update-branch"
        )

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} hybrid PR refresh-policy error(s).")
    sys.exit(1)

print(
    "PASS: stale PR refresh is CI-state-aware: active exact-head Shared CI is cancelled before refresh, "
    "completed RED is held, and only the resulting synchronize event starts fresh CI."
)
