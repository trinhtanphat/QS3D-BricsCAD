#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
PERMISSIONS = SRC / "McpLocalControlPermissions.cs"
DESKTOP = SRC / "McpDesktopAutomationRuntime.cs"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
AUGMENTER = SRC / "McpPersistentAgentCenterAugmenter.cs"
SETTINGS = SRC / "McpPersistentUserSettings.cs"
INSTALLER = SRC / "Updates" / "VerifiedPreviewInstaller.cs"
RUNBOOK = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"


def fail(message: str) -> None:
    print(f"ERROR: MCP granular-permissions preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (PERMISSIONS, DESKTOP, BACKGROUND, AUGMENTER, SETTINGS, INSTALLER, RUNBOOK):
    if not path.is_file():
        fail("missing required file: " + str(path.relative_to(ROOT)))

permissions = PERMISSIONS.read_text(encoding="utf-8")
desktop = DESKTOP.read_text(encoding="utf-8")
background = BACKGROUND.read_text(encoding="utf-8")
augmenter = AUGMENTER.read_text(encoding="utf-8")
settings = SETTINGS.read_text(encoding="utf-8")
installer = INSTALLER.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "internal static class McpLocalControlPermissions",
    "private static int _backgroundHostControl = 1;",
    "private static int _screenRead;",
    "private static int _mouseInput;",
    "private static int _keyboardInput;",
    "private static int _clipboardAccess;",
    "public static bool BackgroundHostControl",
    "public static bool ScreenRead",
    "public static bool MouseInput",
    "public static bool KeyboardInput",
    "public static bool ClipboardAccess",
    "public static bool HasAnyForegroundPermission",
    "SetBackgroundHostControlFromLocalUser",
    "SetScreenReadFromLocalUser",
    "SetMouseInputFromLocalUser",
    "SetKeyboardInputFromLocalUser",
    "SetClipboardAccessFromLocalUser",
    "RequireForTool(string toolName)",
    'case "bricscad_ui_text_snapshot":',
    'case "bricscad_ui_invoke":',
    'case "bricscad_ui_set_text":',
    'case "desktop_screenshot":',
    'case "desktop_window_focus":',
    'case "desktop_mouse_move":',
    'case "desktop_mouse_click":',
    'case "desktop_mouse_scroll":',
    'case "desktop_mouse_drag":',
    'case "desktop_type":',
    'case "desktop_key":',
    'case "desktop_clipboard_read":',
    'case "desktop_clipboard_write":',
):
    if token not in permissions:
        fail("permission authority missing contract: " + token)

if "Environment." in permissions or "CredWrite" in permissions or "File." in permissions:
    fail("granular foreground permissions must remain process-memory-only")

if "McpLocalControlPermissions.RequireForTool(tool);" not in desktop:
    fail("desktop dispatch must enforce granular permission before execution")
if "McpLocalControlPermissions.RequireForTool(step.Tool);" not in desktop:
    fail("desktop_sequence must enforce granular permission for each contained step")
if '"permissions":' not in background or "McpLocalControlPermissions.StatusJson()" not in background:
    fail("interaction policy status must expose current granular permissions")

for token in (
    "PermissionPanelTag",
    "BackgroundPermissionTag",
    "ScreenPermissionTag",
    "MousePermissionTag",
    "KeyboardPermissionTag",
    "ClipboardPermissionTag",
    "new CheckBox",
    "Chạy nền trong BricsCAD (không chiếm chuột/phím)",
    "Cho phép đọc/chụp màn hình",
    "Cho phép điều khiển chuột",
    "Cho phép nhập bàn phím",
    "Cho phép đọc/ghi clipboard",
    "McpLocalControlPermissions.SetBackgroundHostControlFromLocalUser",
    "McpLocalControlPermissions.SetScreenReadFromLocalUser",
    "McpLocalControlPermissions.SetMouseInputFromLocalUser",
    "McpLocalControlPermissions.SetKeyboardInputFromLocalUser",
    "McpLocalControlPermissions.SetClipboardAccessFromLocalUser",
):
    if token not in augmenter:
        fail("Agent Center checkbox UI missing contract: " + token)

if "DesktopForegroundToggleTag" in augmenter:
    fail("legacy coarse foreground toggle must be removed after granular checkbox UI lands")
if "Cho phép chuột / bàn phím / màn hình user" in augmenter:
    fail("legacy coarse foreground label must be removed after granular checkbox UI lands")

# Key persistence must remain durable and verified before process publication.
for token in (
    "WriteCredential(OpenAiRuntimeKeyTarget, secret);",
    "TryReadOpenAiRuntimeApiKey(out persisted)",
    "string.Equals(persisted, secret, StringComparison.Ordinal)",
    'Environment.SetEnvironmentVariable("CONTROL_PLANE_API_KEY", secret, EnvironmentVariableTarget.Process);',
):
    if token not in settings:
        fail("Runtime API-key persistence regression: " + token)

# Preview updater must remain unable to overwrite/delete credential surfaces.
for forbidden in (
    "mcp-bearer-token.txt",
    "QS3D.BricsCAD.MCP.OpenAI.RuntimeApiKey",
    "CredDelete",
    "CONTROL_PLANE_API_KEY",
):
    if forbidden in installer:
        fail("preview updater must not touch MCP credential surface: " + forbidden)

for phrase in (
    "Windows Credential Manager",
    "read-back verification",
    "không ghi plaintext",
    "BackgroundHostControl",
    "ScreenRead",
    "MouseInput",
    "KeyboardInput",
    "ClipboardAccess",
):
    if phrase not in runbook:
        fail("canonical MCP runbook missing current permission/credential truth: " + phrase)

for stale in (
    "QS3D does **not** persist the Runtime API key",
    "On process restart, a user-entered Runtime API key is gone",
    "Do not persist the OpenAI Runtime API key",
):
    if stale in runbook:
        fail("canonical MCP runbook still contains stale Runtime API-key claim: " + stale)

print("MCP granular local permissions preflight passed.")
