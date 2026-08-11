#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

errors = []

agents = ROOT / "AGENTS.md"
remote = ROOT / "docs/REMOTE-AGENT-SCOPE.md"
inbox = ROOT / "docs/LOCAL-AGENT-INBOX.md"

for path in (agents, remote, inbox):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    agents_text = agents.read_text(encoding="utf-8")
    remote_text = remote.read_text(encoding="utf-8")
    inbox_text = inbox.read_text(encoding="utf-8")

    agents_required = (
        "## Mandatory unavailable-work handoff",
        "must not leave that work only in chat",
        "docs/LOCAL-AGENT-INBOX.md",
        "subsequent remote/non-local agents must **read and skip that execution gate",
    )
    for token in agents_required:
        if token not in agents_text:
            errors.append("AGENTS.md missing handoff token: " + token)

    remote_required = (
        "mandatory Markdown handoff condition",
        "before ending the batch",
        "must not leave the blocker only in chat",
        "later remote/non-local agents must read and skip that execution gate",
    )
    for token in remote_required:
        if token not in remote_text:
            errors.append("REMOTE-AGENT-SCOPE.md missing handoff token: " + token)

    inbox_required = (
        "single live queue for LOCAL_ONLY work",
        "before ending the batch",
        "Update an existing matching item instead of creating a duplicate",
        "Required format for new or materially changed handoffs",
        "Source-side status: COMPLETE | PARTIAL | NOT_STARTED",
        "Evidence required: objective pass/fail evidence and exit criteria",
    )
    for token in inbox_required:
        if token not in inbox_text:
            errors.append("LOCAL-AGENT-INBOX.md missing contract token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: unavailable non-local work must be handed off once, durably and without duplicate remote retries.")
