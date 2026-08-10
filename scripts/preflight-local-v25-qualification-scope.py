#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
runner = ROOT / "scripts/run-local-v25-qualification.ps1"
runbook = ROOT / "docs/LOCAL-V25-QUALIFICATION.md"

if not runner.is_file():
    errors.append("missing canonical local V25 qualification runner")
else:
    text = runner.read_text(encoding="utf-8")
    required = (
        "schema = 2",
        "automatedGateStatus = $automatedGateStatus",
        "sourceBuildStatus = $automatedGateStatus",
        "runtimeSmokeStatus = $runtimeSmokeStatus",
        'fullInteractiveMatrixStatus = "NOT_RUN"',
        "customerReleaseQualified = $false",
        "qualificationScope = $qualificationScope",
        'if ($SkipRuntime) { "source-build" } else { "source-build+runtime-smoke" }',
        'if ($SkipRuntime) { "NOT_RUN" } elseif ($null -eq $fatal) { "PASS" } else { "FAIL_OR_INCOMPLETE" }',
        "AUTOMATED SOURCE/BUILD GATES PASS",
        "AUTOMATED SOURCE/BUILD + LICENSED V25 NETLOAD/RIBBON/PALETTE SMOKE PASS",
        "FULL INTERACTIVE/PRIVATE-DWG PRODUCT MATRIX: NOT RUN by this script.",
        "Customer release qualification remains false until docs/LOCAL-V25-QUALIFICATION.md is executed and recorded for the same SHA/package.",
    )
    for token in required:
        if token not in text:
            errors.append("local V25 runner missing evidence-scope contract: " + token)

    forbidden = (
        'Write-Host "AUTOMATED LOCAL V25 QUALIFICATION PASS',
        'customerReleaseQualified = $true',
        'fullInteractiveMatrixStatus = "PASS"',
    )
    for token in forbidden:
        if token in text:
            errors.append("local V25 runner overclaims automated evidence scope: " + token)

if not runbook.is_file():
    errors.append("missing canonical local V25 qualification runbook")

print("QS3D local V25 qualification evidence-scope preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the canonical local runner distinguishes automated source/build gates, optional NETLOAD/Ribbon/Palette runtime smoke and the still-manual interactive/private-DWG product matrix; automated evidence cannot claim customer-release qualification.")
