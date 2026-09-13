#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HUB = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDiagnosticHub.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"
REGISTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDesktopAutomationRuntime.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Qs3dThemeCoordinator.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP direct diagnostics/theme preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def between(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        return ""
    end_index = text.find(end, start_index + len(start))
    return text[start_index:] if end_index < 0 else text[start_index:end_index]


hub = HUB.read_text(encoding="utf-8")
runtime = RUNTIME.read_text(encoding="utf-8")
registry = REGISTRY.read_text(encoding="utf-8")
coordinator = COORDINATOR.read_text(encoding="utf-8")
theme_set_block = between(runtime, "private static string SetTheme", "private static string ThemeMutationAckJson")
theme_ack_block = between(runtime, "private static string ThemeMutationAckJson", "private static int Integer")

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
    "bounded wait": (runtime, "private const int MaxWaitMilliseconds = 7000;"),
    "bounded event line": (runtime, "private const int MaxEventCharacters = 8192;"),
    "canonical audit path": (runtime, "McpCadAgentRuntime.AuditFilePath"),
    "rotated canonical audit": (runtime, 'yield return path + ".1";'),
    "sequence cursor": (runtime, '"afterSequence"'),
    "snapshot bridge": (runtime, 'McpDiagnosticHub.CaptureSnapshot("mcp-direct")'),
    "CAD-context snapshot": (runtime, "McpDiagnosticHub.InvokeInCadContext(() =>"),
    "CAD-context theme read": (runtime, "return McpDiagnosticHub.InvokeInCadContext(ThemeStateJsonInCadContext);"),
    "CAD-context dispatcher": (hub, "Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadRead, item);"),
    "cancel-before-start dispatcher": (hub, "CadReadCancelledBeforeStart"),
    "theme owner": (runtime, "Qs3dThemeCoordinator.SetModeTerminal(mode, \"mcp-theme-set\", ensureMutationRunning)"),
    "queued theme mutation": (coordinator, "ThemeMutationCadContextQueued"),
    "running theme mutation": (coordinator, "ThemeMutationCadContextRunning"),
    "cancel-before-start theme mutation": (coordinator, "ThemeMutationCadContextCancelledBeforeStart"),
    "terminal theme mutation": (coordinator, "ThemeMutationCadContextTerminal"),
    "terminal callback": (coordinator, "ApplyBricsCadThemeTerminalInContext"),
    "terminal readback": (coordinator, "AppliedColorTheme"),
    "host theme state": (runtime, 'Application.GetSystemVariable("COLORTHEME")'),
    "mutation callback": (runtime, "ensureMutationRunning();"),
    "direct descriptors": (registry, "descriptors.AddRange(McpDirectDiagnosticsThemeRuntime.ToolDescriptors());"),
    "theme mutation registry": (registry, '"theme_set",'),
    "desktop consent isolation": (registry, 'tool.StartsWith("desktop_", StringComparison.Ordinal)'),
    "restart cursor seed": (hub, "_sequence = Math.Max(_sequence, LoadLatestPersistedSequence());"),
    "current audit cursor scan": (hub, "ReadLatestSequence(path)"),
    "rotated audit cursor scan": (hub, 'ReadLatestSequence(path + ".1")'),
    "shared sequence parser": (hub, "SequenceRegex.Match(line)"),
    "theme mutation acknowledgement": (theme_set_block, "return ThemeMutationAckJson(mode, colorTheme);"),
    "ack configured mode": (theme_ack_block, "Qs3dThemeCoordinator.CurrentMode"),
    "ack effective mode": (theme_ack_block, "Qs3dThemeCoordinator.EffectiveDark"),
    "ack verified host theme": (theme_ack_block, '\\"bricscadColorTheme\\":'),
}
for label, (source, token) in requirements.items():
    if token not in source:
        fail(f"missing {label}: {token}")

if "ThemeStateJson()" in theme_set_block or "InvokeInCadContext" in theme_set_block:
    fail("theme_set must not perform a post-apply CAD-context readback; theme_get is the verification route")

if 'Application.GetSystemVariable("COLORTHEME")' in theme_ack_block:
    fail("theme mutation acknowledgement must not read COLORTHEME after SetMode")

terminal_callback = between(coordinator, "private static void ApplyBricsCadThemeTerminalInContext", "private static void ApplyTrackedWpfTheme")
if "item.Error = ex;" not in terminal_callback or "item.Done.Set();" not in terminal_callback:
    fail("terminal theme callback must publish error and completion to the owner")
if "warning" in terminal_callback.lower():
    fail("terminal theme callback must not reduce native failure to warning-only success")

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

print("MCP direct diagnostics/theme preflight passed; theme_set acknowledges applied coordinator state without a second CAD-context readback.")
