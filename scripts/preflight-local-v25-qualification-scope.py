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
        "schema = 3",
        "$sourceBuildCompleted = $false",
        "$runtimeSmokeCompleted = $false",
        "$packageCompleted = $false",
        "$signingQualified = $false",
        "$sourceBuildCompleted = $true",
        "$runtimeSmokeCompleted = $true",
        "$packageCompleted = $true",
        "$signingQualified = $true",
        "automatedGateStatus = $automatedGateStatus",
        "sourceBuildStatus = $sourceBuildStatus",
        "runtimeSmokeStatus = $runtimeSmokeStatus",
        "packageStatus = $packageStatus",
        "signingStatus = $signingStatus",
        'fullInteractiveMatrixStatus = "NOT_RUN"',
        "customerReleaseQualified = $false",
        "qualificationScope = $qualificationScope",
        'if ($sourceBuildCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }',
        'if ($SkipRuntime) { "NOT_RUN" } elseif ($runtimeSmokeCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }',
        'if (-not $Package) { "NOT_REQUESTED" } elseif ($packageCompleted) { "PASS" } else { "FAIL_OR_INCOMPLETE" }',
        'if (-not $SignPackage) { "NOT_REQUESTED" } elseif ($signingQualified) { "PASS" } else { "FAIL_OR_INCOMPLETE" }',
        'if ($signingQualified) { "source-build+runtime-smoke+package+authenticode" }',
        'elseif ($runtimeSmokeCompleted -and $packageCompleted) { "source-build+runtime-smoke+package" }',
        'elseif ($runtimeSmokeCompleted) { "source-build+runtime-smoke" }',
        'elseif ($sourceBuildCompleted) { "source-build" } else { "incomplete" }',
        "AUTOMATED SOURCE/BUILD GATES PASS",
        "AUTOMATED SOURCE/BUILD + LICENSED V25 NETLOAD/RIBBON/PALETTE SMOKE PASS",
        "AUTOMATED SOURCE/BUILD + LICENSED V25 RUNTIME + SIGNED/FINALIZED PACKAGE GATES PASS",
        "FULL INTERACTIVE/PRIVATE-DWG PRODUCT MATRIX: NOT RUN by this script.",
        "This does not replace the manual/private-DWG scenario checklist in docs/LOCAL-V25-QUALIFICATION.md.",
        "Customer release qualification remains false until docs/LOCAL-V25-QUALIFICATION.md is executed and recorded for the same SHA/package.",
    )
    for token in required:
        if token not in text:
            errors.append("local V25 runner missing evidence-scope contract: " + token)

    forbidden = (
        'Write-Host "AUTOMATED LOCAL V25 QUALIFICATION PASS',
        "sourceBuildStatus = $automatedGateStatus",
        'customerReleaseQualified = $true',
        'fullInteractiveMatrixStatus = "PASS"',
    )
    for token in forbidden:
        if token in text:
            errors.append("local V25 runner overclaims or conflates automated evidence scope: " + token)

if not runbook.is_file():
    errors.append("missing canonical local V25 qualification runbook")

print("QS3D local V25 qualification evidence-scope preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: schema-3 local evidence preserves independent source/build, runtime-smoke, package and Authenticode states, labels the interactive/private-DWG product matrix NOT_RUN, and cannot claim customer-release qualification from automation/signing alone.")
