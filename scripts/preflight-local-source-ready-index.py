#!/usr/bin/env python3
"""Fail-closed guard for the consolidated LOCAL_ONLY source-ready handoff."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"


def fail(message: str) -> None:
    print(f"ERROR: LOCAL_ONLY source-ready index guard failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require_file(relative: str) -> None:
    path = ROOT / relative
    if not path.is_file():
        fail(f"required committed runner/runbook is missing: {relative}")


if not INDEX.is_file():
    fail("docs/LOCAL-SOURCE-READY-INDEX-2026-08-24.md is missing")

text = INDEX.read_text(encoding="utf-8")

required_literals = (
    "Status: `SOURCE_READY / LOCAL_RUN_ONLY`",
    "Lane-Key: `issue-3680`",
    "agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh",
    "0062e0cd73a570a7ca774dfa8b3ff91e8df20f31",
    "Do not rerun the obsolete #3593 P06 binary.",
    "PR #3616 is merged (`12b5f0d7d8549d8b107a1b921d2bb431f809bf69`)",
    "A local result is one of:",
    "`PASS`",
    "`FAIL`",
    "`NO_RESULT`",
    "Never commit BricsCAD proprietary DLLs, license files, activation material, signing keys",
)
for literal in required_literals:
    if literal not in text:
        fail(f"required handoff contract text is missing: {literal}")

rows = re.findall(r"^\| (LOCAL-\d{3}) \|", text, flags=re.MULTILINE)
expected = [f"LOCAL-{number:03d}" for number in range(1, 19)]
if rows != expected:
    fail(f"LOCAL row order/cardinality drifted: expected {expected}, got {rows}")

for local_id in expected:
    count = rows.count(local_id)
    if count != 1:
        fail(f"{local_id} must appear exactly once in the canonical matrix, got {count}")

# Local workers must receive runnable repository surfaces rather than a prose-only
# promise that remote/source preparation exists.
for relative in (
    "docs/LOCAL-DISPATCH-READY-2026-08-24.md",
    "docs/LOCAL-AGENT-INBOX.md",
    "docs/LOCAL-V25-QUALIFICATION.md",
    "docs/LOCAL-V26-QUALIFICATION.md",
    "docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md",
    "scripts/run-local-v25-qualification.ps1",
):
    require_file(relative)

# The consolidated handoff must not regress to the stale scheduling statements
# that the current #3680 dispatch explicitly supersedes.
for forbidden in (
    "P03 is separately qualified on PR #3616 but not yet integrated",
    "#3621 remains SOURCE_FIX_REQUIRED; do not rerun the unchanged P06 binary",
):
    if forbidden in text:
        fail(f"stale local scheduling text reintroduced: {forbidden}")

print("PASS local source-ready pull-test index")
