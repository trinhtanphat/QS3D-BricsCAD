#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpQs3dDomainRuntime.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL: " + message, file=sys.stderr)
    raise SystemExit(1)

start = text.find("internal static string Call(string tool, string arguments)")
end = text.find("private static string BindProject", start)
if start < 0 or end < 0:
    fail("QS3D domain mutation dispatch boundary not found")
call = text[start:end]

if "McpDiagnosticHub.InvokeInCadContext" in call:
    fail("QS3D domain mutations still use response-bounded diagnostic CAD-context dispatch")

required = (
    "InvokeDomainMutationInCadContext",
    "DomainMutationCadContextWorkItem",
    "DomainMutationCadContextQueued",
    "DomainMutationCadContextRunning",
    "DomainMutationCadContextCancelled",
    "Application.DocumentManager.ExecuteInApplicationContext",
)
for token in required:
    if token not in text:
        fail("missing mutation-owned application-context contract token: " + token)

for tool in (
    "qs3d_project_bind",
    "qs3d_project_reload",
    "qs3d_run_command",
    "qs3d_place_single_footing",
):
    if tool not in call:
        fail("mutation dispatcher no longer owns tool: " + tool)

if "McpCadAgentRuntime.EnsureCurrentMutationRunning();" not in call:
    fail("mutation running ownership check was removed")

if "UnmanagedObject" not in text:
    fail("native database generation affinity is not represented")

print("PASS: QS3D domain mutations require mutation-owned application-context completion")
