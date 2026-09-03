#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpSelfHealingRepairRuntime.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private const int CircuitOpenOccurrence = 4;",
    "var transientFailure = !callerOrPolicyFailure && IsTransientFailure(code, message);",
    "var circuitOpen = (sourceRepairEligible || transientFailure)",
    "&& occurrenceCount >= CircuitOpenOccurrence;",
    'if (circuitOpen) recommendedAction = "human_review";',
    'else if (transientFailure) recommendedAction = "retry_transient";',
]

for needle in required:
    if needle not in text:
        raise SystemExit(f"FAIL transient circuit guard: missing {needle!r}")

forbidden = [
    "var circuitOpen = sourceRepairEligible && occurrenceCount >= CircuitOpenOccurrence;",
]
for needle in forbidden:
    if needle in text:
        raise SystemExit(f"FAIL transient circuit guard: stale unbounded transient topology remains: {needle!r}")

caller_index = text.index("var callerOrPolicyFailure")
transient_index = text.index("var transientFailure")
source_index = text.index("var sourceRepairEligible")
circuit_index = text.index("var circuitOpen")
human_index = text.index('if (circuitOpen) recommendedAction = "human_review";')
retry_index = text.index('else if (transientFailure) recommendedAction = "retry_transient";')

if not (caller_index < transient_index < source_index < circuit_index < human_index < retry_index):
    raise SystemExit("FAIL transient circuit guard: classification/circuit/action ordering regressed")

if "contractFailure || IsCallerOrPolicyFailure" not in text:
    raise SystemExit("FAIL transient circuit guard: caller/policy failures must remain non-retryable")

print("PASS MCP self-healing repeated transient retry circuit")
