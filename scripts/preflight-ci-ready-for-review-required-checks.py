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

    preflight_if = "    if: ${{ github.event_name == 'workflow_dispatch' || github.event_name == 'push' || github.event_name == 'pull_request' }}\n"
    core_if = "    if: ${{ !cancelled() && (github.event_name == 'workflow_dispatch' || github.event_name == 'push' || github.event_name == 'pull_request') }}\n"
    require(text.count(preflight_if) == 1, "Preflight must admit every pull_request action.")
    require(text.count(core_if) == 1,
            "Core must admit every pull_request action while remaining cancellation-aware.")

    preflight_name = "    name: ${{ github.event_name == 'push' && 'branch-preflight' || github.event_name == 'pull_request' && 'preflight' || 'dispatch-preflight' }}\n"
    core_name = "    name: ${{ github.event_name == 'push' && 'branch-core' || github.event_name == 'pull_request' && 'core' || 'dispatch-core' }}\n"
    require(text.count(preflight_name) == 1, "Every PR action must publish stable preflight.")
    require(text.count(core_name) == 1, "Every PR action must publish stable core.")

    for forbidden in (
        "github.event.action == 'edited' && 'metadata'",
        "metadata-preflight",
        "metadata-core",
        "reuse_exact_head_green",
        "QS3D_REUSE_EXACT_HEAD_GREEN",
    ):
        require(forbidden not in text, f"Superseded metadata/historical-reuse contract remains: {forbidden}")

    evidence_if = "github.event_name == 'pull_request' && github.event.action == 'edited'"
    require(text.count(evidence_if) == 1,
            "Edited events must retain exactly one identity-validation gate without a validation bypass.")

workflow = WORKFLOW.read_text(encoding="utf-8")
validate(workflow)

for old, new, label in (
    ("      - ready_for_review\n", "", "ready_for_review"),
    (" && (github.event_name == 'workflow_dispatch'", " && always() && (github.event_name == 'workflow_dispatch'", "core cancellation"),
    ("github.event_name == 'pull_request' && 'preflight'", "github.event_name == 'pull_request' && 'metadata-preflight'", "stable preflight"),
):
    mutated = workflow.replace(old, new, 1)
    require(mutated != workflow, f"{label} mutation probe could not modify fixture.")
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after breaking {label}.")

print("PASS CI ready-for-review and edited events regenerate stable cancellation-safe required checks")
