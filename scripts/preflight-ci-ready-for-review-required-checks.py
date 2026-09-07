#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def pull_request_types_block(text: str) -> str:
    marker = '  "pull_request":\n'
    start = text.find(marker)
    require(start >= 0, "Shared CI must declare a pull_request trigger.")
    types_start = text.find("    types:\n", start)
    branches_start = text.find("    branches:\n", types_start)
    require(types_start >= 0 and branches_start > types_start,
            "Shared CI pull_request trigger must have a bounded types block.")
    return text[types_start:branches_start]


def validate(text: str) -> None:
    types = pull_request_types_block(text)
    for action in ("opened", "synchronize", "reopened", "ready_for_review", "edited"):
        require(types.count(f"      - {action}\n") == 1,
                f"Shared CI pull_request types must contain exactly one {action} event.")

    # Both protected required jobs must continue admitting every pull_request action. This pins
    # ready_for_review to executable canonical validation rather than merely subscribing to an
    # event whose jobs can then be silently skipped by a narrower job-level condition.
    required_job_if = "    if: ${{ github.event_name == 'workflow_dispatch' || github.event_name == 'push' || github.event_name == 'pull_request' }}\n"
    require(text.count(required_job_if) == 2,
            "Shared CI preflight/core must each admit every pull_request action, including ready_for_review.")

    preflight_name = "    name: ${{ github.event_name == 'push' && 'branch-preflight' || github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata-preflight' || github.event_name == 'pull_request' && 'preflight' || 'dispatch-preflight' }}\n"
    core_name = "    name: ${{ github.event_name == 'push' && 'branch-core' || github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata-core' || github.event_name == 'pull_request' && 'core' || 'dispatch-core' }}\n"
    require(text.count(preflight_name) == 1,
            "Ready-for-review must retain the canonical required preflight job identity.")
    require(text.count(core_name) == 1,
            "Ready-for-review must retain the canonical required core job identity.")

    # `ready_for_review` changes merge admission state; it must stay in the ordinary PR-code
    # class so the protected required contexts are regenerated on the unchanged exact head.
    require("github.event.action == 'ready_for_review' && 'metadata'" not in text,
            "ready_for_review must not be routed into metadata-only concurrency.")
    require("github.event.action == 'ready_for_review' && 'metadata-preflight'" not in text,
            "ready_for_review must retain required preflight identity.")
    require("github.event.action == 'ready_for_review' && 'metadata-core'" not in text,
            "ready_for_review must retain required core identity.")

    # `edited` remains metadata-only and must not collide with required reviewable-state checks.
    require("github.event.action == 'edited' && 'metadata'" in text,
            "Edited PR events must retain their metadata concurrency class.")
    require("github.event.action == 'edited' && 'metadata-preflight'" in text,
            "Edited PR events must retain metadata-preflight identity.")
    require("github.event.action == 'edited' && 'metadata-core'" in text,
            "Edited PR events must retain metadata-core identity.")

    # Prior exact-head GREEN reuse is intentionally limited to metadata edits. Reviewability
    # transition must perform ordinary source/build classification and cannot reuse stale evidence.
    evidence_if = "github.event_name == 'pull_request' && github.event.action == 'edited'"
    require(text.count(evidence_if) >= 2,
            "Metadata exact-head reuse must remain explicitly scoped to edited PR events.")


workflow = WORKFLOW.read_text(encoding="utf-8")
validate(workflow)

mutated = workflow.replace("      - ready_for_review\n", "", 1)
require(mutated != workflow, "ready_for_review mutation probe could not modify the workflow fixture.")
try:
    validate(mutated)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed without ready_for_review validation.")

job_if = "    if: ${{ github.event_name == 'workflow_dispatch' || github.event_name == 'push' || github.event_name == 'pull_request' }}\n"
mutated_job = workflow.replace(job_if, "    if: ${{ github.event_name == 'workflow_dispatch' || github.event_name == 'push' }}\n", 1)
require(mutated_job != workflow, "required-job admission mutation probe could not modify the workflow fixture.")
try:
    validate(mutated_job)
except SystemExit:
    pass
else:
    raise SystemExit("Mutation probe unexpectedly passed with pull_request excluded from a required job.")

print("PASS CI ready-for-review required-check regeneration guard")
