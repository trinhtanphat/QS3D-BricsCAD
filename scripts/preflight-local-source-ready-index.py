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


def require_tokens(text: str, label: str, tokens: tuple[str, ...]) -> None:
    for token in tokens:
        if token not in text:
            fail(f"{label} contract is missing: {token}")


for relative in (
    "docs/LOCAL-SOURCE-READY-INDEX-2026-08-24.md",
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

index = INDEX.read_text(encoding="utf-8")
dispatch = DISPATCH.read_text(encoding="utf-8")
inbox = INBOX.read_text(encoding="utf-8")

require_tokens(
    index,
    "source-ready index",
    (
        "Status: `SOURCE_READY / LOCAL_RUN_ONLY`",
        "Lane-Key: `issue-3680`",
        WALL_CONTACT_BRANCH,
        OLD_SHARED_SHA,
        WALL_CONTACT_SOURCE_FIX_SHA,
        WALL_CONTACT_RUNNER,
        "Do not rerun the obsolete #3593 P06 binary.",
        "PR #3616 is merged",
        "LOCAL-019",
        REVIEW_PR,
        REVIEW_RUNTIME_SHA,
        "A local result is one of:",
        "`PASS`",
        "`FAIL`",
        "`NO_RESULT`",
        "Never commit BricsCAD proprietary DLLs",
    ),
)

require_tokens(
    dispatch,
    "#3681 dispatch",
    (
        "Status: `LOCAL_READY / PULL_RUN_ONLY`",
        "Source defect/fix: #3687 / #3692",
        f"Required source-fix ancestor: `{WALL_CONTACT_SOURCE_FIX_SHA}`",
        f"Runnable carrier: `{WALL_CONTACT_BRANCH}`",
        WALL_CONTACT_RUNNER,
        "gross 2.6688 - contact 0.3200 = net 2.3488 m²",
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
    ),
)

require_tokens(
    inbox,
    "LOCAL-019 inbox evidence",
    (
        "## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate",
        "- Status: PASS",
        REVIEW_RUNTIME_SHA,
    ),
)

rows = re.findall(r"^\| (LOCAL-\d{3}) \|", index, flags=re.MULTILINE)
expected = [f"LOCAL-{number:03d}" for number in range(1, 20)]
if rows != expected:
    fail(f"LOCAL row order/cardinality drifted: expected {expected}, got {rows}")

# Lock the three immediate dispatches without coupling the guard to the prose that
# surrounds them. The exact row payload may gain explanatory text, but carrier/SHA
# identity and the one-command #3681 runner must not drift.
for token in (
    f"| P0 | #1744 | `agent/control01/slabopen-undo-semantic-1744` | `{OLD_SHARED_SHA}` |",
    f"| P1 | #3613 | `agent/qs3d-uix-worker-b/issue-3613-coordination-locate-zoom` | `{OLD_SHARED_SHA}` |",
    f"| P0 | #3681 | `{WALL_CONTACT_BRANCH}` |",
    f"must contain `{WALL_CONTACT_SOURCE_FIX_SHA}`",
    f"run `{WALL_CONTACT_RUNNER}` only",
    "| LOCAL-019 | P0 / PASS |",
):
    if token not in index:
        fail(f"canonical dispatch/evidence identity drifted: {token}")

for stale in (
    "| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep` |",
    "| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3687-structwall-brep-contact-fix` |",
    "P03 is separately qualified on PR #3616 but not yet integrated",
    "#3621 remains SOURCE_FIX_REQUIRED; do not rerun the unchanged P06 binary",
):
    if stale in index:
        fail(f"stale local scheduling/carrier text reintroduced: {stale}")

print("PASS local source-ready pull-test index")
