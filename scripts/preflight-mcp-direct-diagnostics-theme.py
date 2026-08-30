#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HUB = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDiagnosticHub.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"
REGISTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDesktopAutomationRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP direct diagnostics/theme preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


hub = HUB.read_text(encoding="utf-8")
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
    "bounded event count": (runtime, "private const int MaxEvents = 100;"),
    "bounded wait": (runtime, "private const int MaxWaitMilliseconds = 15000;"),
    "bounded event line": (runtime, "private const int MaxEventCharacters = 8192;"),
    "canonical audit path": (runtime, "McpCadAgentRuntime.AuditFilePath"),
    "rotated canonical audit": (runtime, 'yield return path + ".1";'),
    "sequence cursor": (runtime, '"afterSequence"'),
    "snapshot bridge": (runtime, 'McpDiagnosticHub.CaptureSnapshot("mcp-direct")'),
    "theme owner": (runtime, "Qs3dThemeCoordinator.SetMode(mode, \"mcp-theme-set\")"),
    "host theme state": (runtime, 'Application.GetSystemVariable("COLORTHEME")'),
    "mutation callback": (runtime, "ensureMutationRunning();"),
    "direct descriptors": (registry, "descriptors.AddRange(McpDirectDiagnosticsThemeRuntime.ToolDescriptors());"),
    "theme mutation registry": (registry, '"theme_set",'),
    "desktop consent isolation": (registry, 'tool.StartsWith("desktop_", StringComparison.Ordinal)'),
    "restart cursor seed": (hub, "_sequence = Math.Max(_sequence, LoadLatestPersistedSequence());"),
    "current audit cursor scan": (hub, "ReadLatestSequence(path)"),
    "rotated audit cursor scan": (hub, 'ReadLatestSequence(path + ".1")'),
    "shared sequence parser": (hub, "SequenceRegex.Match(line)"),
}
for label, (source, token) in requirements.items():
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
