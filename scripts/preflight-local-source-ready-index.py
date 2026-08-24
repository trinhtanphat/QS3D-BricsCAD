#!/usr/bin/env python3
"""Fail-closed guard for the consolidated LOCAL_ONLY source-ready handoff."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"

OLD_SHARED_SHA = "0062e0cd73a570a7ca774dfa8b3ff91e8df20f31"
WALL_CONTACT_BRANCH = "agent/chatgpt-gpt56sol/issue-3687-structwall-brep-contact-fix"
WALL_CONTACT_SHA = "cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb"


def fail(message: str) -> None:
    print(f"ERROR: LOCAL_ONLY source-ready index guard failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require_file(relative: str) -> None:
    path = ROOT / relative
    if not path.is_file():
        fail(f"required committed runner/runbook is missing: {relative}")


if not INDEX.is_file():
    fail("docs/LOCAL-SOURCE-READY-INDEX-2026-08-24.md is missing")
if not DISPATCH.is_file():
    fail("docs/LOCAL-DISPATCH-READY-2026-08-24.md is missing")

text = INDEX.read_text(encoding="utf-8")
dispatch_text = DISPATCH.read_text(encoding="utf-8")

required_literals = (
    "Status: `SOURCE_READY / LOCAL_RUN_ONLY`",
    "Lane-Key: `issue-3680`",
    "agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh",
    OLD_SHARED_SHA,
    WALL_CONTACT_BRANCH,
    WALL_CONTACT_SHA,
    "Do not rerun the obsolete #3681 binary.",
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

for literal in (
    "Status: `LOCAL_READY / PENDING_LOCAL_RERUN`",
    "Source defect/fix: #3687 / #3692",
    f"Source-ready carrier: `{WALL_CONTACT_BRANCH}`",
    f"Exact SHA: `{WALL_CONTACT_SHA}`",
    f"The previous tested SHA `{OLD_SHARED_SHA}` is historical failing evidence only",
):
    if literal not in dispatch_text:
        fail(f"required #3681 rerun dispatch text is missing: {literal}")

rows = re.findall(r"^\| (LOCAL-\d{3}) \|", text, flags=re.MULTILINE)
expected = [f"LOCAL-{number:03d}" for number in range(1, 19)]
if rows != expected:
    fail(f"LOCAL row order/cardinality drifted: expected {expected}, got {rows}")

for local_id in expected:
    count = rows.count(local_id)
    if count != 1:
        fail(f"{local_id} must appear exactly once in the canonical matrix, got {count}")

# #1744 and #3613 intentionally remain on the original exact carrier while #3681
# has a newer source-fix carrier after #3687/#3692. Lock the row-specific pins so
# a shared historical SHA cannot accidentally be reused for the wall-contact rerun.
index_1744 = f"| P0 | #1744 | `agent/control01/slabopen-undo-semantic-1744` | `{OLD_SHARED_SHA}` |"
index_3613 = f"| P1 | #3613 | `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom` | `{OLD_SHARED_SHA}` |"
index_3681 = f"| P0 | #3681 | `{WALL_CONTACT_BRANCH}` | `{WALL_CONTACT_SHA}` |"
for expected_row in (index_1744, index_3613, index_3681):
    if expected_row not in text:
        fail(f"exact dispatch row drifted: {expected_row}")

stale_3681_index = (
    f"| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep` | `{OLD_SHARED_SHA}` |"
)
stale_3681_dispatch = (
    "Source-ready carrier: `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep`"
)
if stale_3681_index in text:
    fail("#3681 regressed to its historical failing index carrier")
if stale_3681_dispatch in dispatch_text:
    fail("#3681 regressed to its historical failing dispatch carrier")

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

# The consolidated handoff must not regress to stale scheduling statements that
# current issue/PR dispositions explicitly supersede.
for forbidden in (
    "P03 is separately qualified on PR #3616 but not yet integrated",
    "#3621 remains SOURCE_FIX_REQUIRED; do not rerun the unchanged P06 binary",
):
    if forbidden in text:
        fail(f"stale local scheduling text reintroduced: {forbidden}")

print("PASS local source-ready pull-test index")
