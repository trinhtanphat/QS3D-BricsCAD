#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"

EXPECTED_GROUP = "  group: qs3d-shared-ci-${{ github.workflow }}-${{ github.event.pull_request.head.repo.full_name || github.repository }}-${{ github.event.pull_request.head.ref || github.ref_name }}-${{ github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata' || github.event_name == 'pull_request' && 'pull_request' || github.event_name == 'push' && 'push' || 'dispatch' }}\n"
EXPECTED_CANCEL = "  cancel-in-progress: ${{ github.run_attempt == 1 }}\n"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require(text.count(EXPECTED_GROUP) == 1,
            "Shared CI must preserve repository/branch/event-class concurrency isolation.")
    require(text.count(EXPECTED_CANCEL) == 1,
            "Shared CI cancellation must be first-attempt-only so stale manual reruns cannot preempt current-head validation.")
    require("  cancel-in-progress: true\n" not in text,
            "Shared CI must not allow every historical rerun attempt to cancel current work.")

    require("github.event.action == 'edited' && 'metadata'" in text,
            "Edited PR events must retain their metadata concurrency class.")
    require("github.event_name == 'pull_request' && 'pull_request'" in text,
            "PR code validation must retain its dedicated concurrency class.")
    require("github.event_name == 'push' && 'push'" in text,
            "Push validation must retain its dedicated concurrency class.")


workflow = WORKFLOW.read_text(encoding="utf-8")
validate(workflow)

mutated = workflow.replace(EXPECTED_CANCEL, "  cancel-in-progress: true\n", 1)
require(mutated != workflow,
        "Stale-rerun cancellation mutation probe could not modify the workflow fixture.")
try:
    validate(mutated)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed with unconditional cancel-in-progress.")

print("PASS CI stale-rerun cancellation guard")
