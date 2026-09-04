#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"

errors = []

try:
    text = WORKFLOW.read_text(encoding="utf-8")
except (OSError, UnicodeError) as exc:
    print(f"ERROR: cannot read {WORKFLOW.relative_to(ROOT)}: {exc}")
    sys.exit(1)

lines = text.splitlines()
concurrency_index = next((i for i, line in enumerate(lines) if re.fullmatch(r"concurrency:\s*(?:#.*)?", line)), None)
if concurrency_index is None:
    errors.append("hybrid coordinator must declare top-level concurrency")
    concurrency_lines = []
else:
    concurrency_lines = []
    for line in lines[concurrency_index + 1:]:
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        concurrency_lines.append(line)

concurrency = "\n".join(concurrency_lines)
group_match = re.search(r"(?m)^\s{2}group:\s*(.+?)\s*$", concurrency)
if group_match is None:
    errors.append("hybrid coordinator concurrency must declare a group")
    group = ""
else:
    group = group_match.group(1)

if group == "qs3d-hybrid-pr-coordinator":
    errors.append("hybrid coordinator concurrency group must not be repository-global")

required_group_tokens = (
    "qs3d-hybrid-pr-coordinator-${{",
    "github.event_name == 'pull_request'",
    "github.event.pull_request.number",
    "github.event_name == 'push'",
    "'main-refresh'",
    "github.run_id",
)
for token in required_group_tokens:
    if token not in group:
        errors.append(f"hybrid coordinator concurrency group missing token: {token}")

for forbidden in (
    "github.event_name == 'workflow_run'",
    "github.event.workflow_run",
):
    if forbidden in group:
        errors.append(f"hybrid coordinator concurrency retains removed workflow_run path: {forbidden}")

if not re.search(r"(?m)^\s{2}cancel-in-progress:\s*false\s*(?:#.*)?$", concurrency):
    errors.append("hybrid coordinator concurrency must keep cancel-in-progress: false")

print("QS3D Hybrid PR Coordinator concurrency preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: coordinator concurrency is scoped per PR, main refresh events share only the main-refresh key, fallback events remain isolated, and no removed workflow_run path remains.")
