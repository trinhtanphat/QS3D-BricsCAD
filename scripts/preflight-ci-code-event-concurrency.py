#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
text = WORKFLOW.read_text(encoding="utf-8")

REQUIRED = (
    "github.workflow",
    "github.event.pull_request.head.repo.full_name",
    "github.repository",
    "github.event.pull_request.head.ref",
    "github.ref_name",
    "github.event_name",
    "github.event.action == 'edited'",
    "'metadata'",
    "'pull_request'",
    "'push'",
    "'dispatch'",
    "cancel-in-progress: true",
)
FORBIDDEN = (
    "github.event.pull_request.number || github.ref",
    "github.event.pull_request.number",
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
    for token in REQUIRED:
        if token not in block:
            errors.append(f"missing required token: {token}")
    for token in FORBIDDEN:
        if token in block:
            errors.append(f"uses forbidden PR-number concurrency identity: {token}")
    return errors


try:
    block = concurrency_block(text)
except ValueError as exc:
    raise SystemExit(str(exc)) from exc

errors = validate(block)
if errors:
    raise SystemExit("Shared CI code-event concurrency failed closed: " + "; ".join(errors))

# Every safety-bearing identity discriminator is mutation-tested. Removing any one must make the
# validator reject the workflow; this prevents a future refactor from silently collapsing event
# families back into a cross-event cancellation domain or dropping fork/branch isolation.
for token in REQUIRED:
    mutated = block.replace(token, "", 1)
    if mutated == block:
        raise SystemExit(f"Concurrency mutation fixture could not remove required token: {token}")
    if not validate(mutated):
        raise SystemExit(f"Concurrency mutation unexpectedly passed after removing: {token}")

for token in FORBIDDEN:
    mutated = block + "\n# mutation probe " + token
    if not validate(mutated):
        raise SystemExit(f"Concurrency mutation unexpectedly passed after adding forbidden identity: {token}")

print("PASS Shared CI isolates push/PR-code/PR-metadata/dispatch cancellation while preserving branch and fork identity")
