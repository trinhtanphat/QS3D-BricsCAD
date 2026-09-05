#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_REL = "src/QS3D.BricsCAD.V25/McpPopupObserver.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def method_slice(source: str, start_needle: str, end_needle: str, label: str) -> str:
    start = require(source, start_needle, f"missing {label} start")
    end = source.find(end_needle, start + len(start_needle))
    if end < 0:
        fail(f"missing {label} boundary")
    return source[start:end]


def main() -> int:
    path = ROOT / SOURCE_REL
    if not path.exists():
        fail(f"missing required source: {SOURCE_REL}")

    source = path.read_text(encoding="utf-8")

    candidate = method_slice(
        source,
        "private static bool IsCandidateRoot",
        "private static PopupCapture Capture",
        "popup candidate predicate",
    )
    require(candidate, "IsKnownNonDialogChrome", "popup candidate predicate must reject known BricsCAD chrome")
    require(source, '"LookFrom"', "popup observer must explicitly exclude LookFrom chrome")
    require(source, '"mini-command-line-frame"', "popup observer must explicitly exclude mini command-line frame")
    require(source, "IsCommandLineClass", "popup observer must centralize command-line subtree rejection")

    capture = method_slice(
        source,
        "private static PopupCapture Capture",
        "private static string NormalizeText",
        "popup capture",
    )
    require(capture, "IsCommandLineClass", "popup capture must reject command-line descendants")
    require(capture, "CaptureAutomationText", "popup capture must fall back to UI Automation text extraction")
    require(source, "AutomationElement.FromHandle", "UI Automation fallback must begin at the popup HWND")
    require(source, "TextPattern.Pattern", "UI Automation fallback must support TextPattern")
    require(source, "ValuePattern.Pattern", "UI Automation fallback must support ValuePattern")

    require(source, "ClassifySeverity", "popup severity must be evidence-based")
    if 'Severity = "warning"' in capture:
        fail("popup capture must not assign warning severity unconditionally")

    print("PASS: V25 popup observer rejects BricsCAD chrome/command-line noise, captures UIA text, and classifies severity from evidence.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
