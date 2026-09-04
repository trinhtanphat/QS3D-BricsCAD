#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
BACKGROUND = SRC / "McpBackgroundHostRuntime.cs"
SEMANTIC = SRC / "McpBackgroundSemanticUiRuntime.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-background-semantic-ui.md"


def fail(message: str) -> None:
    print(f"ERROR: MCP background semantic UI preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (BACKGROUND, SEMANTIC, RUNBOOK):
    if not path.is_file():
        fail(f"missing {path.relative_to(ROOT)}")

background = BACKGROUND.read_text(encoding="utf-8")
semantic = SEMANTIC.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for tool in ("bricscad_ui_text_snapshot", "bricscad_ui_invoke"):
    if f'"{tool}"' not in background:
        fail(f"background runtime missing compatibility tool {tool}")

background_requirements = {
    "semantic snapshot mode": "semantic",
    "semantic mode schema": "enum\\\":[\\\"text\\\",\\\"semantic\\\"]",
    "semantic window handle schema": "WindowHandleProperty()",
    "semantic element path schema": "elementPath",
    "semantic action schema": "invoke\\\",\\\"toggle\\\",\\\"select\\\",\\\"expand\\\",\\\"collapse",
    "expected control type schema": "expectedControlType",
    "expected automation id schema": "expectedAutomationId",
    "semantic tree routing": "McpBackgroundSemanticUiRuntime.SemanticTree(body, audit)",
    "semantic action routing": "McpBackgroundSemanticUiRuntime.SemanticAction(body, ensureMutationRunning, audit)",
    "legacy Win32 button path": 'string.Equals(className, "Button", StringComparison.OrdinalIgnoreCase)',
    "legacy bounded message": "SendMessageBounded(hwnd, BM_CLICK",
    "sensitive read confirmation": 'McpTopLevelJson.ExtractBoolean(body, "confirmSensitiveRead")',
    "background result marker": "background",
}
for label, token in background_requirements.items():
    if token not in background:
        fail(f"background runtime missing {label}: {token}")

semantic_requirements = {
    "UI Automation": "System.Windows.Automation",
    "same-process target guard": "RequiredCurrentBricsCadWindow",
    "current BricsCAD process": "Process.GetCurrentProcess()",
    "UIA process identity": "root.Current.ProcessId",
    "UIA root": "AutomationElement.FromHandle",
    "ControlView traversal": "TreeWalker.ControlViewWalker",
    "bounded depth": "MaxDepth = 8",
    "bounded nodes": "MaxNodes = 200",
    "exact path metadata": "elementPath",
    "automation id metadata": "automationId",
    "control type metadata": "controlType",
    "actions metadata": "actions",
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
    "mutation confirmation": "confirmMutation",
    "sensitive discovery confirmation": "confirmSensitiveRead",
    "pre/post mutation barrier": "ensureMutationRunning();",
    "stale path fail closed": "elementPath is stale or no longer resolves",
    "control type mismatch fail closed": "Semantic target control type changed",
    "automation id mismatch fail closed": "Semantic target automationId changed",
    "background result": "background",
}
for label, token in semantic_requirements.items():
    if token not in semantic:
        fail(f"semantic runtime missing {label}: {token}")

if semantic.count("ensureMutationRunning();") < 2:
    fail("semantic mutations must re-check the shared mutation/emergency barrier before and after the UIA action")

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
    "PrintWindow(",
    "BitBlt(",
    "Process.Start(",
    "CreateProcess(",
    "ShellExecute(",
    "cmd.exe",
    "powershell.exe",
    "System.Management.Automation",
    "File.ReadAllText(",
    "Directory.GetFiles(",
):
    if forbidden.lower() in semantic.lower():
        fail(f"semantic runtime exposes forbidden foreground/read/execution surface: {forbidden}")

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
        fail(f"runbook missing safety/runtime phrase: {phrase}")

print("MCP background semantic UI source contract passed.")
