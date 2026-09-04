#!/usr/bin/env python3
"""Fail closed unless source-repair circuit identity ignores volatile failure details."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpSelfHealingRepairRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP self-healing repair fingerprint preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

record_start = source.find("public static string RecordFailure(")
record_end = source.find("internal static string BuildFingerprint(", record_start)
if record_start < 0 or record_end < 0:
    fail("RecordFailure/BuildFingerprint boundary is missing")
record = source[record_start:record_end]

# Source-repair eligibility must be decided before selecting the fingerprint message.
for needle in [
    "IsCallerOrPolicyFailure(code, message)",
    "IsTransientFailure(code, message)",
    "IsSourceRepairFailure(code, message)",
]:
    if needle not in record:
        fail(f"failure classification contract drifted: {needle}")

raw_fingerprint = "BuildFingerprint(tool, code, lane, exceptionType, message)"
if raw_fingerprint in record:
    fail("source-repair fingerprint still consumes the full volatile failure message")

if "sourceRepairEligible" not in record or "BuildSourceRepairIdentity" not in record:
    fail("source-repair failures do not select a stable repair identity")
if record.find("BuildSourceRepairIdentity") > record.find("BuildFingerprint"):
    fail("stable source-repair identity is not computed before the fingerprint")

helper_start = source.find("private static string BuildSourceRepairIdentity(")
helper_end = source.find("private static bool IsCallerOrPolicyFailure", helper_start)
if helper_start < 0 or helper_end < 0:
    fail("stable source-repair identity helper is missing")
helper = source[helper_start:helper_end]

# Prefer stable exception source-site evidence, while retaining a canonical message template
# to distinguish separate failures from the same method. Volatile values must not fragment
# a circuit across retries.
for needle in [
    "TargetSite",
    "DeclaringType",
    "CanonicalizeSourceRepairMessage",
    "<GUID>",
    "<HEX>",
    "<NUMBER>",
    "<PATH>",
]:
    if needle not in helper:
        fail(f"stable source-repair identity is missing required normalization: {needle}")

# Do not weaken the existing threshold or source-repair participation in the circuit.
# Repeated transient failures now share the same bounded fail-closed threshold, while
# caller/policy failures remain excluded by transientFailure classification.
if "private const int CircuitOpenOccurrence = 4;" not in source:
    fail("repair circuit threshold drifted")
if "var transientFailure = !callerOrPolicyFailure && IsTransientFailure(code, message);" not in record:
    fail("transient circuit classification no longer excludes caller/policy failures")
if "var circuitOpen = (sourceRepairEligible || transientFailure)" not in record:
    fail("source-repair failures no longer participate in the bounded repair circuit")
if "&& occurrenceCount >= CircuitOpenOccurrence;" not in record:
    fail("repair circuit no longer uses the bounded occurrence threshold")
if "var circuitOpen = sourceRepairEligible && occurrenceCount >= CircuitOpenOccurrence;" in record:
    fail("repair circuit regressed to source-repair-only topology")

print("MCP self-healing repair fingerprint preflight passed.")
sys.exit(0)
