#!/usr/bin/env python3
"""Validate sanitized LOCAL-002 H.1 licensed evidence and emit a fail-closed route."""

# Lane-Key: issue-3656
import argparse
import json
import re
import sys
from pathlib import Path

SCHEMA_VERSION = "qs3d.local002-h1-result/v1"
LANE = "LOCAL-002-H1"
SHA40 = re.compile(r"^[0-9a-fA-F]{40}$")
SHA256 = re.compile(r"^[0-9a-fA-F]{64}$")
ATTEMPT = re.compile(r"^P[0-9]{2,}$")
V25_VERSION = re.compile(r"^25\.[0-9]+\.[0-9]+(?:\.[0-9]+)?$")
SAFE_CODE = re.compile(r"^[A-Za-z0-9_.:+-]{1,96}$")
SAFE_UPPER_TOKEN = re.compile(r"^[A-Z0-9_]{2,96}$")
PROCESS_EXIT = re.compile(r"^(?:0|0x[0-9A-Fa-f]{1,8}|-?[0-9]{1,11}|NOT_RUN)$")

VERDICTS = {"PASS", "FAIL", "NO_RESULT"}
STATUSES = {"PASS", "FAIL", "NOT_RUN"}
NO_RESULT_REASONS = {
    "STARTUP_OR_HARNESS_FAILURE",
    "STARTUP_TIMEOUT",
    "HARNESS_FAILURE",
    "MARKER_MISSING",
    "HOST_NOT_MATCHED",
    "PRECHECK_FAILED",
}

TOP_KEYS = {
    "schemaVersion",
    "lane",
    "attempt",
    "verdict",
    "exactSha",
    "bricscadProductVersion",
    "pluginSha256",
    "coreSha256",
    "precheck",
    "functional",
    "finalHost",
    "safety",
    "noResult",
}
PRECHECK_KEYS = {
    "focusedGuardsPassed",
    "focusedGuardsTotal",
    "v25BuildWarnings",
    "v25BuildErrors",
    "helperBuildWarnings",
    "helperBuildErrors",
    "coreSmokePass",
    "sourceLinkExact",
    "zeroBricscadProcessesBefore",
}
FUNCTIONAL_KEYS = {
    "aBound",
    "bBound",
    "wrapperDriftNativeIdentity",
    "cBound",
    "dynamicHubs",
    "projectIsolation",
    "repeatCycle",
}
BOUND_KEYS = {"status", "closed", "expected"}
FINAL_HOST_KEYS = {
    "status",
    "hostMatched",
    "processExitCode",
    "gracefulExit",
    "applicationErrorCount",
    "werCount",
    "applicationHangCount",
    "dotNetRuntimeErrorCount",
    "accessViolationCount",
    "failure",
}
FAILURE_KEYS = {
    "class",
    "faultModule",
    "exceptionCode",
    "werEventName",
    "bricscadReportCode",
    "signatureFamily",
}
SAFETY_KEYS = {
    "publicFixtureUnchanged",
    "protectedUserDwgUnchanged",
    "demandLoadLoaderUnchanged",
    "demandLoadBytesUnchanged",
    "loadCtrls",
    "privateStateRestored",
    "zeroBricscadProcessesAfter",
    "zeroHelperProcessesAfter",
    "trackedTreeClean",
    "rawEvidenceIgnored",
    "sanitizedOnly",
}
NO_RESULT_KEYS = {"reasonCode"}


class ContractError(ValueError):
    pass


def fail(message: str) -> None:
    raise ContractError(message)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def require_object(value, field: str) -> dict:
    require(isinstance(value, dict), f"{field} must be a JSON object.")
    return value


def require_exact_keys(obj: dict, allowed: set[str], required: set[str], field: str) -> None:
    unknown = sorted(set(obj) - allowed)
    if unknown:
        fail(f"{field} contains forbidden field(s): {', '.join(unknown)}.")
    missing = sorted(required - set(obj))
    if missing:
        fail(f"{field} is missing required field(s): {', '.join(missing)}.")


def require_bool(value, field: str) -> bool:
    require(type(value) is bool, f"{field} must be true or false.")
    return value


