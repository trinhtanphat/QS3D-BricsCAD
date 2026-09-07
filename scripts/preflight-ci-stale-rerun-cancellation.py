#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"

LIVE_GROUP = "qs3d-shared-ci-${{ github.workflow }}-${{ github.event.pull_request.head.repo.full_name || github.repository }}-${{ github.event.pull_request.head.ref || github.ref_name }}-${{ github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata' || github.event_name == 'pull_request' && 'pull_request' || github.event_name == 'push' && 'push' || 'dispatch' }}"
EXPECTED_GROUP = f"  group: {LIVE_GROUP}-${{{{ github.run_attempt > 1 && github.run_id || 0 }}}}\n"
EXPECTED_CANCEL = "  cancel-in-progress: true\n"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require(text.count(EXPECTED_GROUP) == 1,
            "Shared CI must isolate historical reruns from the live repository/branch/event-class concurrency group.")
    require(text.count(EXPECTED_CANCEL) == 1,
            "Live first-attempt events must retain superseded-work cancellation semantics.")
    require("cancel-in-progress: ${{ github.run_attempt == 1 }}" not in text,
            "Conditional cancellation alone is insufficient because a new pending run replaces an older pending run in the same group.")

    require("github.event.action == 'edited' && 'metadata'" in text,
            "Edited PR events must retain their metadata concurrency class.")
    require("github.event_name == 'pull_request' && 'pull_request'" in text,
            "PR code validation must retain its dedicated concurrency class.")
    require("github.event_name == 'push' && 'push'" in text,
            "Push validation must retain its dedicated concurrency class.")
    require("github.run_attempt > 1 && github.run_id || 0" in text,
            "Historical reruns must receive a run-id-specific concurrency suffix while first attempts share the live suffix.")


workflow = WORKFLOW.read_text(encoding="utf-8")
validate(workflow)

mutated = workflow.replace(
    "-${{ github.run_attempt > 1 && github.run_id || 0 }}\n  cancel-in-progress: true\n",
    "\n  cancel-in-progress: true\n",
    1,
)
require(mutated != workflow,
        "Stale-rerun group-isolation mutation probe could not modify the workflow fixture.")
try:
    validate(mutated)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed without historical-rerun group isolation.")

print("PASS CI stale-rerun cancellation guard")
