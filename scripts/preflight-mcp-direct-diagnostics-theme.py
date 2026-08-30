#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"
REGISTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDesktopAutomationRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP direct diagnostics/theme preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


runtime = RUNTIME.read_text(encoding="utf-8")
registry = REGISTRY.read_text(encoding="utf-8")

for tool in (
    "diagnostics_log_tail",
    "diagnostics_since",
    "diagnostics_snapshot",
    "diagnostics_wait",
    "theme_get",
    "theme_set",
):
    if f'"{tool}"' not in runtime:
        fail(f"runtime is missing direct MCP tool {tool}")
    if f'"{tool}"' not in registry:
        fail(f"registry is missing direct MCP tool {tool}")

requirements = {
    "bounded event count": "private const int MaxEvents = 100;",
    "bounded wait": "private const int MaxWaitMilliseconds = 15000;",
    "bounded event line": "private const int MaxEventCharacters = 8192;",
    "canonical audit path": "McpCadAgentRuntime.AuditFilePath",
    "rotated canonical audit": 'yield return path + ".1";',
    "sequence cursor": '"afterSequence"',
    "snapshot bridge": 'McpDiagnosticHub.CaptureSnapshot("mcp-direct")',
    "theme owner": "Qs3dThemeCoordinator.SetMode(mode, \"mcp-theme-set\")",
    "host theme state": 'Application.GetSystemVariable("COLORTHEME")',
    "mutation callback": "ensureMutationRunning();",
    "direct descriptors": "descriptors.AddRange(McpDirectDiagnosticsThemeRuntime.ToolDescriptors());",
    "theme mutation registry": '"theme_set",',
    "desktop consent isolation": 'tool.StartsWith("desktop_", StringComparison.Ordinal)',
}
for label, token in requirements.items():
    source = registry if label in {"direct descriptors", "theme mutation registry", "desktop consent isolation"} else runtime
    if token not in source:
        fail(f"missing {label}: {token}")

if '"theme_set"' not in registry.split("private static readonly HashSet<string> MutationTools", 1)[1].split("};", 1)[0]:
    fail("theme_set must stay inside MutationTools so McpCadAgentRuntime enforces confirmMutation/emergency-stop")

for forbidden in (
    "Process.Start(",
    "cmd.exe",
    "powershell",
    "pwsh",
    "File.ReadAllText(McpTopLevelJson",
    "Directory.GetFiles(McpTopLevelJson",
):
    if forbidden.lower() in runtime.lower():
        fail(f"forbidden arbitrary execution/path surface detected: {forbidden}")

print("MCP direct diagnostics/theme preflight passed.")
