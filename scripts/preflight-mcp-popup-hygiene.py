#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
OBSERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPopupObserver.cs"
HOST = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpBackgroundHostRuntime.cs"
CLASSIFIER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPopupWindowClassifier.cs"

errors = []


def require_text(path, needle, reason):
    if not path.exists():
        errors.append(f"missing {path.relative_to(ROOT)}: {reason}")
        return
    text = path.read_text(encoding="utf-8")
    if needle not in text:
        errors.append(f"{path.relative_to(ROOT)} missing {needle!r}: {reason}")


if not OBSERVER.exists():
    errors.append("missing V25 popup observer source")
if not HOST.exists():
    errors.append("missing V25 background host source")
if not CLASSIFIER.exists():
    errors.append("missing shared V25 popup window classifier")

if HOST.exists():
    host = HOST.read_text(encoding="utf-8")
    if "var isPopupTop = hwnd != mainWindow;" in host:
        errors.append("scope=popup still treats every non-main top-level window as a popup")
    if "McpPopupWindowClassifier.IsPopupRoot(" not in host:
        errors.append("scope=popup does not use the shared popup classifier")

if OBSERVER.exists():
    observer = OBSERVER.read_text(encoding="utf-8")
    for needle, reason in (
        ("using System.Windows.Automation;", "WPF/custom dialog body extraction requires UI Automation fallback"),
        ("AutomationElement.FromHandle", "popup observer must inspect UIA-backed dialog descendants"),
        ("ControlType.Edit", "UIA fallback must explicitly exclude editable controls"),
        ("ControlType.Button", "UIA fallback must preserve button extraction"),
        ("ControlType.Text", "UIA fallback must capture non-editable body text"),
        ("McpPopupWindowClassifier.IsPopupRoot(", "observer and text snapshot must share popup classification"),
    ):
        if needle not in observer:
            errors.append(f"{OBSERVER.relative_to(ROOT)} missing {needle!r}: {reason}")
    if "ValuePattern" in observer or "TextPattern" in observer:
        errors.append("popup observer must not read UIA ValuePattern/TextPattern content from editable controls")

if CLASSIFIER.exists():
    classifier = CLASSIFIER.read_text(encoding="utf-8")
    for needle, reason in (
        ("internal static class McpPopupWindowClassifier", "classifier must be shared by observer and popup-scope snapshot"),
        ("#32770", "standard Win32 dialogs must remain recognized"),
        ("HwndWrapper", "WPF dialog roots must be recognized"),
        ("LookFrom", "BricsCAD LookFrom chrome must be explicitly rejected"),
        ("LookFromToolTip", "BricsCAD LookFrom tooltip chrome must be explicitly rejected"),
        ("mini-command-line-frame", "BricsCAD mini command line must be explicitly rejected"),
    ):
        if needle not in classifier:
            errors.append(f"{CLASSIFIER.relative_to(ROOT)} missing {needle!r}: {reason}")

if errors:
    print("ERROR: MCP popup hygiene preflight failed closed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("MCP popup hygiene preflight PASS")
