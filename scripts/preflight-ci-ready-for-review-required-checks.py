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

print("PASS CI ready-for-review required-check regeneration guard")
