#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
workflow = root / ".github" / "workflows" / "hybrid-pr-coordinator.yml"
text = workflow.read_text(encoding="utf-8")
marker = "  refresh-branches:\n"
start = text.find(marker)
if start < 0:
    print("ERROR: refresh-branches job is missing")
    sys.exit(1)
tail = text[start + len(marker):]
match = re.search(r"^  [A-Za-z0-9_-]+:\s*$", tail, re.MULTILINE)
block = tail if match is None else tail[:match.start()]
errors = []
if "update-branch" not in block or "expected_head_sha" not in block:
    errors.append("refresh-branches lost exact-head update-branch semantics")
if "gh api graphql" in block:
    errors.append("refresh-branches is coupled to GraphQL auto-merge operations")
if "enablePullRequestAutoMerge" in block or "autoMergeRequest" in block:
    errors.append("refresh-branches contains native auto-merge state handling")
if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: branch refresh is independent from native auto-merge permission handling")
