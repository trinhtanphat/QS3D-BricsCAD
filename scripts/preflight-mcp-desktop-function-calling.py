#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
CAD_RUNTIME = V25 / "McpCadAgentRuntime.cs"
DESKTOP_RUNTIME = V25 / "McpDesktopAutomationRuntime.cs"

TOOLS = (
    "desktop_cursor_position",
    "desktop_window_list",
    "desktop_foreground_window",
    "desktop_window_focus",
    "desktop_mouse_move",
    "desktop_mouse_click",
    "desktop_mouse_scroll",
    "desktop_type",
    "desktop_key",
    "desktop_clipboard_read",
    "desktop_clipboard_write",
    "desktop_screenshot",
)

MUTATIONS = (
    "desktop_window_focus",
    "desktop_mouse_move",
    "desktop_mouse_click",
    "desktop_mouse_scroll",
    "desktop_type",
    "desktop_key",
    "desktop_clipboard_write",
)


def main() -> int:
    errors: list[str] = []
    for path in (SERVER, CAD_RUNTIME, DESKTOP_RUNTIME):
        if not path.is_file():
            errors.append(f"missing {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    server = SERVER.read_text(encoding="utf-8")
    cad = CAD_RUNTIME.read_text(encoding="utf-8")
    desktop = DESKTOP_RUNTIME.read_text(encoding="utf-8")

    if "McpDesktopAutomationRuntime.ToolDescriptors()" not in server:
        errors.append("MCP tools/list does not append the desktop tool catalog")
    if "McpDesktopAutomationRuntime.IsTool(tool)" not in cad:
        errors.append("CAD runtime does not route desktop tools")
    if "McpDesktopAutomationRuntime.RequiresMutation(tool)" not in cad:
        errors.append("desktop mutation classification is not enforced by CAD runtime")
    if "return Mutation(args, tool" not in cad:
        errors.append("desktop mutations do not reuse the existing confirmation/emergency-stop epoch gate")
    if "EnsureCurrentMutationRunning" not in cad or "Audit(tool, detail)" not in cad:
        errors.append("desktop dispatcher lacks per-input stop verification or shared audit callback")

    for tool in TOOLS:
        if f'"{tool}"' not in desktop:
            errors.append(f"desktop runtime missing tool: {tool}")
    for tool in MUTATIONS:
        if tool not in desktop:
            errors.append(f"desktop mutation missing classification: {tool}")

    required = {
        "mutation confirmation descriptor": '"confirmMutation"',
        "sensitive-read acknowledgement": '"confirmSensitiveRead"',
        "current-session target guard": "Process.GetProcessById",
        "visible-window enumeration": "EnumWindows",
        "foreground revalidation": "GetForegroundWindow",
        "absolute cursor placement": "SetCursorPos",
        "modern input API": "SendInput",
        "Unicode keyboard input": "KEYEVENTF_UNICODE",
        "clipboard STA": "ApartmentState.STA",
        "clipboard text-only surface": "Clipboard.GetText",
        "in-memory screenshot": "PngBitmapEncoder",
        "GDI screen capture": "BitBlt",
        "bounded screenshot output": "MaxScreenshotBytes",
        "bounded screenshot dimensions": "MaxScreenshotWidth",
        "window-title privacy bound": "MaxWindowTitleLength",
        "typed-text privacy audit": '"; chars="',
    }
    for label, token in required.items():
        if token not in desktop:
            errors.append(f"desktop runtime missing {label}: {token}")

    for forbidden in ("Process.Start(", "cmd.exe", "powershell.exe", "CreateProcess(", "mouse_event("):
        if forbidden in desktop:
            errors.append(f"desktop runtime exposes forbidden execution/input surface: {forbidden}")

    if desktop.count('"confirmSensitiveRead"') < 2:
        errors.append("clipboard/screenshot sensitive reads are not both explicitly gated")
    if "McpCadAgentRuntime.AutomationStopped" in desktop:
        errors.append("desktop runtime bypasses the canonical mutation epoch gate with a racy stop-only check")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP desktop function-calling source contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
