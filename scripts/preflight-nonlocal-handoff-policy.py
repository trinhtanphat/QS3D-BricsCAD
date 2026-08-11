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

    for token in ("docs/REMOTE-AGENT-SCOPE.md", "docs/LOCAL-AGENT-INBOX.md"):
        if token not in agents_text:
            errors.append("AGENTS.md must route agents through " + token)

    remote_required = (
        "Mandatory durable handoff for anything a non-local agent cannot finish",
        "before ending that work batch",
        "chat-only note",
        "update that item instead of creating a duplicate",
        "Source-side status: COMPLETE",
        "Handoff only the irreducible local residue",
    )
    for token in remote_required:
        if token not in remote_text:
            errors.append("REMOTE-AGENT-SCOPE.md missing contract token: " + token)

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

print("PASS: non-local blockers require a durable deduplicated LOCAL-AGENT-INBOX handoff before the remote batch ends.")