def require_nonnegative_int(value, field: str) -> int:
    require(type(value) is int and value >= 0, f"{field} must be a non-negative integer.")
    return value


def require_status(value, field: str) -> str:
    require(isinstance(value, str) and value in STATUSES, f"{field} must be PASS, FAIL, or NOT_RUN.")
    return value


def require_safe_code(value, field: str, *, upper_token: bool = False) -> str:
    pattern = SAFE_UPPER_TOKEN if upper_token else SAFE_CODE
    require(isinstance(value, str) and pattern.fullmatch(value) is not None, f"{field} contains unsafe or non-sanitized text.")
    require("/" not in value and "\\" not in value, f"{field} must not contain a path.")
    return value


def validate_bound(obj, field: str, expected_count: int) -> str:
    obj = require_object(obj, field)
    require_exact_keys(obj, BOUND_KEYS, BOUND_KEYS, field)
    status = require_status(obj["status"], f"{field}.status")
    closed = require_nonnegative_int(obj["closed"], f"{field}.closed")
    expected = require_nonnegative_int(obj["expected"], f"{field}.expected")
    require(expected == expected_count, f"{field}.expected must be {expected_count} for LOCAL-002 H.1.")
    require(closed <= expected, f"{field}.closed cannot exceed {field}.expected.")
    if status == "PASS":
        require(closed == expected, f"{field} PASS requires {expected}/{expected} close/detach evidence.")
    return status


def validate_precheck(obj: dict, verdict: str) -> None:
    obj = require_object(obj, "precheck")
    require_exact_keys(obj, PRECHECK_KEYS, PRECHECK_KEYS, "precheck")
    passed = require_nonnegative_int(obj["focusedGuardsPassed"], "precheck.focusedGuardsPassed")
    total = require_nonnegative_int(obj["focusedGuardsTotal"], "precheck.focusedGuardsTotal")
    require(total > 0 and passed <= total, "precheck focused guard counts are invalid.")
    v25_warnings = require_nonnegative_int(obj["v25BuildWarnings"], "precheck.v25BuildWarnings")
    v25_errors = require_nonnegative_int(obj["v25BuildErrors"], "precheck.v25BuildErrors")
    helper_warnings = require_nonnegative_int(obj["helperBuildWarnings"], "precheck.helperBuildWarnings")
    helper_errors = require_nonnegative_int(obj["helperBuildErrors"], "precheck.helperBuildErrors")
    core_smoke = require_bool(obj["coreSmokePass"], "precheck.coreSmokePass")
    source_link = require_bool(obj["sourceLinkExact"], "precheck.sourceLinkExact")
    zero_before = require_bool(obj["zeroBricscadProcessesBefore"], "precheck.zeroBricscadProcessesBefore")

    if verdict in {"PASS", "FAIL"}:
        require(passed == total, f"{verdict} evidence requires all focused guards to pass.")
        require(v25_warnings == 0 and v25_errors == 0, f"{verdict} evidence requires a clean V25 build.")
        require(helper_warnings == 0 and helper_errors == 0, f"{verdict} evidence requires a clean helper build.")
        require(core_smoke, f"{verdict} evidence requires Core smoke PASS.")
        require(source_link, f"{verdict} evidence requires exact SourceLink identity.")
        require(zero_before, f"{verdict} evidence requires zero BricsCAD processes before launch.")


def validate_functional(obj: dict, verdict: str) -> tuple[str, str, list[str]]:
    obj = require_object(obj, "functional")
    require_exact_keys(obj, FUNCTIONAL_KEYS, FUNCTIONAL_KEYS, "functional")
    a_status = validate_bound(obj["aBound"], "functional.aBound", 13)
    b_status = validate_bound(obj["bBound"], "functional.bBound", 2)
    other_statuses = []
    for key in (
        "wrapperDriftNativeIdentity",
        "cBound",
        "dynamicHubs",
        "projectIsolation",
        "repeatCycle",
    ):
        other_statuses.append(require_status(obj[key], f"functional.{key}"))

    statuses = [a_status, b_status, *other_statuses]
    if verdict == "PASS":
        require(all(status == "PASS" for status in statuses), "PASS evidence requires the complete A/B/C functional matrix to PASS.")
    if verdict == "NO_RESULT":
        require("FAIL" not in statuses, "NO_RESULT cannot contain a functional FAIL; classify a reproducible product failure as FAIL.")
    return a_status, b_status, other_statuses


