#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AGENTS = ROOT / "AGENTS.md"
REMOTE_SCOPE = ROOT / "docs/REMOTE-AGENT-SCOPE.md"
LOCAL_INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing remote/local handoff policy file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


agents = read(AGENTS)
remote = read(REMOTE_SCOPE)
inbox = read(LOCAL_INBOX)

for token in (
    "## Mandatory unavailable-work handoff",
    "must not leave that work only in chat",
    "docs/LOCAL-AGENT-INBOX.md",
    "subsequent remote/non-local agents must **read and skip that execution gate",
):
    if token not in agents:
        errors.append("AGENTS.md missing mandatory unavailable-work handoff token: " + token)

for token in (
    "mandatory Markdown handoff condition",
    "must not leave the blocker only in chat",
    "later remote/non-local agents must read and skip that execution gate",
    "Local-capability gaps belong to compatible local agents",
):
    if token not in remote:
        errors.append("REMOTE-AGENT-SCOPE.md missing remote/local handoff token: " + token)

for token in (
    "## Mandatory handoff contract",
    "must add or update the matching item in this file **in the same source/docs batch",
    "LOCAL_PASS requires real evidence tied to the exact tested SHA",
    "Never commit proprietary BricsCAD DLLs",
):
    if token not in inbox:
        errors.append("LOCAL-AGENT-INBOX.md missing mandatory handoff token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: unavailable remote work must be handed to the canonical local inbox and equivalent remote agents must skip repeated local-only attempts.")
