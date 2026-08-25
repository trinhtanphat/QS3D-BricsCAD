#!/usr/bin/env python3
"""Fail closed if LOCAL-016 loses the post-#3878 V26 package lifecycle handoff."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
RUNNER = "scripts/test-v26-package-install-lifecycle.ps1"
RUNBOOK = "docs/LOCAL-V26-PACKAGE-INSTALL-LIFECYCLE.md"
SOURCE_READY_SHA = "5da966686826a350d8babc8f22a390ab29ec824b"


def fail(message: str) -> None:
    print(f"ERROR: LOCAL-016 V26 package source-ready handoff failed: {message}", file=sys.stderr)
    raise SystemExit(1)


if not INBOX.is_file():
    fail("canonical local-agent inbox is missing")

text = INBOX.read_text(encoding="utf-8")
start = text.find("## LOCAL-016 — BricsCAD V26 native authoring and dependent-output qualification")
end = text.find("## LOCAL-017 —", start)
if start < 0 or end < 0:
    fail("LOCAL-016 section boundary is missing")
block = text[start:end]

required = (
    "- Status: IN_PROGRESS",
    "#3878",
    "#3879",
    "SOURCE_FIX_READY / PENDING_LICENSED_V26",
    SOURCE_READY_SHA,
    RUNNER,
    RUNBOOK,
    "clean exact checkout",
    "no local source patch",
    "PASS/FAIL/NO_RESULT",
)
for token in required:
    if token not in block:
        fail(f"LOCAL-016 package handoff is missing: {token}")

for stale in (
    "6808636f4f6809e44d6c6fcd1f0c73121e1b5dd3",
    "runtimeOptions.framework.name",
    "SOURCE_FIX_REQUIRED",
):
    if stale in block:
        fail(f"LOCAL-016 package handoff reintroduced stale source state: {stale}")

print("PASS LOCAL-016 publishes the exact post-#3878 V26 package lifecycle source-ready handoff")
