#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DESKTOP = V25 / "McpDesktopAutomationRuntime.cs"
SEMANTIC = V25 / "McpDesktopUiAutomationRuntime.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-background-semantic-ui.md"
CANONICAL = ROOT / "docs" / "MCP-CANONICAL-RUNBOOK.md"

TREE_TOOL = "bricscad_ui_semantic_tree"
ACTION_TOOL = "bricscad_ui_semantic_action"


def main() -> int:
    errors: list[str] = []
    for path in (DESKTOP, SEMANTIC, RUNBOOK, CANONICAL):
        if not path.is_file():
            errors.append(f"missing {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    desktop = DESKTOP.read_text(encoding="utf-8")
    semantic = SEMANTIC.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")
    canonical = CANONICAL.read_text(encoding="utf-8")

    for tool in (TREE_TOOL, ACTION_TOOL):
        if f'"{tool}"' not in desktop:
            errors.append(f"desktop registry/routing missing {tool}")
        if f'"{tool}"' not in semantic:
            errors.append(f"semantic runtime missing {tool}")
        if tool not in runbook:
            errors.append(f"runbook missing {tool}")

    desktop_requirements = {
        "background semantic descriptors": "McpDesktopUiAutomationRuntime.BackgroundToolDescriptors()",
        "background semantic dispatch": "McpDesktopUiAutomationRuntime.CallBackground(tool, args, ensureMutationRunning, audit)",
        "semantic action mutation classification": f'"{ACTION_TOOL}"',
        "background default policy gate": "McpBackgroundHostRuntime.EnsureGlobalInteractionAllowed(tool);",
    }
    for label, token in desktop_requirements.items():
        if token not in desktop:
            errors.append(f"desktop dispatcher missing {label}: {token}")

    mutation_block = desktop.split("private static readonly HashSet<string> MutationTools", 1)[1].split("};", 1)[0]
    if f'"{ACTION_TOOL}"' not in mutation_block:
        errors.append(f"{ACTION_TOOL} must be behind the shared MCP mutation confirmation/epoch gate")
    if f'"{TREE_TOOL}"' in mutation_block:
        errors.append(f"{TREE_TOOL} must remain read-only")

    semantic_requirements = {
        "background tool set": "BackgroundTools",
        "same-process target guard": "RequireCurrentBricsCadWindow",
        "current BricsCAD process identity": "Process.GetCurrentProcess()",
        "UIA process identity": "current.ProcessId",
        "UI Automation root": "AutomationElement.FromHandle",
        "ControlView traversal": "TreeWalker.ControlViewWalker",
        "exact element path": '"elementPath"',
        "bounded path depth": "MaxDepth = 8",
        "bounded nodes": "MaxNodes = 200",
        "automation id metadata": '"automationId"',
        "supported actions metadata": '"actions"',
        "InvokePattern": "InvokePattern.Pattern",
        "TogglePattern": "TogglePattern.Pattern",
        "SelectionItemPattern": "SelectionItemPattern.Pattern",
        "ExpandCollapsePattern": "ExpandCollapsePattern.Pattern",
        "pattern lookup": "TryGetCurrentPattern",
        "invoke action": ".Invoke();",
        "toggle action": ".Toggle();",
        "select action": ".Select();",
        "expand action": ".Expand();",
        "collapse action": ".Collapse();",
        "pre/post mutation barrier": "ensureMutationRunning();",
        "sensitive read confirmation": '"confirmSensitiveRead"',
        "mutation confirmation": '"confirmMutation"',
        "no implicit foreground result": '"background":true',
    }
    for label, token in semantic_requirements.items():
        if token not in semantic:
            errors.append(f"semantic runtime missing {label}: {token}")

    if semantic.count("ensureMutationRunning();") < 6:
        errors.append("semantic background mutations must re-check the shared mutation barrier before and after UIA action")

    for forbidden in (
        "ValuePattern.Pattern",
        "TextPattern.Pattern",
        "GetCurrentPattern(ValuePattern",
        "GetCurrentPattern(TextPattern",
        "SetFocus(",
        ".SetFocus(",
        "SendInput(",
        "SetCursorPos(",
        "mouse_event(",
        "keybd_event(",
        "Process.Start(",
        "CreateProcess(",
        "ShellExecute(",
        "cmd.exe",
        "powershell.exe",
    ):
        if forbidden in semantic:
            errors.append(f"background semantic runtime exposes forbidden foreground/execution surface: {forbidden}")

    for phrase in (
        "same-process BricsCAD",
        "does not focus",
        "does not move the cursor",
        "does not inject keyboard or mouse input",
        "no screenshot or OCR",
        "exact elementPath",
        "fail closed",
        "confirmMutation=true",
        "confirmSensitiveRead=true",
        "InvokePattern",
        "TogglePattern",
        "SelectionItemPattern",
        "ExpandCollapsePattern",
        "static/hosted qualification",
        "licensed BricsCAD runtime",
    ):
        if phrase.lower() not in runbook.lower():
            errors.append(f"runbook missing safety/runtime phrase: {phrase}")

    canonical_lower = canonical.lower()
    for phrase in (
        TREE_TOOL,
        ACTION_TOOL,
        "same-process background host control",
        "no implicit foreground fallback",
    ):
        if phrase.lower() not in canonical_lower:
            errors.append(f"canonical MCP runbook missing background semantic contract: {phrase}")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP background semantic UI action source contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
