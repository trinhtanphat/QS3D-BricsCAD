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
WALL_CONTACT_EVIDENCE_PR = "#3849"
WALL_CONTACT_EVIDENCE_MERGE = "7fec6f36a7c1181d7113f0e7220ea3dafca66e29"
WALL_CONTACT_RUNNER = "scripts/run-local-v25-wall-contact-3681.ps1"
WALL_CONTACT_RUNNER_NAME = Path(WALL_CONTACT_RUNNER).name
LOCAL001_SOURCE_READY_SHA = "ab0202194e33a1a27dbdf322b9b6d73b9d56778a"
LOCAL001_SOURCE_FIX_ISSUE = "#3930"
LOCAL001_SOURCE_FIX_PR = "#3932"
LOCAL005_SOURCE_MERGE = "ba6e1c7508086beb8ac5db9a4a78d2c43fc09492"
LOCAL005_EVIDENCE_PR = "#3735"
LOCAL005_EVIDENCE_MERGE = "73fec2c48726c09196b773c117be77ee1983031e"
LOCAL006_SOURCE_MERGE = "887173f28126b928765e458f28202e83a6f3b88f"
LOCAL006_RUNTIME_SHA = "a572ab0a350f54f8e994ac1e91f825907646af9c"
LOCAL006_EVIDENCE_PR = "#3777"
LOCAL006_EVIDENCE_MERGE = "7f30d019a97d36c025c34a4e08364ef3bd73ffad"
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


def require_local_row_status(text: str, row: str, next_row: str, status: str) -> None:
    match = re.search(
        rf"^## {re.escape(row)}\b.*?(?=^## {re.escape(next_row)}\b)",
        text,
        flags=re.MULTILINE | re.DOTALL,
    )
    if match is None:
        fail(f"canonical inbox row is missing or cannot be bounded: {row}")
    expected = f"- Status: {status}"
    if expected not in match.group(0):
        fail(f"{row} must publish truthful top-level status {status}")


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
        "LOCAL-001 | P0 / IN_PROGRESS",
        LOCAL001_SOURCE_READY_SHA,
        LOCAL001_SOURCE_FIX_ISSUE,
        LOCAL001_SOURCE_FIX_PR,
        "LOCAL-005 | P1 / OPEN",
        LOCAL005_SOURCE_MERGE,
        LOCAL005_EVIDENCE_PR,
        LOCAL005_EVIDENCE_MERGE,
        "LOCAL-006 | P1 / OPEN",
        LOCAL006_SOURCE_MERGE,
        LOCAL006_RUNTIME_SHA,
        LOCAL006_EVIDENCE_PR,
        LOCAL006_EVIDENCE_MERGE,
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
    "#3681 completed dispatch history",
    (
        "## Completed bounded references — DO_NOT_RERUN",
        "#3681 StructuralWall live-BREP concrete-contact/formwork",
        "accepted licensed V25 `LOCAL_PASS`",
        WALL_CONTACT_EXACT_RUNTIME_SHA,
        WALL_CONTACT_EVIDENCE_PR,
        WALL_CONTACT_EVIDENCE_MERGE,
        "Status: `COMPLETED / DO_NOT_RERUN`",
        f"Minimum source-ready ancestor: `{WALL_CONTACT_SOURCE_READY_FLOOR_SHA}`",
        f"Exact runtime source: `{WALL_CONTACT_EXACT_RUNTIME_SHA}`",
        f"Accepted evidence: PR {WALL_CONTACT_EVIDENCE_PR} / merge `{WALL_CONTACT_EVIDENCE_MERGE}`",
        WALL_CONTACT_RUNNER_NAME,
        "Do not execute it by default.",
        "touching-only",
        "0.05 m penetration regression",
        "gross 2.6688 - contact 0.3200 = net 2.3488 m²",
    ),
)

wall_heading = "## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork"
if wall_heading not in inbox:
    fail("#3681 canonical inbox block is missing")
wall_block = inbox.split(wall_heading, 1)[1].split("\n## ", 1)[0]

require_tokens(
    wall_block,
    "#3681 canonical inbox",
    (
        "- Priority: P0",
        "- Status: PASS",
        "- Remote disposition: COMPLETED / NO_RERUN",
        WALL_CONTACT_EXACT_RUNTIME_SHA,
        WALL_CONTACT_SOURCE_READY_FLOOR_SHA,
        WALL_CONTACT_RUNNER,
        WALL_CONTACT_EVIDENCE_PR,
        WALL_CONTACT_EVIDENCE_MERGE,
        "LOCAL_PASS",
        "do not rerun",
    ),
)
for stale in (
    "- Status: OPEN",
    "Evidence: PENDING_LOCAL",
    WALL_CONTACT_SUPERSEDED_RUNTIME_SHA,
):
    if stale in wall_block:
        fail(f"#3681 canonical inbox reintroduced completed-state stale data: {stale}")

