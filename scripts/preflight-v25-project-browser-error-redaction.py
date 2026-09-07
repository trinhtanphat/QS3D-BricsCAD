#!/usr/bin/env python3
"""Fail closed when V25 Project Browser modeless status exposes host exception details."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs"


def fail(message: str) -> None:
    print(f"ERROR: V25 Project Browser error-redaction preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        fail(f"missing {signature}")
    brace = text.find("{", start)
    if brace < 0:
        fail(f"missing body for {signature}")
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    fail(f"unterminated body for {signature}")
    return ""


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    if ".Message" in text:
        fail("Project Browser still publishes Exception.Message/raw exception details")
    if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", text):
        fail("Project Browser catches still retain exception objects for modeless reporting")

    reporter = body(text, "private void ReportProjectBrowserFailure(string operation, bool clearBrowser = false)")
    for token in ("Chi tiết nội bộ đã được ẩn", "Refresh Project Browser", "ClearProjectBrowser(message)", "SetBrowserStatus(message)"):
        if token not in reporter:
            fail(f"stable Project Browser failure reporter missing {token!r}")

    guarded = (
        ("private void OnBrowserResetClick(object sender, RoutedEventArgs e)", 'ReportProjectBrowserFailure("Reset Project Browser")'),
        ("private void ApplyBrowserView()", 'ReportProjectBrowserFailure("Project Browser view")'),
        ("private void RefreshProjectBrowser(bool forceRebind, bool revealPrimarySelection = false)", 'ReportProjectBrowserFailure("Refresh Project Browser", true)'),
        ("private void OnBrowserNodeSelectionChanged(object sender, SelectionChangedEventArgs e)", 'ReportProjectBrowserFailure("Project Browser node")'),
        ("private void OnBrowserNodeDoubleClick(object sender, MouseButtonEventArgs e)", 'ReportProjectBrowserFailure("Project Browser expand/collapse")'),
        ("private void SelectBrowserCad(bool zoom)", 'ReportProjectBrowserFailure("Browser → CAD")'),
    )
    for signature, expected in guarded:
        block = body(text, signature)
        if "catch (Exception)" not in block:
            fail(f"{signature} must use a redacted non-capturing exception boundary")
        if expected not in block:
            fail(f"{signature} does not route failure through stable Project Browser reporting")

    resolve = body(text, "private void ResolveAndSelectBrowserCad(Document document, ProjectState project, IReadOnlyList<string> elementIds, bool zoom)")
    set_pickfirst = resolve.find("document.Editor.SetImpliedSelection(objectIds.ToArray());")
    persist = resolve.find("PersistBrowserState(document, project, state);")
    warning = resolve.find("ReportProjectBrowserPostSelectionWarning();")
    if set_pickfirst < 0 or persist < 0 or warning < 0 or not (set_pickfirst < persist < warning):
        fail("post-selection warning must remain after native PICKFIRST commit and failed presentation-state persistence")
    if "catch (Exception)" not in resolve:
        fail("post-selection persistence boundary must not retain an exception object")

    post = body(text, "private void ReportProjectBrowserPostSelectionWarning()")
    for token in ("CAD selection đã commit", "browser presentation state", "Refresh Project Browser"):
        if token not in post:
            fail(f"post-selection truth helper missing {token!r}")

    print("OK: V25 Project Browser failures are exception-redacted and post-PICKFIRST warnings preserve committed native truth.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
