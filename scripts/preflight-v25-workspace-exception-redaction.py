#!/usr/bin/env python3
"""Fail closed when modeless Workspace status publishes raw exception details."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"


def fail(message: str) -> None:
    print(f"ERROR: V25 Workspace exception-redaction preflight failed: {message}", file=sys.stderr)
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


def assert_order(block: str, first: str, second: str, context: str) -> None:
    first_at = block.find(first)
    second_at = block.find(second)
    if first_at < 0 or second_at < 0 or first_at >= second_at:
        fail(f"{context}: expected {first!r} before {second!r}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    if ".Message" in text:
        fail("WorkspacePanel still references Exception.Message; modeless status must use stable redacted diagnostics")

    helper = body(text, "private void ReportWorkspaceFailure(string operation)")
    for token in ("SetStatus(", "không hoàn tất", "Refresh Workspace"):
        if token not in helper:
            fail(f"redacted failure helper missing {token!r}")

    guarded_handlers = (
        "public void RefreshProject()",
        "public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)",
        "private void OnZoneChanged(object sender, SelectionChangedEventArgs e)",
        "private void OnFloorChanged(object sender, SelectionChangedEventArgs e)",
        "private void OnAddClick(object sender, RoutedEventArgs e)",
        "private void OnDeleteClick(object sender, RoutedEventArgs e)",
        "private void OnRefreshClick(object sender, RoutedEventArgs e)",
        "private void OnFamilySelectionChanged(object sender, SelectionChangedEventArgs e)",
    )
    for signature in guarded_handlers:
        block = body(text, signature)
        if "ReportWorkspaceFailure(" not in block:
            fail(f"{signature} does not route failure through the redacted Workspace helper")
        if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", block):
            fail(f"{signature} still captures an exception object for user-visible failure reporting")

    post_commit = body(text, "private void RefreshAfterCommit(Action refresh, string successMessage, string context)")
    if "ReportWorkspacePostCommitWarning(successMessage, context)" not in post_commit:
        fail("post-commit refresh does not route UI-only failure through the stable warning helper")
    assert_order(post_commit, "SetStatus(successMessage)", "ReportWorkspacePostCommitWarning", "post-commit truth")

    post_commit_helper = body(text, "private void ReportWorkspacePostCommitWarning(string successMessage, string context)")
    for token in ("đã commit", "đồng bộ UI chưa hoàn tất", "Refresh Workspace"):
        if token not in post_commit_helper:
            fail(f"post-commit warning helper missing {token!r}")

    send = body(text, "private void Send(string command)")
    if "ReportWorkspaceFailure(\"Gửi lệnh \" + normalized)" not in send:
        fail("command dispatch failure does not use stable redacted Workspace reporting")
    if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", send):
        fail("command dispatch still captures an exception object for user-visible reporting")

    print("OK: V25 Workspace modeless failure reporting is stable, exception-redacted, and post-commit truthful.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
