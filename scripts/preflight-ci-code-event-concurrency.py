#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
text = WORKFLOW.read_text(encoding="utf-8")

marker = "concurrency:\n"
start = text.find(marker)
if start < 0:
    raise SystemExit("Shared CI workflow has no concurrency policy")
end = text.find("\njobs:\n", start)
if end < 0:
    raise SystemExit("Shared CI concurrency block is not bounded before jobs")
block = text[start:end]

required = (
    "github.event.pull_request.head.repo.full_name",
    "github.repository",
    "github.event.pull_request.head.ref",
    "github.ref_name",
    "github.event.action == 'edited'",
    "cancel-in-progress: true",
)
for token in required:
    if token not in block:
        raise SystemExit(f"Shared CI concurrency policy is missing required token: {token}")

for forbidden in (
    "github.event.pull_request.number || github.ref",
    "github.event.pull_request.number",
):
    if forbidden in block:
        raise SystemExit(
            "Shared CI code-event concurrency diverges push and pull_request runs for the same branch: "
            + forbidden
        )

print("PASS Shared CI coalesces same-branch push/PR code runs while preserving fork and metadata-edit isolation")
