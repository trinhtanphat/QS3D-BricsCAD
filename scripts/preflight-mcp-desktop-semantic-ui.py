#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DESKTOP = V25 / "McpDesktopAutomationRuntime.cs"
SEMANTIC = V25 / "McpDesktopUiAutomationRuntime.cs"
PROJECT = V25 / "QS3D.BricsCAD.V25.csproj"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-desktop-semantic-ui.md"

TOOLS = (
    "desktop_window_set_state",
    "desktop_window_move_resize",
    "desktop_ui_tree",
)


def main() -> int:
    errors: list[str] = []
    for path in (DESKTOP, SEMANTIC, PROJECT, RUNBOOK):
        if not path.is_file():
            errors.append(f"missing {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    desktop = DESKTOP.read_text(encoding="utf-8")
    semantic = SEMANTIC.read_text(encoding="utf-8")
    project = PROJECT.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    for tool in TOOLS:
        if f'"{tool}"' not in desktop:
            errors.append(f"desktop dispatcher missing tool registration: {tool}")
        if f'"{tool}"' not in semantic:
            errors.append(f"semantic runtime missing tool implementation: {tool}")
        if tool not in runbook:
            errors.append(f"runbook missing tool contract: {tool}")

    required_desktop = {
        "semantic descriptors": "McpDesktopUiAutomationRuntime.ToolDescriptors()",
        "semantic dispatcher": "McpDesktopUiAutomationRuntime.Call(tool, args, ensureMutationRunning, audit)",
        "window state mutation class": '"desktop_window_set_state"',
        "move/resize mutation class": '"desktop_window_move_resize"',
        "UI tree sensitive class": '"desktop_ui_tree"',
        "shared local consent": "McpDesktopControlSession.RequireLocalConsent(tool)",
        "shared guarded action": "McpDesktopControlSession.BeginGuardedAction(tool)",
    }
    for label, token in required_desktop.items():
        if token not in desktop:
            errors.append(f"desktop dispatcher missing {label}: {token}")

    required_semantic = {
        "UI Automation root": "AutomationElement.FromHandle",
        "ControlView walker": "TreeWalker.ControlViewWalker",
        "bounded depth": "MaxDepth = 8",
        "bounded nodes": "MaxNodes = 200",
        "safe name allowlist": "SafeNameControl",
        "password redaction": "current.IsPassword",
        "current-session guard": "Process.GetCurrentProcess()",
        "top-level window guard": "GetAncestor(hwnd, 2)",
        "virtual desktop X": "SM_XVIRTUALSCREEN",
        "virtual desktop width": "SM_CXVIRTUALSCREEN",
        "bounded move API": "SetWindowPos",
        "maximize/restore API": "ShowWindow",
        "mutation epoch callback": "ensureMutationRunning();",
        "post-mutation state verification": "GetWindowRect",
        "hex handle contract": 'ToString("X", CultureInfo.InvariantCulture)',
        "hex handle schema": '^[0-9A-Fa-f]{1,16}$',
        "sensitive acknowledgement": '"confirmSensitiveRead"',
        "mutation confirmation": '"confirmMutation"',
    }
    for label, token in required_semantic.items():
        if token not in semantic:
            errors.append(f"semantic runtime missing {label}: {token}")

    if semantic.count("ensureMutationRunning();") < 4:
        errors.append("window mutations must re-check the shared mutation epoch before and after Win32 mutation")

    for forbidden in (
        "ValuePattern.",
        "TextPattern.",
        "GetCurrentPattern(ValuePattern",
        "GetCurrentPattern(TextPattern",
        "ControlType.Edit",
        "ControlType.Document",
        "ControlType.Text",
        "Process.Start(",
        "CreateProcess(",
        "cmd.exe",
        "powershell.exe",
    ):
        if forbidden in semantic:
            errors.append(f"semantic runtime exposes forbidden privacy/execution surface: {forbidden}")

    if '<Reference Include="UIAutomationClient" />' not in project:
        errors.append("V25 project missing UIAutomationClient reference")
    if '<Reference Include="UIAutomationTypes" />' not in project:
        errors.append("V25 project missing UIAutomationTypes reference")

    for phrase in (
        "exact visible current-session",
        "confirmMutation=true",
        "confirmSensitiveRead=true",
        "Edit/Document/password",
        "no ValuePattern or TextPattern",
        "static/hosted qualification",
    ):
        if phrase.lower() not in runbook.lower():
            errors.append(f"runbook missing safety/runtime boundary phrase: {phrase}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP semantic desktop UI/window-layout source contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
