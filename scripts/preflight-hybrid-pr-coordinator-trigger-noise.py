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
on_index = next((i for i, line in enumerate(lines) if re.fullmatch(r'"?on"?:\s*(?:#.*)?', line)), None)
if on_index is None:
    errors.append("hybrid coordinator must declare top-level on block")
    trigger_lines = []
else:
    trigger_lines = []
    for line in lines[on_index + 1:]:
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        trigger_lines.append(line)

trigger_text = "\n".join(trigger_lines)
if re.search(r'(?m)^\s{2}["\']?workflow_run["\']?\s*:', trigger_text):
    errors.append("hybrid coordinator must not subscribe to workflow_run; GitHub creates unavoidable empty/noise runs before job-level if evaluation")

for required in ("pull_request", "push"):
    if not re.search(rf'(?m)^\s{{2}}["\']?{required}["\']?\s*:', trigger_text):
        errors.append(f"hybrid coordinator must retain {required} trigger")

for forbidden in (
    "promote-green-draft:",
    "github.event.workflow_run",
    "github.event_name == 'workflow_run'",
):
    if forbidden in text:
        errors.append(f"hybrid coordinator retains workflow_run-dependent noise path: {forbidden}")

for required in (
    "arm-native-automerge:",
    "refresh-branches:",
    "github.event_name == 'pull_request'",
    "github.event_name == 'push'",
    "enablePullRequestAutoMerge",
    "disablePullRequestAutoMerge",
    "/update-branch",
    "expected_head_sha",
):
    if required not in text:
        errors.append(f"hybrid coordinator missing preserved behavior token: {required}")

print("QS3D Hybrid PR Coordinator trigger-noise preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Hybrid PR Coordinator no longer creates workflow_run noise while PR auto-merge reconciliation and main branch refresh remain intact.")
