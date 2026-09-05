#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
OBSERVER_PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPopupObserver.cs"
HOST_PATH = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpBackgroundHostRuntime.cs"


def fail(message: str) -> None:
    print("ERROR: MCP popup hygiene preflight failed: " + message, file=sys.stderr)
    raise SystemExit(1)


observer = OBSERVER_PATH.read_text(encoding="utf-8")
host = HOST_PATH.read_text(encoding="utf-8")

required_observer = {
    "shared popup-root classifier": "internal static bool IsPopupRoot(IntPtr hwnd)",
    "WPF/custom popup UI Automation import": "using System.Windows.Automation;",
    "UI Automation root acquisition": "AutomationElement.FromHandle(hwnd)",
    "bounded ControlView traversal": "TreeWalker.ControlViewWalker",
    "safe UIA name metadata read": ".Current.Name",
    "LookFrom exclusion": '"LookFrom"',
    "LookFromToolTip exclusion": '"LookFromToolTip"',
    "mini command-line exclusion": '"mini-command-line-frame"',
}
for label, needle in required_observer.items():
    if needle not in observer:
        fail(label + " is missing")

for forbidden in ("ValuePattern", "TextPattern"):
    if forbidden in observer:
        fail("popup observer must not read editable UI Automation content via " + forbidden)

if "var isPopupTop = hwnd != mainWindow;" in host:
    fail('scope=popup still treats every non-main top-level HWND as a popup')
if "McpPopupObserver.IsPopupRoot(hwnd)" not in host:
    fail("text popup scope does not reuse the popup-root classifier")
if 'scope == "popup" && LooksLikeCommandLineClass(className)' not in host:
    fail("popup text scope does not exclude command-line/edit descendants")

print("MCP popup hygiene source preflight passed.")
