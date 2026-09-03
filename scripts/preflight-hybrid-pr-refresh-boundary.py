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
update_pos = block.find("update-branch")
graphql_pos = block.find("gh api graphql")
if update_pos < 0 or "expected_head_sha" not in block:
    errors.append("refresh-branches lost exact-head update-branch semantics")
if graphql_pos < 0:
    errors.append("refresh-branches lost native auto-merge reconciliation")
elif update_pos < 0 or graphql_pos < update_pos:
    errors.append("GraphQL auto-merge runs before branch refresh")
if "automerge_failures" not in block:
    errors.append("refresh-branches must aggregate auto-merge failures instead of aborting the PR loop")
if "HTTP (409|422)" not in block:
    errors.append("refresh-branches lost safe stale/conflict handling")
if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: branch refresh precedes auto-merge reconciliation and auto-merge failures are aggregated")