require_local_row_status(inbox, "LOCAL-001", "LOCAL-002", "IN_PROGRESS")

require_tokens(
    index,
    "LOCAL-001 source-ready continuation",
    (
        "| LOCAL-001 | P0 / IN_PROGRESS |",
        LOCAL001_SOURCE_FIX_ISSUE,
        LOCAL001_SOURCE_FIX_PR,
        LOCAL001_SOURCE_READY_SHA,
        "scripts/run-local-v25-qualification.ps1",
        "Do not patch production source locally",
    ),
)
if "| LOCAL-001 | P0 / PASS |" in index:
    fail("LOCAL-001 source-ready index must not claim PASS while the canonical inbox remains IN_PROGRESS")

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

require_tokens(
    index,
    "#3681 completed source-ready history",
    (
        "#1744, #3613 and #3681 already have accepted licensed bounded PASS evidence",
        "## Completed #3681 licensed wall-contact qualification — DO_NOT_RERUN",
        WALL_CONTACT_EXACT_RUNTIME_SHA,
        WALL_CONTACT_SOURCE_READY_FLOOR_SHA,
        WALL_CONTACT_TOUCHING_PROBE_FLOOR_SHA,
        WALL_CONTACT_EVIDENCE_PR,
        WALL_CONTACT_EVIDENCE_MERGE,
        "COMPLETED / DO_NOT_RERUN",
        "Do not schedule or execute it again",
    ),
)

require_tokens(
    index,
    "LOCAL-005 accepted bounded evidence",
    (
        "| LOCAL-005 | P1 / OPEN |",
        "LOCAL-005 post-#3715 build -> native Undo -> native Redo",
        "`LOCAL_PASS`",
        LOCAL005_SOURCE_MERGE,
        LOCAL005_EVIDENCE_PR,
        LOCAL005_EVIDENCE_MERGE,
        "Do not repeat the accepted build -> native Undo -> native Redo cell solely because this index changed.",
    ),
)

require_tokens(
    index,
    "LOCAL-006 accepted bounded evidence",
    (
        "| LOCAL-006 | P1 / OPEN |",
        "LOCAL-006 post-#3721 `QS3DTAG -> native Undo -> native Redo`",
        "`BOUNDED_LOCAL_PASS / OVERALL_IN_PROGRESS`",
        LOCAL006_SOURCE_MERGE,
        LOCAL006_RUNTIME_SHA,
        LOCAL006_EVIDENCE_PR,
        LOCAL006_EVIDENCE_MERGE,
        "Do not repeat the accepted `QS3DTAG -> native Undo -> native Redo` cell solely because this index changed.",
    ),
)

for token in (
    "| LOCAL-001 | P0 / IN_PROGRESS |",
    "| LOCAL-005 | P1 / OPEN |",
    "| LOCAL-006 | P1 / OPEN |",
    "| LOCAL-019 | P0 / PASS |",
):
    if token not in index:
        fail(f"canonical dispatch/evidence identity drifted: {token}")

for stale in (
    f"| P0 | #3681 | exact published descendant recorded on #3681 / #72 | must contain `{WALL_CONTACT_SOURCE_READY_FLOOR_SHA}`",
    f"fetch the exact published SHA, run `{WALL_CONTACT_RUNNER}` only",
    "Status: `LOCAL_READY / PULL_RUN_ONLY`",
    "Exact runnable SHA: published on #3681 and #72 after this carrier's protected branch CI succeeds.",
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
    "rerun bounded multi-region build -> native Undo -> native Redo first",
    "run multi-region build -> native Undo -> native Redo first",
    "rerun bounded `QS3DTAG -> native Undo -> native Redo` first",
    "run `QS3DTAG -> native Undo -> native Redo` first",
):
    if stale in index or stale in dispatch:
        fail(f"stale local scheduling/carrier text reintroduced: {stale}")

print("PASS local source-ready pull-test index with truthful LOCAL-001 continuation, completed bounded LOCAL-005/006 evidence, and #3681 exact licensed PASS/no-rerun semantics")
