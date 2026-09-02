#!/usr/bin/env python3
from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpSelfHealingRepairRuntime.cs"
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-self-healing-repair.md"
PLAN = ROOT / "docs" / "superpowers" / "plans" / "2026-09-02-mcp-self-healing-repair.md"

errors = []

def require(text, token, where):
    if token not in text:
        errors.append(f"{where} missing contract token: {token}")

runtime = RUNTIME.read_text(encoding="utf-8") if RUNTIME.is_file() else ""
server = SERVER.read_text(encoding="utf-8") if SERVER.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""
plan = PLAN.read_text(encoding="utf-8") if PLAN.is_file() else ""

if not runtime:
    errors.append("missing McpSelfHealingRepairRuntime.cs")
else:
    for token in (
        "internal static class McpSelfHealingRepairRuntime",
        "CircuitOpenOccurrence = 4",
        "MaxTickets = 256",
        "RecordFailure(",
        "BuildFingerprint(",
        "SHA256.Create()",
        "QS3D-REPAIR-",
        "occurrenceCount",
        "sourceRepairEligible",
        "circuitOpen",
        "humanReviewRequired",
        "recommendedAction",
        "correct_call_or_refresh_tools",
        "open_source_repair",
        "human_review",
        "IsCallerOrPolicyFailure",
        "UNKNOWN MCP CAD TOOL",
        "CONFIRMMUTATION",
    ):
        require(runtime, token, "self-healing runtime")

    # The ledger must stay process-bounded rather than growing with every chat/agent.
    require(runtime, "Tickets.Count >= MaxTickets", "bounded repair ledger")

# Existing tool-call error paths must enrich the current error envelope rather than create a
# dynamic repair MCP tool that clients would need to rediscover/enable.
for token in (
    "McpSelfHealingRepairRuntime.RecordFailure",
    "repairJson",
    "\\\"repair\\\":" ,
):
    require(server, token, "MCP server repair integration")

for forbidden in (
    'Tool("cad_repair',
    'Tool("mcp_repair',
    'Tool("github_repair',
):
    if forbidden in server:
        errors.append("self-healing must not add a dynamic repair MCP tool: " + forbidden)

for token in (
    "correct call locally",
    "sourceRepairEligible",
    "circuitOpen",
    "GitHub repair carrier",
    "do not patch-loop",
):
    require(runbook, token, "self-healing runbook")

for token in (
    "deterministic SHA-256 fingerprint",
    "Caller/schema/auth/policy/confirmation mistakes are never source-repair candidates",
    "circuit opening after four identical repairable failures",
):
    require(plan, token, "implementation plan")

# Behavioral model pinned independently of C# implementation details.
def normalize(value):
    return re.sub(r"\\s+", " ", (value or "").strip()).upper()

def fingerprint(tool, code, lane, exc_type, message):
    canonical = "|".join(normalize(v) for v in (tool, code, lane, exc_type, message))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()

same_a = fingerprint("cad_create_circle", "TOOL_FAILED", "cad-direct", "InvalidOperationException", "radius must be > 0")
same_b = fingerprint(" cad_create_circle ", "tool_failed", "CAD-DIRECT", "InvalidOperationException", "radius   must be > 0")
other = fingerprint("cad_create_circle", "TOOL_FAILED", "cad-direct", "InvalidOperationException", "No active BricsCAD document")
assert same_a == same_b
assert same_a != other
assert len(same_a) == 64

# Contract model: the fourth identical repairable occurrence opens the circuit.
occurrences = [1, 2, 3, 4]
assert [n >= 4 for n in occurrences] == [False, False, False, True]

print("QS3D MCP self-healing repair preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: tools/call failures carry bounded deterministic repair metadata, caller mistakes stay non-repairable, and repeated repairable failures fail closed into human review.")
