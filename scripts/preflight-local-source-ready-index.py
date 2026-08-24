#!/usr/bin/env python3
"""Fail-closed guard for the consolidated LOCAL_ONLY source-ready handoff."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

OLD_SHARED_SHA = "0062e0cd73a570a7ca774dfa8b3ff91e8df20f31"
WALL_CONTACT_BRANCH = "agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh"
WALL_CONTACT_SOURCE_FIX_SHA = "cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb"
WALL_CONTACT_RUNNER = "scripts/run-local-v25-wall-contact-3681.ps1"
REVIEW_RUNTIME_SHA = "9cfff87262d7a7117c5ef1f03b486271a0723fa3"
REVIEW_PR = "#3693"


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
if not INBOX.is_file():
    fail("docs/LOCAL-AGENT-INBOX.md is missing")

text = INDEX.read_text(encoding="utf-8")
dispatch_text = DISPATCH.read_text(encoding="utf-8")
inbox_text = INBOX.read_text(encoding="utf-8")

required_literals = (
    "Status: `SOURCE_READY / LOCAL_RUN_ONLY`",
    "Lane-Key: `issue-3680`",
    WALL_CONTACT_BRANCH,
    OLD_SHARED_SHA,
    WALL_CONTACT_SOURCE_FIX_SHA,
    WALL_CONTACT_RUNNER,
    "Do not rerun the obsolete #3593 P06 binary.",
    "PR #3616 is merged (`12b5f0d7d8549d8b107a1b921d2bb431f809bf69`)",
    "LOCAL-019",
    REVIEW_PR,
    REVIEW_RUNTIME_SHA,
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
    "Status: `LOCAL_READY / PULL_RUN_ONLY`",
    "Source defect/fix: #3687 / #3692",
    f"Required source-fix ancestor: `{WALL_CONTACT_SOURCE_FIX_SHA}`",
    f"Runnable carrier: `{WALL_CONTACT_BRANCH}`",
    WALL_CONTACT_RUNNER,
    "gross 2.6688 - contact 0.3200 = net 2.3488 m²",
):
    if literal not in dispatch_text:
        fail(f"required #3681 pull/run dispatch text is missing: {literal}")

for literal in (
    "## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate",
    "- Status: PASS",
    REVIEW_RUNTIME_SHA,
):
    if literal not in inbox_text:
        fail(f"required LOCAL-019 licensed handoff evidence is missing from inbox: {literal}")

rows = re.findall(r"^\| (LOCAL-\d{3}) \|", text, flags=re.MULTILINE)
expected = [f"LOCAL-{number:03d}" for number in range(1, 20)]
if rows != expected:
    fail(f"LOCAL row order/cardinality drifted: expected {expected}, got {rows}")

for local_id in expected:
    count = rows.count(local_id)
    if count != 1:
        fail(f"{local_id} must appear exactly once in the canonical matrix, got {count}")

index_1744 = f"| P0 | #1744 | `agent/control01/slabopen-undo-semantic-1744` | `{OLD_SHARED_SHA}` |"
index_3613 = f"| P1 | #3613 | `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom` | `{OLD_SHARED_SHA}` |"
index_3681 = (
    f"| P0 | #3681 | `{WALL_CONTACT_BRANCH}` | exact validated runner SHA published on #3681; "
    f"must contain `{WALL_CONTACT_SOURCE_FIX_SHA}` | fetch exact SHA, run `{WALL_CONTACT_RUNNER}` only |"
)
for expected_row in (index_1744, index_3613, index_3681):
    if expected_row not in text:
        fail(f"exact dispatch row drifted: {expected_row}")

local_019_row = (
    f"| LOCAL-019 | P0 / PASS | Six-sheet QS Review export + Excel-to-Model Locate source landed "
    f"through PR {REVIEW_PR}; licensed V25/V26 exact-SHA qualification passed on `{REVIEW_RUNTIME_SHA}`. |"
)
if local_019_row not in text:
    fail("LOCAL-019 source/runtime completion row drifted")

stale_3681_rows = (
    f"| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep` | `{OLD_SHARED_SHA}` |",
    f"| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3687-structwall-brep-contact-fix` | `{WALL_CONTACT_SOURCE_FIX_SHA}` |",
)
for stale in stale_3681_rows:
    if stale in text:
        fail("#3681 regressed to a historical source-only carrier instead of the committed pull/run carrier")

for relative in (
    "docs/LOCAL-DISPATCH-READY-2026-08-24.md",
    "docs/LOCAL-AGENT-INBOX.md",
    "docs/LOCAL-V25-QUALIFICATION.md",
    "docs/LOCAL-V26-QUALIFICATION.md",
    "docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md",
    "scripts/run-local-v25-qualification.ps1",
    WALL_CONTACT_RUNNER,
    "tests/QS3D.BricsCAD.V25.LocalQualification/QS3D.BricsCAD.V25.LocalQualification.csproj",
    "tests/QS3D.BricsCAD.V25.LocalQualification/WallContact3681QualificationCommands.cs",
    "scripts/test-bricscad-review-workbook-roundtrip.ps1",
):
    require_file(relative)

for forbidden in (
    "P03 is separately qualified on PR #3616 but not yet integrated",
    "#3621 remains SOURCE_FIX_REQUIRED; do not rerun the unchanged P06 binary",
):
    if forbidden in text:
        fail(f"stale local scheduling text reintroduced: {forbidden}")

print("PASS local source-ready pull-test index")
