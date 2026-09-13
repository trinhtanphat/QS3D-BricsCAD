#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL: " + message, file=sys.stderr)
    raise SystemExit(1)

start = text.find("internal static string Call(string tool, string arguments)")
end = text.find("internal static string CallCadCommandSequence", start)
if start < 0 or end < 0:
    fail("DirectModel Call boundary not found")
call = text[start:end]

for pattern in (
    "if (mutation)\n                    return InvokeDirectMutationInCadContext(tool",
    "return InvokeDirectMutationInCadContext(tool, () =>",
):
    if pattern not in call:
        fail("DirectModel mutation branch is not routed through mutation-owned CAD-context completion: " + pattern)

seq_start = text.find("internal static string CallCadCommandSequence")
seq_end = text.find("private static string SaveCadCommandSequence", seq_start)
if seq_start < 0 or seq_end < 0:
    fail("DirectModel command-sequence boundary not found")
sequence = text[seq_start:seq_end]
if "McpDiagnosticHub.InvokeInCadContext" in sequence:
    fail("direct layout/command mutation still uses diagnostic CAD-context dispatch")
if 'InvokeDirectMutationInCadContext("cad_command_sequence"' not in sequence:
    fail("direct command sequence lacks mutation-owned CAD-context completion")

for token in (
    "DirectMutationCadContextWorkItem", "DirectMutationCadContextQueued",
    "DirectMutationCadContextRunning", "DirectMutationCadContextCancelled",
    "Application.DocumentManager.ExecuteInApplicationContext", "UnmanagedObject",
    "McpCadAgentRuntime.EnsureCurrentMutationRunning();",
):
    if token not in text:
        fail("missing DirectModel mutation-owned application-context contract token: " + token)

print("PASS: DirectModel mutations require mutation-owned application-context completion")