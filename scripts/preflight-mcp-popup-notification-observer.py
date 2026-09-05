#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPopupObserver.cs"
CLASSIFIER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPopupWindowClassifier.cs"
ENTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"


def require(text: str, needle: str, label: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def main() -> int:
    if not SOURCE.is_file():
        print("ERROR: missing", SOURCE.relative_to(ROOT))
        return 1
    if not CLASSIFIER.is_file():
        print("ERROR: missing", CLASSIFIER.relative_to(ROOT))
        return 1
    if not ENTRY.is_file():
        print("ERROR: missing", ENTRY.relative_to(ROOT))
        return 1

    source = SOURCE.read_text(encoding="utf-8")
    classifier = CLASSIFIER.read_text(encoding="utf-8")
    entry = ENTRY.read_text(encoding="utf-8")
    errors: list[str] = []

    required_source = {
        "current-process WinEvent filter": "_processId = (uint)Process.GetCurrentProcess().Id",
        "dialog WinEvent hook": "EventSystemDialogStart",
        "show WinEvent hook": "EventObjectShow",
        "shared popup classifier": "McpPopupWindowClassifier.IsPopupRoot",
        "static notification text": 'childClass.IndexOf("Static", StringComparison.OrdinalIgnoreCase)',
        "button captions": 'childClass.IndexOf("Button", StringComparison.OrdinalIgnoreCase)',
        "editable controls excluded": 'childClass.IndexOf("Edit", StringComparison.OrdinalIgnoreCase)',
        "bounded popup event": 'McpDiagnosticHub.Record("bricscad", "warning", "popup-notification"',
        "dedupe": "ShouldRecord(hwnd, signature)",
        "hook cleanup": "UnhookWinEvent",
    }
    for label, token in required_source.items():
        require(source, token, label, errors)

    required_classifier = {
        "standard dialog class": 'string.Equals(className, "#32770", StringComparison.Ordinal)',
        "owned-window fallback": "GetWindow(hwnd, GwOwner)",
        "owned-window process boundary": "BelongsToCurrentProcess(owner)",
    }
    for label, token in required_classifier.items():
        require(classifier, token, label, errors)

    required_entry = {
        "observer startup": "McpPopupObserver.Start();",
        "observer teardown": "TryCleanup(McpPopupObserver.Stop);",
        "optional startup isolation": 'ReportOptionalStartupFailure("popup notification observer", ex);',
    }
    for label, token in required_entry.items():
        require(entry, token, label, errors)

    forbidden = {
        "cross-process popup scraping": "WINEVENT_SKIPOWNPROCESS",
        "editable popup text capture": "GetDlgItemText",
    }
    for label, token in forbidden.items():
        if token in source or token in classifier:
            errors.append(f"forbidden {label}: {token}")

    if errors:
        for error in errors:
            print("ERROR: MCP popup observer", error)
        return 1

    print("PASS MCP popup notification observer: current-process dialog text -> shared classifier -> bounded diagnostics -> cad_audit_tail")
    return 0


if __name__ == "__main__":
    sys.exit(main())