def validate_failure(obj) -> None:
    obj = require_object(obj, "finalHost.failure")
    require_exact_keys(obj, FAILURE_KEYS, FAILURE_KEYS, "finalHost.failure")
    require_safe_code(obj["class"], "finalHost.failure.class", upper_token=True)
    module = require_safe_code(obj["faultModule"], "finalHost.failure.faultModule")
    require(module.lower().endswith((".dll", ".exe")), "finalHost.failure.faultModule must be a sanitized module basename.")
    require_safe_code(obj["exceptionCode"], "finalHost.failure.exceptionCode")
    require_safe_code(obj["werEventName"], "finalHost.failure.werEventName")
    require_safe_code(obj["bricscadReportCode"], "finalHost.failure.bricscadReportCode")
    require_safe_code(obj["signatureFamily"], "finalHost.failure.signatureFamily", upper_token=True)


def validate_final_host(obj: dict, verdict: str) -> str:
    obj = require_object(obj, "finalHost")
    required = FINAL_HOST_KEYS - {"failure"}
    require_exact_keys(obj, FINAL_HOST_KEYS, required, "finalHost")
    status = require_status(obj["status"], "finalHost.status")
    host_matched = require_bool(obj["hostMatched"], "finalHost.hostMatched")
    require(
        isinstance(obj["processExitCode"], str) and PROCESS_EXIT.fullmatch(obj["processExitCode"]) is not None,
        "finalHost.processExitCode must be a sanitized decimal/hex exit code or NOT_RUN.",
    )
    graceful = require_bool(obj["gracefulExit"], "finalHost.gracefulExit")
    counts = {
        key: require_nonnegative_int(obj[key], f"finalHost.{key}")
        for key in (
            "applicationErrorCount",
            "werCount",
            "applicationHangCount",
            "dotNetRuntimeErrorCount",
            "accessViolationCount",
        )
    }

    has_failure = "failure" in obj
    if has_failure:
        validate_failure(obj["failure"])

    if verdict == "PASS":
        require(status == "PASS", "PASS verdict requires finalHost.status=PASS.")
        require(host_matched, "PASS verdict requires the exact host PID to be matched.")
        require(graceful, "PASS verdict requires graceful final host shutdown.")
        require(all(count == 0 for count in counts.values()), "PASS verdict requires zero exact-PID Application Error/Hang/.NET Runtime/WER/AccessViolation evidence.")
        require(not has_failure, "PASS verdict must not carry a failure classification.")
    elif verdict == "FAIL":
        require(status == "FAIL", "FAIL verdict requires finalHost.status=FAIL for the bounded H.1 closeout contract.")
        require(has_failure, "FAIL verdict requires a sanitized finalHost.failure classification.")
        require(
            (not host_matched) or (not graceful) or any(count > 0 for count in counts.values()),
            "FAIL verdict requires an observable final-host failure signal.",
        )
    else:
        require(status == "NOT_RUN", "NO_RESULT requires finalHost.status=NOT_RUN.")
        require(not has_failure, "NO_RESULT must not carry a product failure classification.")
        require(all(count == 0 for count in counts.values()), "NO_RESULT must not invent exact-PID failure counts.")
    return status


def validate_safety(obj: dict) -> None:
    obj = require_object(obj, "safety")
    require_exact_keys(obj, SAFETY_KEYS, SAFETY_KEYS, "safety")
    for key in SAFETY_KEYS - {"loadCtrls"}:
        require_bool(obj[key], f"safety.{key}")
    require_nonnegative_int(obj["loadCtrls"], "safety.loadCtrls")

    for key in SAFETY_KEYS - {"loadCtrls"}:
        require(obj[key] is True, f"Validated H.1 evidence requires safety.{key}=true.")
    require(obj["loadCtrls"] == 2, "Validated H.1 evidence requires DemandLoad LoadCtrls=2.")


