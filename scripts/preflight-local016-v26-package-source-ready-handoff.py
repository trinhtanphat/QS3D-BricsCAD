#!/usr/bin/env python3
"""Fail closed if LOCAL-016 loses the accepted V26 package lifecycle bounded PASS."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
RUNBOOK_PATH = ROOT / "docs" / "LOCAL-V26-PACKAGE-INSTALL-LIFECYCLE.md"
RUNNER = "scripts/test-v26-package-install-lifecycle.ps1"
RUNBOOK = "docs/LOCAL-V26-PACKAGE-INSTALL-LIFECYCLE.md"
SOURCE_READY_SHA = "5da966686826a350d8babc8f22a390ab29ec824b"
TESTED_SHA = "e90c6aba7ef7bf903042d42dd991f9e7112fe659"
EVIDENCE_PR = "#3916"
EVIDENCE_MERGE = "d67119defb31d8649ea7099e2c45bc53996331b5"
PACKAGE_SHA256 = "60F5239611B13F424BAE49922E5D34ADF3FC12C3064BF7506FE06CD27B8B3F7C"


def fail(message: str) -> None:
    print(f"ERROR: LOCAL-016 V26 package bounded-PASS handoff failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path, label in ((INBOX, "canonical local-agent inbox"), (RUNBOOK_PATH, "V26 package lifecycle runbook")):
    if not path.is_file():
        fail(f"{label} is missing")

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
    EVIDENCE_PR,
    "Package lifecycle evidence: `LOCAL_PASS / BOUNDED`",
    SOURCE_READY_SHA,
    TESTED_SHA,
    EVIDENCE_MERGE,
    RUNNER,
    RUNBOOK,
    "broader V26",
)
for token in required:
    if token not in block:
        fail(f"LOCAL-016 package handoff is missing: {token}")

runbook = RUNBOOK_PATH.read_text(encoding="utf-8")
for token in (
    "Status: `LOCAL_PASS / BOUNDED`",
    EVIDENCE_PR,
    TESTED_SHA,
    "BricsCAD V26.2.07",
    "0.1.0-preview.10081",
    PACKAGE_SHA256,
    RUNNER,
    "#1462",
):
    if token not in runbook:
        fail(f"V26 package lifecycle runbook is missing accepted evidence: {token}")

for stale in (
    "SOURCE_FIX_READY / PENDING_LICENSED_V26",
    "Package lifecycle evidence: `PENDING_LICENSED_V26`",
    "no post-#3878 licensed rerun is recorded here yet",
    "6808636f4f6809e44d6c6fcd1f0c73121e1b5dd3",
    "runtimeOptions.framework.name",
    "SOURCE_FIX_REQUIRED",
):
    if stale in block or stale in runbook:
        fail(f"LOCAL-016 package handoff reintroduced stale state: {stale}")

print("PASS LOCAL-016 preserves the exact #3916 V26 package install/uninstall bounded PASS while overall LOCAL-016 remains IN_PROGRESS")
