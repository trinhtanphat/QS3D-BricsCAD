#!/usr/bin/env python3
"""Fail closed if reruns can escape the live exact-head cancellation domain."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
EXPECTED_GROUP = (
    "  group: qs3d-shared-ci-${{ github.workflow }}-"
    "${{ github.event.pull_request.head.repo.full_name || github.repository }}-"
    "${{ github.event.pull_request.head.ref || github.ref_name }}-"
    "${{ github.event_name == 'pull_request' && 'pull_request' || "
    "github.event_name == 'push' && 'push' || 'dispatch' }}\n"
)
EXPECTED_CANCEL = "  cancel-in-progress: true\n"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require(text.count(EXPECTED_GROUP) == 1,
            "Shared CI must use one exact repository/head/event-class cancellation group.")
    require(text.count(EXPECTED_CANCEL) == 1,
            "Superseded exact-head validation must remain cancellable.")
    for forbidden in (
        "github.run_attempt",
        "github.run_id",
        "github.event.action == 'edited' && 'metadata'",
        "cancel-in-progress: ${{ github.run_attempt == 1 }}",
    ):
        require(forbidden not in text,
                f"Rerun/metadata isolation can preserve stale required-context authority: {forbidden}")


workflow = WORKFLOW.read_text(encoding="utf-8")
validate(workflow)

for old, new, label in (
    (EXPECTED_GROUP, EXPECTED_GROUP.rstrip("\n") + "-${{ github.run_id }}\n", "run-id isolation"),
    (EXPECTED_CANCEL, "  cancel-in-progress: false\n", "disabled cancellation"),
    ("'pull_request' || github.event_name == 'push'", "github.event.action == 'edited' && 'metadata' || github.event_name == 'push'", "metadata split"),
):
    mutated = workflow.replace(old, new, 1)
    require(mutated != workflow, f"{label} mutation probe could not modify workflow fixture.")
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after {label}.")

print("PASS CI reruns remain in the live exact-head cancellation domain with no stale-context escape")
