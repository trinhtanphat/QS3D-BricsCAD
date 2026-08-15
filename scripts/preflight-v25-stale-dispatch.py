#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
errors = []

if not WORKFLOW.is_file():
    errors.append("missing V25 post-main dispatcher workflow")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    stale_guard = 'if [[ "${current_main}" != "${source_sha}" ]]; then'
    stale_exit = "exit 0"
    reservation = 'reservation="${reservation_prefix} ordinal=${preview} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"'
    dispatch = "gh workflow run release-v25-cloud.yml"

    guard_index = text.find(stale_guard)
    exit_index = text.find(stale_exit, guard_index)
    reservation_index = text.find(reservation)
    dispatch_index = text.find(dispatch)

    if guard_index < 0:
        errors.append("dispatcher does not compare current main to triggering source_sha")
    if exit_index < 0:
        errors.append("stale dispatcher does not exit successfully")
    if reservation_index < 0:
        errors.append("preview reservation contract is missing")
    if dispatch_index < 0:
        errors.append("downstream release dispatch is missing")
    if min(guard_index, exit_index, reservation_index, dispatch_index) >= 0:
        if not (guard_index < exit_index < reservation_index < dispatch_index):
            errors.append("stale-main exit must occur before preview reservation and downstream dispatch")

    stale_block_end = text.find("\n          fi", guard_index)
    if guard_index >= 0 and stale_block_end >= 0:
        stale_block = text[guard_index:stale_block_end]
        if "without reserving or dispatching" not in stale_block:
            errors.append("stale dispatcher diagnostic must explain that it skips reservation and dispatch")

print("QS3D V25 stale-dispatch preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: a dispatcher superseded by a newer main SHA exits successfully before reserving a preview ordinal or dispatching the release workflow.")