def validate_no_result(obj, verdict: str) -> None:
    if verdict == "NO_RESULT":
        obj = require_object(obj, "noResult")
        require_exact_keys(obj, NO_RESULT_KEYS, NO_RESULT_KEYS, "noResult")
        reason = obj["reasonCode"]
        require(isinstance(reason, str) and reason in NO_RESULT_REASONS, "noResult.reasonCode is not an allowlisted bounded retry reason.")
    else:
        require(obj is None, f"{verdict} evidence must not contain noResult routing data.")


def validate_report(report: dict, expected_sha: str | None) -> dict:
    report = require_object(report, "report")
    required_top = TOP_KEYS - {"noResult"}
    require_exact_keys(report, TOP_KEYS, required_top, "report")

    require(report["schemaVersion"] == SCHEMA_VERSION, f"schemaVersion must be {SCHEMA_VERSION}.")
    require(report["lane"] == LANE, f"lane must be {LANE}.")
    require(isinstance(report["attempt"], str) and ATTEMPT.fullmatch(report["attempt"]) is not None, "attempt must be a bounded P## identifier.")
    verdict = report["verdict"]
    require(isinstance(verdict, str) and verdict in VERDICTS, "verdict must be PASS, FAIL, or NO_RESULT.")

    exact_sha = str(report["exactSha"]).strip().lower()
    require(SHA40.fullmatch(exact_sha) is not None, "exactSha must be a full 40-character Git SHA.")
    if expected_sha is not None:
        require(exact_sha == expected_sha, f"exactSha does not match expected SHA {expected_sha}.")

    product_version = str(report["bricscadProductVersion"]).strip()
    require(V25_VERSION.fullmatch(product_version) is not None, "bricscadProductVersion must identify BricsCAD V25 with a numeric ProductVersion.")

    plugin_hash = str(report["pluginSha256"]).strip().lower()
    core_hash = str(report["coreSha256"]).strip().lower()
    require(SHA256.fullmatch(plugin_hash) is not None, "pluginSha256 must be a full SHA-256 digest.")
    require(SHA256.fullmatch(core_hash) is not None, "coreSha256 must be a full SHA-256 digest.")

    validate_precheck(report["precheck"], verdict)
    validate_functional(report["functional"], verdict)
    validate_final_host(report["finalHost"], verdict)
    validate_safety(report["safety"])
    validate_no_result(report.get("noResult"), verdict)

    if verdict == "PASS":
        route = "LOCAL_PASS_ELIGIBLE"
        eligible = True
    elif verdict == "FAIL":
        route = "SOURCE_DIAGNOSIS_REQUIRED"
        eligible = False
    else:
        route = "BOUNDED_RETRY_REQUIRED"
        eligible = False

    return {
        "schemaVersion": SCHEMA_VERSION,
        "lane": LANE,
        "attempt": report["attempt"],
        "verdict": verdict,
        "exactSha": exact_sha,
        "bricscadProductVersion": product_version,
        "pluginSha256": plugin_hash,
        "coreSha256": core_hash,
        "route": route,
        "localPassEligible": eligible,
    }


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="Validate sanitized LOCAL-002 H.1 licensed-result evidence and emit its fail-closed route.")
    parser.add_argument("--input", required=True, help="Sanitized H.1 result JSON produced from the private/local harness evidence.")
    parser.add_argument("--expected-sha", help="Optional exact 40-character SHA that the result must match.")
    args = parser.parse_args(argv)

    expected_sha = None
    if args.expected_sha is not None:
        expected_sha = args.expected_sha.strip().lower()
        if SHA40.fullmatch(expected_sha) is None:
            print("ERROR: --expected-sha must be a full 40-character Git SHA.", file=sys.stderr)
            return 2

    source = Path(args.input)
    if not source.is_file():
        print("ERROR: sanitized H.1 result input does not exist.", file=sys.stderr)
        return 2

    try:
        report = json.loads(source.read_text(encoding="utf-8-sig"))
        output = validate_report(report, expected_sha)
    except (OSError, json.JSONDecodeError, ContractError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    print(json.dumps(output, sort_keys=True, separators=(",", ":")))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
