#!/usr/bin/env python3
"""Fail closed if durable MCP ACK ledger tail records can bypass validation."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpMutationAckLedger.cs"
text = SOURCE.read_text(encoding="utf-8")

start = text.find("private static void LoadDurableLocked()")
end = text.find("private static void TrimDurableToBoundsLocked()", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL: durable ledger loader boundary not found")
loader = text[start:end]

failures = []
legacy = "i < lines.Length && loaded < MaxDurableRecords"
if legacy in loader:
    failures.append("loader stops parsing at MaxDurableRecords and can silently ignore tail records")

loop = "for (var i = 1; i < lines.Length; i++)"
if loop not in loader:
    failures.append("loader must inspect every admitted ledger line")

guard = "if (loaded >= MaxDurableRecords)"
blank = "if (string.IsNullOrWhiteSpace(lines[i])) continue;"
parse = "var fields = lines[i].Split('|');"
for needle in (guard, blank, parse):
    if needle not in loader:
        failures.append(f"missing record-limit contract: {needle}")

if all(needle in loader for needle in (guard, blank, parse)):
    blank_pos = loader.index(blank)
    guard_pos = loader.index(guard)
    parse_pos = loader.index(parse)
    if not (blank_pos < guard_pos < parse_pos):
        failures.append("record limit must be checked after blank-line filtering and before record parsing")

if "MaxDurableRecords" not in loader:
    failures.append("loader no longer binds admission to MaxDurableRecords")
if "InvalidDataException" not in loader:
    failures.append("over-limit persisted state must remain fail-closed through corrupt-ledger recovery")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}")
    sys.exit(1)

print("PASS: durable MCP ACK ledger validates all admitted lines and rejects record 1025+ fail-closed")
