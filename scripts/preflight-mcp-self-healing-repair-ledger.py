#!/usr/bin/env python3
"""Fail closed unless repair-ledger pressure preserves active source circuits."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpSelfHealingRepairRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP self-healing repair ledger preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

for needle in [
    "private const int MaxTickets = 256;",
    "SourceRepairEligible = sourceRepairEligible",
    "private static string? SelectEvictionCandidateLocked(bool incomingSourceRepair)",
    "pair.Value.SourceRepairEligible",
    "if (!incomingSourceRepair) return null;",
    "ephemeralTicket",
]:
    if needle not in source:
        fail(f"missing source-priority ledger contract: {needle}")

record_start = source.find("public static string RecordFailure(")
record_end = source.find("internal static string BuildFingerprint(", record_start)
record = source[record_start:record_end]
if record_start < 0 or record_end < 0:
    fail("RecordFailure boundary missing")

historical = "if (Tickets.Count >= MaxTickets)\n                    {\n                        string? oldestKey = null;"
if historical in record:
    fail("global oldest-ticket eviction can still discard an active source circuit")

for needle in [
    "SelectEvictionCandidateLocked(sourceRepairEligible)",
    "if (evictionCandidate == null)",
    "ephemeralTicket = true",
    "Tickets.Remove(evictionCandidate)",
]:
    if needle not in record:
        fail(f"bounded admission ordering drifted: {needle}")

helper_start = source.find("private static string? SelectEvictionCandidateLocked(bool incomingSourceRepair)")
helper_end = source.find("internal static string BuildFingerprint(", helper_start)
if helper_start < 0 or helper_end < 0:
    fail("source-priority eviction helper missing")
helper = source[helper_start:helper_end]

# First pass must select oldest non-source state. Only an incoming source-repair record may
# fall back to evicting oldest source state. Non-source pressure must become ephemeral.
for needle in [
    "if (pair.Value.SourceRepairEligible) continue;",
    "if (oldestNonSourceKey != null) return oldestNonSourceKey;",
    "if (!incomingSourceRepair) return null;",
    "return oldestSourceKey;",
]:
    if needle not in helper:
        fail(f"eviction priority is incomplete: {needle}")

# Ephemeral non-source observations must not pretend to be persisted/accumulated state.
if "occurrenceCount = 1;" not in record or "firstSeenUtc = now;" not in record or "lastSeenUtc = now;" not in record:
    fail("ephemeral non-source metadata is not deterministic occurrence-1 state")

if "private const int CircuitOpenOccurrence = 4;" not in source:
    fail("source circuit threshold drifted")

print("MCP self-healing repair ledger preflight passed.")
sys.exit(0)
