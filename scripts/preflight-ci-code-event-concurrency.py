#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"

EXPECTED_GROUP = (
    "group: qs3d-shared-ci-${{ github.workflow }}-"
    "${{ github.event.pull_request.head.repo.full_name || github.repository }}-"
    "${{ github.event.pull_request.head.ref || github.ref_name }}-"
    "${{ github.event_name == 'pull_request' && 'pull_request' || "
    "github.event_name == 'push' && 'push' || 'dispatch' }}"
)
REQUIRED = (
    "github.workflow",
    "github.event.pull_request.head.repo.full_name",
    "github.repository",
    "github.event.pull_request.head.ref",
    "github.ref_name",
    "github.event_name",
    "'pull_request'",
    "'push'",
    "'dispatch'",
    "cancel-in-progress: true",
)
FORBIDDEN = (
    "github.event.pull_request.number",
    "github.event.action == 'edited'",
    "'metadata'",
    "github.run_attempt",
    "github.run_id",
)

def concurrency_block(workflow: str) -> str:
    marker = "concurrency:\n"
    start = workflow.find(marker)
    if start < 0:
        raise ValueError("Shared CI workflow has no concurrency policy")
    end = workflow.find("\njobs:\n", start)
    if end < 0:
        raise ValueError("Shared CI concurrency block is not bounded before jobs")
    return workflow[start:end]

def validate(block: str) -> list[str]:
    errors: list[str] = []
    if block.count(EXPECTED_GROUP) != 1:
        errors.append("missing exact unified PR/head concurrency group")
    for token in REQUIRED:
        if token not in block:
            errors.append(f"missing required token: {token}")
    for token in FORBIDDEN:
        if token in block:
            errors.append(f"uses forbidden split/stale concurrency token: {token}")
    return errors

try:
    block = concurrency_block(WORKFLOW.read_text(encoding="utf-8"))
except (OSError, UnicodeError, ValueError) as exc:
    raise SystemExit(str(exc)) from exc

errors = validate(block)
if errors:
    raise SystemExit("Shared CI code-event concurrency failed closed: " + "; ".join(errors))

for token in REQUIRED:
    mutated = block.replace(token, "")
    if mutated == block or not validate(mutated):
        raise SystemExit(f"Concurrency mutation unexpectedly passed after removing: {token}")

for token in FORBIDDEN:
    mutated = block + "\n# mutation probe " + token
    if not validate(mutated):
        raise SystemExit(f"Concurrency mutation unexpectedly passed after adding forbidden token: {token}")

print("PASS Shared CI unifies all PR actions per repository/head while isolating push/dispatch and rejecting stale rerun domains")
