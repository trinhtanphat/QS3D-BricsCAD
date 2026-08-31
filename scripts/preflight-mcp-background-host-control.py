#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
V26_SRC = ROOT / "src" / "QS3D.BricsCAD.V26"
DESKTOP = SRC / "McpDesktopAutomationRuntime.cs"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
SESSION = SRC / "McpDesktopControlSession.cs"
PERSISTENCE_UI = SRC / "McpPersistentAgentCenterAugmenter.cs"
SETTINGS = SRC / "McpPersistentUserSettings.cs"
PLUGIN = SRC / "PluginEntry.cs"
V26_PLUGIN = V26_SRC / "PluginEntry.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP background-host preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


desktop = DESKTOP.read_text(encoding="utf-8")
background = BACKGROUND.read_text(encoding="utf-8")
session = SESSION.read_text(encoding="utf-8")
persistence_ui = PERSISTENCE_UI.read_text(encoding="utf-8")
settings = SETTINGS.read_text(encoding="utf-8")
plugin = PLUGIN.read_text(encoding="utf-8")
v26_plugin = V26_PLUGIN.read_text(encoding="utf-8")

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
    "foreground toggle": (persistence_ui, "Cho phép chuột / bàn phím / màn hình user"),
    "foreground off keeps background agent": (session, "DisableForegroundAccessFromLocalUser"),
    "secure credential target": (settings, "QS3D.BricsCAD.MCP.OpenAI.RuntimeApiKey"),
    "windows credential write": (settings, 'EntryPoint = "CredWriteW"'),
    "windows credential read": (settings, 'EntryPoint = "CredReadW"'),
    "V25 startup secret restore": (plugin, "McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment()"),
    "V26 startup secret restore": (v26_plugin, "McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment()"),
    "V25 persistence UI startup": (plugin, "McpPersistentAgentCenterAugmenter.Start()"),
    "V26 persistence UI startup": (v26_plugin, "McpPersistentAgentCenterAugmenter.Start()"),
    "typed key capture": (persistence_ui, "McpPersistentUserSettings.SaveOpenAiRuntimeApiKey(value)"),
}
for label, (source, token) in requirements.items():
    if token not in source:
        fail(f"missing {label}: {token}")

# Restore the user-scoped secret before transport autostart on every supported BricsCAD host major.
for host, host_plugin in (("V25", plugin), ("V26", v26_plugin)):
    if host_plugin.index("McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment()") > host_plugin.index("McpTransportCoordinator.TryAutoStartPreferred()"):
        fail(f"{host} saved Runtime API key must be restored before transport auto-start")

# Turning desktop permission OFF must not invoke the historical agent-wide StopAutomation path.
disable_block = session.split("public static void DisableForegroundAccessFromLocalUser", 1)[1].split("public static void RequireLocalConsent", 1)[0]
if 'StopSession(reason, false, false, "OFF")' not in disable_block:
    fail("foreground OFF must revoke desktop access without stopping background CAD/API automation")

# WPF click handlers must fail closed without rethrowing into the dispatcher.
toggle_block = persistence_ui.split("private static void ToggleDesktopForegroundAccess()", 1)[1].split("private static void TrySetInteractionPolicy", 1)[0]
if "throw;" in toggle_block:
    fail("foreground toggle must not rethrow failures into the WPF dispatcher")
if "McpAgentExperience.Error(" not in toggle_block:
    fail("foreground toggle failure must publish a bounded local error after fail-closed revocation")
if 'TrySetInteractionPolicy("background_only")' not in toggle_block:
    fail("foreground toggle failure must restore background_only before reporting failure")

# Do not regress to plaintext secret persistence in QS3D files.
settings_lower = settings.lower()
for forbidden in (
    "file.writealltext",
    "streamwriter",
    "runtime-api-key.txt",
    "secret.txt",
):
    if forbidden in settings_lower:
        fail(f"secret persistence must stay in Windows Credential Manager, found: {forbidden}")

# Wipe the exact native UTF-8 credential blob length, not character count.
if "i < bytes.Length" not in settings or "Marshal.WriteByte(blob, i, 0)" not in settings:
    fail("native credential buffer must be zeroed for every allocated UTF-8 byte")

mutation_block = desktop.split("private static readonly HashSet<string> MutationTools", 1)[1].split("};", 1)[0]
for tool in (
    "bricscad_interaction_policy_set",
    "bricscad_ui_invoke",
    "bricscad_ui_set_text",
):
    if f'"{tool}"' not in mutation_block:
        fail(f"{tool} must remain behind McpCadAgentRuntime mutation confirmation/epoch guard")

# The same-process runtime must not become a remote shell/process/file system escape hatch.
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

print("MCP background-host + persistence preflight passed.")
