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

    required = {
        "AGENTS.md": (
            (agents_text, "## Mandatory unavailable-work handoff"),
            (agents_text, "must not leave that work only in chat"),
            (agents_text, "subsequent remote/non-local agents must **read and skip that execution gate"),
        ),
        "docs/REMOTE-AGENT-SCOPE.md": (
            (remote_text, "mandatory Markdown handoff condition"),
            (remote_text, "must not leave the blocker only in chat"),
            (remote_text, "later remote/non-local agents must read and skip that execution gate"),
        ),
        "docs/LOCAL-AGENT-INBOX.md": (
            (inbox_text, "single live queue for LOCAL_ONLY work"),
            (inbox_text, "DO_NOT_RETRY_REMOTE"),
            (inbox_text, "update the existing matching item instead of duplicating the same unavailable work"),
        ),
    }

    for label, checks in required.items():
        for text, token in checks:
            if token not in text:
                errors.append(label + " missing non-local handoff token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: unavailable non-local work is handed off to the canonical local inbox and is not retried by equivalent remote agents.")
