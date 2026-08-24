#!/usr/bin/env python3
"""Regression guard for the fail-closed LOCAL-002 H.1 licensed-result contract."""

# Lane-Key: issue-3656
import copy
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "scripts" / "validate-local002-h1-result.py"
EXPECTED_SHA = "ec4384eb6a12ff6763dfdd19d4e4b84747ab60f3"
PLUGIN_SHA256 = "a" * 64
CORE_SHA256 = "b" * 64


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def base_manifest() -> dict:
    return {
        "schemaVersion": "qs3d.local002-h1-result/v1",
        "lane": "LOCAL-002-H1",
        "attempt": "P07",
        "verdict": "PASS",
        "exactSha": EXPECTED_SHA,
        "bricscadProductVersion": "25.2.10.1",
        "pluginSha256": PLUGIN_SHA256,
        "coreSha256": CORE_SHA256,
        "precheck": {
            "focusedGuardsPassed": 16,
            "focusedGuardsTotal": 16,
            "v25BuildWarnings": 0,
            "v25BuildErrors": 0,
            "helperBuildWarnings": 0,
            "helperBuildErrors": 0,
            "coreSmokePass": True,
            "sourceLinkExact": True,
            "zeroBricscadProcessesBefore": True,
        },
        "functional": {
            "aBound": {"status": "PASS", "closed": 13, "expected": 13},
            "bBound": {"status": "PASS", "closed": 2, "expected": 2},
            "wrapperDriftNativeIdentity": "PASS",
            "cBound": "PASS",
            "dynamicHubs": "PASS",
            "projectIsolation": "PASS",
            "repeatCycle": "PASS",
        },
        "finalHost": {
            "status": "PASS",
            "hostMatched": True,
            "processExitCode": "0",
            "gracefulExit": True,
            "applicationErrorCount": 0,
            "werCount": 0,
            "applicationHangCount": 0,
            "dotNetRuntimeErrorCount": 0,
            "accessViolationCount": 0,
        },
        "safety": {
            "publicFixtureUnchanged": True,
            "protectedUserDwgUnchanged": True,
            "demandLoadLoaderUnchanged": True,
            "demandLoadBytesUnchanged": True,
            "loadCtrls": 2,
            "privateStateRestored": True,
            "zeroBricscadProcessesAfter": True,
            "zeroHelperProcessesAfter": True,
            "trackedTreeClean": True,
            "rawEvidenceIgnored": True,
            "sanitizedOnly": True,
        },
    }


def invoke(manifest: dict, expected_sha: str = EXPECTED_SHA):
    with tempfile.TemporaryDirectory(prefix="qs3d-local002-h1-contract-") as temp_dir:
        source = Path(temp_dir) / "result.json"
        source.write_text(json.dumps(manifest, sort_keys=True), encoding="utf-8")
        completed = subprocess.run(
            [
                sys.executable,
                str(VALIDATOR),
                "--input",
                str(source),
                "--expected-sha",
                expected_sha,
            ],
            cwd=str(ROOT),
            check=False,
            text=True,
            capture_output=True,
            timeout=30,
        )
        return completed


def require_valid(manifest: dict, verdict: str, route: str, eligible: bool) -> None:
    completed = invoke(manifest)
    require(
        completed.returncode == 0,
        f"expected valid {verdict} manifest, got rc={completed.returncode}: {completed.stderr.strip()}",
    )
    try:
        output = json.loads(completed.stdout)
    except json.JSONDecodeError as exc:
        raise AssertionError(f"validator stdout must be one JSON object: {completed.stdout!r}") from exc
    require(output.get("verdict") == verdict, f"validator did not preserve {verdict} verdict.")
    require(output.get("route") == route, f"validator routed {verdict} incorrectly: {output!r}")
    require(output.get("localPassEligible") is eligible, f"validator eligibility is wrong for {verdict}.")
    require(output.get("exactSha") == EXPECTED_SHA, "validator must bind routing output to the exact tested SHA.")
    require("input" not in output and "raw" not in output, "validator output must not echo raw manifest content.")


def require_invalid(manifest: dict, message_fragment: str, expected_sha: str = EXPECTED_SHA) -> None:
    completed = invoke(manifest, expected_sha=expected_sha)
    require(completed.returncode != 0, "invalid manifest unexpectedly validated.")
    require(not completed.stdout.strip(), "invalid manifest must not emit a success routing object.")
    require(
        message_fragment.lower() in completed.stderr.lower(),
        f"invalid manifest error did not explain {message_fragment!r}: {completed.stderr!r}",
    )


require(VALIDATOR.is_file(), "LOCAL-002 H.1 result validator is missing.")

valid_pass = base_manifest()
require_valid(valid_pass, "PASS", "LOCAL_PASS_ELIGIBLE", True)

valid_fail = copy.deepcopy(valid_pass)
valid_fail["verdict"] = "FAIL"
valid_fail["finalHost"].update(
    {
        "status": "FAIL",
        "applicationErrorCount": 1,
        "werCount": 1,
        "accessViolationCount": 1,
        "failure": {
            "class": "FINAL_HOST_NATIVE_WPF_TEARDOWN",
            "faultModule": "ucrtbase.dll",
            "exceptionCode": "0xc0000409",
            "werEventName": "BEX64",
            "bricscadReportCode": "C0000005",
            "signatureFamily": "ACRX_WPF_TEARDOWN",
        },
    }
)
require_valid(valid_fail, "FAIL", "SOURCE_DIAGNOSIS_REQUIRED", False)

valid_no_result = copy.deepcopy(valid_pass)
valid_no_result["verdict"] = "NO_RESULT"
valid_no_result["functional"] = {
    "aBound": {"status": "NOT_RUN", "closed": 0, "expected": 13},
    "bBound": {"status": "NOT_RUN", "closed": 0, "expected": 2},
    "wrapperDriftNativeIdentity": "NOT_RUN",
    "cBound": "NOT_RUN",
    "dynamicHubs": "NOT_RUN",
    "projectIsolation": "NOT_RUN",
    "repeatCycle": "NOT_RUN",
}
valid_no_result["finalHost"] = {
    "status": "NOT_RUN",
    "hostMatched": False,
    "processExitCode": "NOT_RUN",
    "gracefulExit": False,
    "applicationErrorCount": 0,
    "werCount": 0,
    "applicationHangCount": 0,
    "dotNetRuntimeErrorCount": 0,
    "accessViolationCount": 0,
}
valid_no_result["noResult"] = {"reasonCode": "STARTUP_OR_HARNESS_FAILURE"}
require_valid(valid_no_result, "NO_RESULT", "BOUNDED_RETRY_REQUIRED", False)

missing_hash = copy.deepcopy(valid_pass)
del missing_hash["coreSha256"]
require_invalid(missing_hash, "coreSha256")

unsafe_pass = copy.deepcopy(valid_pass)
unsafe_pass["finalHost"]["applicationErrorCount"] = 1
require_invalid(unsafe_pass, "PASS")

wrong_sha = copy.deepcopy(valid_pass)
wrong_sha["exactSha"] = "1" * 40
require_invalid(wrong_sha, "expected SHA")

fail_without_classification = copy.deepcopy(valid_fail)
del fail_without_classification["finalHost"]["failure"]
require_invalid(fail_without_classification, "failure")

raw_path = copy.deepcopy(valid_fail)
raw_path["debugPath"] = r"C:\\Users\\someone\\private\\failure.dmp"
require_invalid(raw_path, "forbidden")

print("[OK] LOCAL-002 H.1 result contract is fail-closed for PASS/FAIL/NO_RESULT routing and sanitized exact-SHA evidence.")
