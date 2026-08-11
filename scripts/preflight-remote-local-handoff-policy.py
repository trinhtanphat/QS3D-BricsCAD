#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

required = {
    "AGENTS.md": [
        "docs/REMOTE-AGENT-SCOPE.md",
        "docs/LOCAL-AGENT-INBOX.md",
        "Do not repeatedly re-audit an already parked LOCAL_ONLY gate",
    ],
    "docs/REMOTE-AGENT-SCOPE.md": [
        "## Mandatory remote inability handoff",
        "docs/LOCAL-AGENT-INBOX.md",
        "update that item instead of creating a duplicate",
        "exact source SHA",
        "do-not-repeat remote backlog",
        "never mark `PASS` from remote evidence",
        "A remote agent must not finish with only a chat note",
    ],
    "docs/LOCAL-AGENT-INBOX.md": [
        "## Mandatory handoff contract",
        "same source/docs batch",
        "DO_NOT_RETRY_REMOTE",
        "Evidence required:",
        "Evidence: PENDING_LOCAL",
        "Valid statuses: `OPEN`, `IN_PROGRESS`, `PASS`, `BLOCKED`",
    ],
}

errors = []
for relative, tokens in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append(relative + " is missing")
        continue
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(relative + " missing remote/local handoff guard: " + token)

if errors:
    print("QS3D remote/local handoff policy preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: remote agents must park machine-only blockers in the canonical local inbox with exact scenario/evidence, reuse existing LOCAL items instead of duplicating them, and honor DO_NOT_RETRY_REMOTE instead of repeating or marking local qualification PASS remotely.")
