#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

required = {
    "AGENTS.md": [
        "docs/REMOTE-AGENT-SCOPE.md",
        "docs/LOCAL-AGENT-INBOX.md",
        "Remote agents must skip execution gates already classified `LOCAL_ONLY` rather than repeatedly rechecking them.",
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
    "docs/LOCAL-AGENT-HANDOFF-SCHEMA.md": [
        "schema/contract only",
        "docs/LOCAL-AGENT-INBOX.md` remains the single live queue",
        "Required format for new or materially changed handoffs",
        "Source-side status: REMOTE_DONE | REMOTE_PARTIAL | NOT_STARTED",
        "Remote disposition: DO_NOT_RETRY_REMOTE",
        "Blocker: exact reason a non-local agent cannot execute/prove the remaining work",
        "Source SHA: exact source/main SHA whose behavior must be qualified",
        "Expected result: objective pass condition",
        "Evidence required: exact artifacts/measurements/logs/state needed to prove PASS",
        "A chat-only note",
        "Do not use it to avoid repository work that can be implemented or statically validated remotely",
        "cannot manufacture `LOCAL_PASS`",
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

print("PASS: remote agents must complete source-safe work, park only irreducible machine-only residue in the canonical local inbox using the required exact-SHA handoff schema, reuse existing LOCAL items, and honor DO_NOT_RETRY_REMOTE without manufacturing LOCAL_PASS.")
