#!/usr/bin/env python3
"""Fail-closed guard for the consolidated LOCAL_ONLY source-ready handoff."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
INDEX = ROOT / "docs" / "LOCAL-SOURCE-READY-INDEX-2026-08-24.md"
DISPATCH = ROOT / "docs" / "LOCAL-DISPATCH-READY-2026-08-24.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

WALL_CONTACT_SOURCE_READY_FLOOR_SHA = "c64eb8c1b83761e155da670904a72e64669464b7"
WALL_CONTACT_TOUCHING_PROBE_FLOOR_SHA = "4d6830a9e2ed315e0d4f8fcec0c708ad27727fb0"
WALL_CONTACT_EXACT_RUNTIME_SHA = "a4f1a53683a9296532a0290fcb79bc49b9d4b892"
WALL_CONTACT_SUPERSEDED_RUNTIME_SHA = "447ba9805d777a3225827117587d932135cf0959"
WALL_CONTACT_RUNNER = "scripts/run-local-v25-wall-contact-3681.ps1"
WALL_CONTACT_RUNNER_NAME = Path(WALL_CONTACT_RUNNER).name
LOCAL005_SOURCE_MERGE = "ba6e1c7508086beb8ac5db9a4a78d2c43fc09492"
LOCAL006_SOURCE_MERGE = "887173f28126b928765e458f28202e83a6f3b88f"
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
    "tests/QS3D.BricsCAD.V25.LocalQualification/WallContact3681SourceFixGateCommands.cs",
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
        WALL_CONTACT_SOURCE_READY_FLOOR_SHA,
        WALL_CONTACT_TOUCHING_PROBE_FLOOR_SHA,
        "#3729",
        "#3833",
        "#3836",
        WALL_CONTACT_RUNNER,
        "touching-only",
        "0.05 m penetration",
        "LOCAL-005 | P1 / SOURCE_FIX_READY",
        LOCAL005_SOURCE_MERGE,
        "LOCAL-006 | P1 / SOURCE_FIX_READY",
        LOCAL006_SOURCE_MERGE,
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
        "Touching-probe floor source defect/fixes: #3711 / #3716 / #3729",
        "Harness-minimum correction: #3754 / #3833",
        "Finite touching-footprint correction: #3770 / #3836",
        f"Minimum source-ready ancestor: `{WALL_CONTACT_SOURCE_READY_FLOOR_SHA}`",
        "Exact runnable SHA: published on #3681 and #72 after this carrier's protected branch CI succeeds.",
        WALL_CONTACT_RUNNER_NAME,
        "touching-only",
        "0.05 m penetration",
        "fails fast before the broader matrix",
        "gross 2.6688 - contact 0.3200 = net 2.3488 m²",
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
    ),
)

require_tokens(
    inbox,
    "#3681 canonical inbox",
    (
        "## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork",
        "- Priority: P0",
        "- Status: OPEN",
        "- Remote disposition: SOURCE_READY / LOCAL_RUN_ONLY",
        WALL_CONTACT_EXACT_RUNTIME_SHA,
        WALL_CONTACT_SOURCE_READY_FLOOR_SHA,
        WALL_CONTACT_RUNNER,
        "LOCAL_PASS",
        "LOCAL_FAIL",
        "NO_RESULT",
        "local worker must not edit production source",
    ),
)
if WALL_CONTACT_SUPERSEDED_RUNTIME_SHA in inbox:
    fail("#3681 canonical inbox reintroduced the superseded pre-#3846 runtime SHA")

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

for token in (
    f"| P0 | #3681 | exact published descendant recorded on #3681 / #72 | must contain `{WALL_CONTACT_SOURCE_READY_FLOOR_SHA}` (#3833 + #3836); this descendant also contains `{WALL_CONTACT_TOUCHING_PROBE_FLOOR_SHA}` (#3729) |",
    f"fetch the exact published SHA, run `{WALL_CONTACT_RUNNER}` only",
    f"| LOCAL-005 | P1 / SOURCE_FIX_READY |",
    f"| LOCAL-006 | P1 / SOURCE_FIX_READY |",
    "| LOCAL-019 | P0 / PASS |",
):
    if token not in index:
        fail(f"canonical dispatch/evidence identity drifted: {token}")

for stale in (
    "| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3665-wall-contact-brep` |",
    "| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3687-structwall-brep-contact-fix` |",
    "| P0 | #3681 | `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh` |",
    f"Required source-fix ancestor: `{WALL_CONTACT_TOUCHING_PROBE_FLOOR_SHA}`",
    "Runnable carrier: `agent/chatgpt-gpt56sol/issue-3680-local-dispatch-refresh`",
    f"Exact runnable SHA: `{WALL_CONTACT_SOURCE_READY_FLOOR_SHA}`",
    "must contain `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb`",
    "Required source-fix ancestor: `cb10e04954973aedf77a9cfeebbd28a5ccbcbbdb`",
    "P03 is separately qualified on PR #3616 but not yet integrated",
    "#3621 remains SOURCE_FIX_REQUIRED; do not rerun the unchanged P06 binary",
):
    if stale in index or stale in dispatch:
        fail(f"stale local scheduling/carrier text reintroduced: {stale}")

print("PASS local source-ready pull-test index with #3681 exact post-#3846 runtime SHA, canonical inbox publication and #3833/#3836 source floor")
