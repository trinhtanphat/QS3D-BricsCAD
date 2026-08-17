#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_REL = "src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs"


def require(text, needle, scope):
    if needle not in text:
        raise SystemExit(f"FAIL: {scope} missing required responsive contract: {needle}")


def forbid(text, needle, scope):
    if needle in text:
        raise SystemExit(f"FAIL: {scope} contains stale/non-responsive contract: {needle}")


def section(text, start, end):
    try:
        return text.split(start, 1)[1].split(end, 1)[0]
    except IndexError as exc:
        raise SystemExit(f"FAIL: could not isolate section {start}") from exc


def main():
    source = (ROOT / SOURCE_REL).read_text(encoding="utf-8")

    shell = section(source, "private UIElement BuildShell()", "private Grid BuildLeftPane()")
    for needle in (
        "var leftContent = BuildLeftPane();",
        "var left = CreateVerticalScrollViewer(leftContent);",
        "left.SizeChanged += (_, e) => leftContent.MinHeight = Math.Max(0d, e.NewSize.Height);",
    ):
        require(shell, needle, SOURCE_REL + "::BuildShell")
    require(shell, "Grid.SetRow(status, 1);", SOURCE_REL + "::BuildShell")

    left = section(source, "private Grid BuildLeftPane()", "private Grid BuildRecentPane()")
    for needle in (
        "var brand = new Grid();",
        "brand.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });",
        "brand.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });",
        "Text = \"Giải pháp mô hình hóa thông tin công trình BIM 3D trực quan và tối ưu hóa bóc tách khối lượng.\"",
        "TextWrapping = TextWrapping.Wrap",
    ):
        require(left, needle, SOURCE_REL + "::BuildLeftPane")
    forbid(left, "tối ưu\\nhóa", SOURCE_REL + "::BuildLeftPane")
    if left.count("TextWrapping = TextWrapping.Wrap") < 3:
        raise SystemExit("FAIL: left pane must wrap brand, description and version text")

    recent = section(source, "private Grid BuildRecentPane()", "private static ScrollViewer CreateVerticalScrollViewer(UIElement content)")
    require(recent, "Text = \"Nhấp vào dự án để mở trực tiếp và bắt đầu làm việc\"", SOURCE_REL + "::BuildRecentPane")
    require(recent, "TextWrapping = TextWrapping.Wrap", SOURCE_REL + "::BuildRecentPane")
    require(recent, "var scroll = CreateVerticalScrollViewer(_recentPanel);", SOURCE_REL + "::BuildRecentPane")

    scroll = section(source, "private static ScrollViewer CreateVerticalScrollViewer(UIElement content)", "private static Style CreateCompactScrollBarStyle()")
    for needle in (
        "VerticalScrollBarVisibility = ScrollBarVisibility.Auto",
        "HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled",
        "CanContentScroll = false",
        "PanningMode = PanningMode.VerticalOnly",
        "Content = content",
    ):
        require(scroll, needle, SOURCE_REL + "::CreateVerticalScrollViewer")

    cards = section(source, "private Button CreateActionCard(", "private UIElement StatusButton(")
    if cards.count("TextWrapping = TextWrapping.Wrap") < 2:
        raise SystemExit("FAIL: action-card title and subtitle must both wrap")
    require(cards, "button.MinHeight = compact ? 54 : 58;", SOURCE_REL + "::CreateActionCard")

    recent_row = section(source, "private UIElement CreateRecentRow(", "private void OpenRecentProject(")
    require(recent_row, "TextTrimming = TextTrimming.CharacterEllipsis", SOURCE_REL + "::CreateRecentRow")

    print("PASS: embedded Start Center keeps the status strip fixed, routes both panes through the shared vertical-only pixel scroller, disables horizontal scrolling, and wraps narrow-width descriptive/action text.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
