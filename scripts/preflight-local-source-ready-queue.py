#!/usr/bin/env python3
"""Fail-closed source guard for the canonical local source-ready dispatcher.

This guard proves repository/source wiring only. Hosted execution never becomes
LOCAL_PASS and this script never executes BricsCAD.
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-source-ready-queue.ps1"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
LOCAL_011 = ROOT / "scripts" / "run-local-v25-local-011.ps1"
MERGED_3905 = "07ccc293d1a9cf9ea1524b0fcf38b062bf305431"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"ERROR: local source-ready queue preflight failed: {message}")


def block(text: str, heading_pattern: str) -> str:
    match = re.search(
        rf"(?ms)^## {heading_pattern}[^\r\n]*\r?\n(?P<body>.*?)(?=^## |\Z)", text
    )
    require(match is not None, f"missing inbox block matching {heading_pattern!r}")
    return match.group("body")


require(RUNNER.is_file(), "missing scripts/run-local-source-ready-queue.ps1")
require(INBOX.is_file(), "missing canonical docs/LOCAL-AGENT-INBOX.md")
require(LOCAL_011.is_file(), "missing merged #3905 LOCAL-011 runner")
runner = RUNNER.read_text(encoding="utf-8")
inbox = INBOX.read_text(encoding="utf-8")
local_011 = LOCAL_011.read_text(encoding="utf-8")

# The dispatcher consumes the single live inbox and refuses completed rows.
require("docs\\LOCAL-AGENT-INBOX.md" in runner, "dispatcher does not bind the canonical inbox")
require("Get-CanonicalLocalQueue" in runner, "dispatcher lacks canonical inbox parsing")
require("$row.status -eq 'PASS' -or $row.noRerun" in runner, "PASS/NO_RERUN refusal is missing")
require("fullLocalPass = $false" in runner, "dispatcher could mislabel bounded execution as LOCAL_PASS")
require("customerReleaseQualified = $false" in runner, "dispatcher could mislabel customer release readiness")
require("MANUAL_OR_EXTERNAL" in runner, "dispatcher lacks explicit non-automatable boundaries")
require("UNMAPPED_FAIL_CLOSED" in runner, "unknown inbox rows must fail closed")

# #3681 remains authoritative completed/no-rerun truth.
wall_contact = block(inbox, r"P0 — #3681")
require(re.search(r"(?m)^- Status: PASS\s*$", wall_contact) is not None, "#3681 must remain PASS")
require("NO_RERUN" in wall_contact, "#3681 must remain NO_RERUN")
require(
    "'#3681'" in runner and "COMPLETED_NO_RERUN" in runner,
    "dispatcher does not preserve #3681 no-rerun contract",
)

# LOCAL-011 source preparation is merged by #3905. The queue entrypoint must
# refuse old heads and delegate to that committed interactive runner rather than
# reconstructing the 21 native/manual rows remotely.
local_011_inbox = block(inbox, r"LOCAL-011")
require("REMOTE_DONE / PENDING_LOCAL / DO_NOT_RETRY_REMOTE" in local_011_inbox, "LOCAL-011 remote disposition drifted")
require("scripts/run-local-v25-local-011.ps1" in local_011_inbox, "LOCAL-011 inbox handoff lost its committed runner")
require("'LOCAL-011'" in runner and "INTERACTIVE_RUNBOOK" in runner, "LOCAL-011 queue contract is missing")
require("'run-local-v25-local-011.ps1'" in runner, "LOCAL-011 dedicated runner is not delegated")
require(MERGED_3905 in runner, "LOCAL-011 queue contract is not pinned to exact merged #3905")
require("merge-base --is-ancestor" in runner, "minimum merged carrier ancestry is not enforced")
require("localPassClaimedByRunner=$false" in local_011.replace(" ", ""), "LOCAL-011 runner could claim LOCAL_PASS")
require("PASS/FAIL/BLOCKED/NO_RESULT" in runner, "LOCAL-011 bounded result contract is not explicit")

# Verify every concrete runner referenced by the dispatcher exists.
referenced = sorted(set(re.findall(r"'((?:run|test)-[^']+\.ps1)'", runner)))
require(referenced, "dispatcher references no concrete runner scripts")
missing = [name for name in referenced if not (ROOT / "scripts" / name).is_file()]
require(not missing, f"dispatcher references missing runner(s): {', '.join(missing)}")

# Lock orchestration calls to the committed runner parameter contracts. This
# catches renamed/removed parameters before a licensed local run is spent.
runner_parameter_contracts = {
    "run-local-v25-qualification.ps1": (
        "$BricsCadDir", "$Profile", "$ArtifactDir", "$PythonPath", "$SkipScreenshot"
    ),
    "run-local-v25-local-011.ps1": (
        "$BricsCadDir", "$Profile", "$ArtifactDir"
    ),
    "test-bricscad-v25-level-z.ps1": (
        "$BricsCadDir", "$PluginDll", "$DrawingCopy", "$Profile", "$ArtifactDir",
        "$ExpectedSourceSha", "$ConfirmDisposableCopy", "$NativeDrawingUnit"
    ),
    "test-bricscad-v25-level-z-lifecycle.ps1": (
        "$BricsCadDir", "$PluginDll", "$DrawingCopy", "$Profile", "$ArtifactDir",
        "$ExpectedSourceSha", "$ConfirmDisposableCopy"
    ),
    "test-bricscad-v25-source-reconcile.ps1": (
        "$BricsCadDir", "$PluginDll", "$FixtureDwg", "$Profile", "$ArtifactDir",
        "$ConfirmDisposableCopies"
    ),
    "test-bricscad-v25-brc-probe.ps1": (
        "$BricsCadDir", "$PluginDll", "$DrawingCopy", "$Profile", "$ArtifactDir",
        "$ConfirmReferenceCopy"
    ),
    "test-bricscad-v25-brc-quantity-roundtrip.ps1": (
        "$BricsCadDir", "$PluginDll", "$DrawingCopy", "$Profile", "$ArtifactDir",
        "$ConfirmReferenceCopy"
    ),
}
for name, parameters in runner_parameter_contracts.items():
    path = ROOT / "scripts" / name
    require(path.is_file(), f"parameter-contract runner is missing: {name}")
    text = path.read_text(encoding="utf-8")
    absent = [parameter for parameter in parameters if parameter not in text]
    require(not absent, f"runner {name} lost parameter(s): {', '.join(absent)}")

# LOCAL-016's already-qualified bounded package lifecycle must stay impossible
# to dispatch again from this queue even though its broader parent remains open.
require(
    "'LOCAL-016'" in runner
    and "COMPLETED_NO_RERUN" in runner
    and "e90c6aba7ef7bf903042d42dd991f9e7112fe659" in runner,
    "LOCAL-016 completed licensed boundary is missing",
)
require(
    "test-v26-package-install-lifecycle.ps1" not in runner,
    "LOCAL-016 completed lifecycle runner became executable again",
)

# These are the currently source-ready executable handoff runners. Adding a new
# executable lane requires deliberate guard expansion.
required_runner_names = {
    "run-local-v25-qualification.ps1",
    "run-local-v25-local-011.ps1",
    "test-bricscad-v25-level-z.ps1",
    "test-bricscad-v25-level-z-lifecycle.ps1",
    "test-bricscad-v25-source-reconcile.ps1",
    "test-bricscad-v25-brc-probe.ps1",
    "test-bricscad-v25-brc-quantity-roundtrip.ps1",
}
require(required_runner_names.issubset(set(referenced)), "source-ready automated runner set drifted")

print("PASS: canonical local source-ready queue dispatcher is fail-closed, #3905-aware and runner-complete")
