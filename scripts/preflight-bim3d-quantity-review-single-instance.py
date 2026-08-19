#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS_REL = "src/QS3D.BricsCAD.V25/Commands.cs"
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.SingleInstance.cs"


def require(text, token, rel):
    if token not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {token}")


def forbid(text, token, rel):
    if token in text:
        raise SystemExit(f"FAIL: {rel} must not introduce feature-specific window registry/host token: {token}")


def main():
    commands = (ROOT / COMMANDS_REL).read_text(encoding="utf-8")
    window = (ROOT / WINDOW_REL).read_text(encoding="utf-8")

    for token in (
        '[CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]',
        "new QuantitySummaryWindow(doc, rows, locate, recalculate)",
        "Application.ShowModelessWindow",
    ):
        require(commands, token, COMMANDS_REL)

    # Guard the behavior, not one particular top-level-window enumeration syntax.
    # QS3D is hosted by BricsCAD, so the live-window discovery contract must also
    # work when System.Windows.Application.Current is not available.
    for token in (
        "public partial class QuantitySummaryWindow",
        "protected override void OnSourceInitialized(EventArgs e)",
        "Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ReuseExistingLogicalWindow));",
        "EnumerateLiveReviewWindows()",
        "PresentationSource.CurrentSources",
        "!ReferenceEquals(window, this)",
        "ReferenceEquals(window._document, _document)",
        'existing.EnsureCurrentProject("làm mới BQ khi gọi lại QS3DBQ");',
        "existing.RefreshRowsForCurrentMode(false);",
        "existing.WindowState == WindowState.Minimized",
        "existing.Activate();",
        "Close();",
        "try { existing.Close(); }",
    ):
        require(window, token, WINDOW_REL)

    for token in (
        "static Dictionary",
        "ConditionalWeakTable",
        "WorkspaceFloatingToolHost",
        "new Window",
        "if (application == null) return;",
    ):
        forbid(window, token, WINDOW_REL)

    print(
        "PASS: repeated QS3DBQ discovers live hosted-WPF review windows, refreshes/focuses the existing "
        "same-document quantity review before render, keeps different DWGs independent, and introduces no "
        "feature-specific window registry/host."
    )


if __name__ == "__main__":
    main()
