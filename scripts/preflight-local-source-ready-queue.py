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
CI = ROOT / ".github" / "workflows" / "ci.yml"


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
require(CI.is_file(), "missing shared CI workflow")
runner = RUNNER.read_text(encoding="utf-8")
inbox = INBOX.read_text(encoding="utf-8")
ci = CI.read_text(encoding="utf-8")

# The dispatcher reads live priority/status from the canonical queue instead of
# embedding a second queue.
require("docs\\LOCAL-AGENT-INBOX.md" in runner, "dispatcher does not bind the canonical inbox")
require("Get-CanonicalLocalQueue" in runner, "dispatcher lacks canonical inbox parsing")
require("$row.status -eq 'PASS' -or $row.noRerun" in runner, "PASS/NO_RERUN refusal is missing")
require("fullLocalPass = $false" in runner, "dispatcher could mislabel bounded execution as LOCAL_PASS")
require("customerReleaseQualified = $false" in runner, "dispatcher could mislabel customer release readiness")
require("MANUAL_OR_EXTERNAL" in runner, "dispatcher lacks an explicit non-automatable boundary")
require("UNMAPPED_FAIL_CLOSED" in runner, "unknown inbox rows must fail closed")
require(
    "scripts/run-local-source-ready-queue.ps1" in ci,
    "dispatcher is not covered by the shared CI PowerShell parse gate",
)

# #3681 is authoritative completed/no-rerun truth. Its runner may remain only as
# a non-executable regression reference.
wall_contact = block(inbox, r"P0 — #3681")
require(re.search(r"(?m)^- Status: PASS\s*$", wall_contact) is not None, "#3681 must remain PASS")
require("NO_RERUN" in wall_contact, "#3681 must remain NO_RERUN")
require(
    "'#3681'" in runner and "COMPLETED_NO_RERUN" in runner,
    "dispatcher does not preserve #3681 no-rerun contract",
)

# Verify every concrete runner referenced by the dispatcher exists.
referenced = sorted(set(re.findall(r"'((?:run|test)-[^']+\.ps1)'", runner)))
require(referenced, "dispatcher references no concrete runner scripts")
missing = [name for name in referenced if not (ROOT / "scripts" / name).is_file()]
require(not missing, f"dispatcher references missing runner(s): {', '.join(missing)}")

# Lock orchestration calls to the committed runner parameter contracts. This
# catches a renamed/removed parameter before a licensed local run is spent.
runner_parameter_contracts = {
    "run-local-v25-qualification.ps1": (
        "$BricsCadDir", "$Profile", "$ArtifactDir", "$PythonPath", "$SkipScreenshot"
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

# Guard historical truth drift at the execution boundary even while the large
# canonical inbox retains provenance text from earlier pending states.
require(
    "'LOCAL-007'" in runner and "COMPLETED_BOUNDED" in runner,
    "LOCAL-007 P01-P03 completion boundary is missing",
)
require(
    "#3593/#3621" in runner and "do not rerun superseded H.1 P01-P06" in runner,
    "LOCAL-002 H.1 completed-boundary warning is missing",
)

# #3878 received the one intended licensed rerun and LOCAL_PASS on exact source
# e90c6aba7ef7bf903042d42dd991f9e7112fe659. It must now be impossible for the
# source-ready dispatcher to spend another package-install lifecycle run.
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
for obsolete_parameter in ("$VersionKey", "$LanguageKey", "$ConfirmDisposableInstall"):
    require(
        obsolete_parameter not in runner,
        f"LOCAL-016 obsolete execution parameter remains exposed: {obsolete_parameter}",
    )

# These are the only currently automated source-ready handoff runners. Adding a
# new executable lane requires deliberate guard expansion.
required_runner_names = {
    "run-local-v25-qualification.ps1",
    "test-bricscad-v25-level-z.ps1",
    "test-bricscad-v25-level-z-lifecycle.ps1",
    "test-bricscad-v25-source-reconcile.ps1",
    "test-bricscad-v25-brc-probe.ps1",
    "test-bricscad-v25-brc-quantity-roundtrip.ps1",
}
require(required_runner_names.issubset(set(referenced)), "source-ready automated runner set drifted")

print("PASS: canonical local source-ready queue dispatcher is fail-closed and runner-complete")
