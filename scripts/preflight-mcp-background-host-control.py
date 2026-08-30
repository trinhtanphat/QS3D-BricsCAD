#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DESKTOP = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDesktopAutomationRuntime.cs"
BACKGROUND = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpBackgroundHostRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP background-host preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


desktop = DESKTOP.read_text(encoding="utf-8")
background = BACKGROUND.read_text(encoding="utf-8")

for tool in (
    "bricscad_interaction_policy_get",
    "bricscad_interaction_policy_set",
    "bricscad_ui_text_snapshot",
    "bricscad_ui_invoke",
    "bricscad_ui_set_text",
):
    if f'"{tool}"' not in background:
        fail(f"background runtime is missing {tool}")
    if f'"{tool}"' not in desktop:
        fail(f"desktop registry/routing is missing {tool}")

requirements = {
    "background default": (background, "private static int _interactionPolicy = BackgroundOnly;"),
    "local fallback consent": (background, 'McpDesktopControlSession.RequireLocalConsent("foreground-fallback-enable")'),
    "global input gate": (desktop, "McpBackgroundHostRuntime.EnsureGlobalInteractionAllowed(tool);"),
    "background descriptors": (desktop, "McpBackgroundHostRuntime.ToolDescriptors()"),
    "background dispatch": (desktop, "McpBackgroundHostRuntime.Call(tool, args, ensureMutationRunning, audit)"),
    "same-process validation": (background, "BelongsToCurrentProcess"),
    "current process id": (background, "Process.GetCurrentProcess()"),
    "bounded child enumeration": (background, "EnumChildWindows"),
    "sensitive text confirmation": (background, 'McpTopLevelJson.ExtractBoolean(body, "confirmSensitiveRead")'),
    "bounded window message": (background, "SendMessageTimeout"),
    "button only": (background, 'string.Equals(className, "Button", StringComparison.OrdinalIgnoreCase)'),
    "edit only": (background, 'value == "EDIT" || value.StartsWith("RICHEDIT", StringComparison.Ordinal)'),
    "window PrintWindow path": (desktop, "CaptureWindowBitmap(hwnd"),
    "PrintWindow API": (desktop, "PrintWindow(IntPtr hwnd"),
    "screen BitBlt retained": (desktop, "BitBlt(memory, 0, 0, width, height, screen, x, y, SRCCOPY)"),
}
for label, (source, token) in requirements.items():
    if token not in source:
        fail(f"missing {label}: {token}")

mutation_block = desktop.split("private static readonly HashSet<string> MutationTools", 1)[1].split("};", 1)[0]
for tool in (
    "bricscad_interaction_policy_set",
    "bricscad_ui_invoke",
    "bricscad_ui_set_text",
):
    if f'"{tool}"' not in mutation_block:
        fail(f"{tool} must remain behind McpCadAgentRuntime mutation confirmation/epoch guard")

# The new same-process runtime must not become a remote shell/process/file system escape hatch.
for forbidden in (
    "Process.Start(",
    "cmd.exe",
    "powershell",
    "pwsh",
    "System.Management.Automation",
    "CreateProcess(",
    "ShellExecute(",
    "File.ReadAllText(",
    "Directory.GetFiles(",
):
    if forbidden.lower() in background.lower():
        fail(f"forbidden arbitrary execution/filesystem surface detected: {forbidden}")

# Window mode must not silently fall back to reading desktop pixels, otherwise occlusion/user activity
# can corrupt what ChatGPT believes is the BricsCAD target-window image.
screenshot = desktop.split("private static string Screenshot", 1)[1].split("private static RECT ApplyScreenshotCrop", 1)[0]
if 'if (scope == "window")' not in screenshot or "CaptureWindowBitmap" not in screenshot:
    fail("desktop_screenshot window branch must use CaptureWindowBitmap")
window_branch = screenshot.split('if (scope == "window")', 1)[1].split("\n            else\n", 1)[0]
if "CaptureBitmap(" in window_branch or "BitBlt(" in window_branch:
    fail("window screenshot must not sample desktop pixels")
if "CaptureBitmap(rect.Left, rect.Top, width, height)" not in screenshot:
    fail("screen screenshot must retain the bounded desktop BitBlt path")

print("MCP background-host preflight passed.")
