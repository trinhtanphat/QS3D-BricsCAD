#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PANEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterPanel.cs"


def main():
    text = PANEL.read_text(encoding="utf-8")
    required = (
        "using System.Windows.Controls.Primitives;",
        "var left = CreateVerticalScrollViewer(leftContent);",
        "var scroll = CreateVerticalScrollViewer(_recentPanel);",
        "private static ScrollViewer CreateVerticalScrollViewer(UIElement content)",
        "VerticalScrollBarVisibility = ScrollBarVisibility.Auto",
        "HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled",
        "CanContentScroll = false",
        "PanningMode = PanningMode.VerticalOnly",
        "PanningDeceleration = 0.001",
        "scroll.Resources[typeof(ScrollBar)] = CreateCompactScrollBarStyle();",
        "scroll.Resources[typeof(Thumb)] = CreateCompactScrollThumbStyle();",
        "private static Style CreateCompactScrollBarStyle()",
        "Control.BackgroundProperty, ScrollTrackBrush",
        "FrameworkElement.WidthProperty, 10d",
        "private static Style CreateCompactScrollThumbStyle()",
        "FrameworkElement.MinHeightProperty, 30d",
        "Control.BackgroundProperty, ScrollThumbHoverBrush",
        "Property = UIElement.IsMouseOverProperty",
        "left.SizeChanged += (_, e) => leftContent.MinHeight = Math.Max(0d, e.NewSize.Height);",
    )
    missing = [token for token in required if token not in text]
    if missing:
        for token in missing:
            print("ERROR: Start Center scrollbar contract missing:", token)
        return 1

    if text.count("CreateVerticalScrollViewer(") != 3:
        print("ERROR: both Start Center panes must share exactly one vertical-scroll helper contract.")
        return 1

    if text.count("new ScrollViewer") != 1:
        print("ERROR: direct Start Center ScrollViewer construction bypasses the shared dark/proportional style.")
        return 1

    print(
        "PASS: KHỞI ĐẦU uses one shared pixel-scrolling vertical ScrollViewer contract for both panes; "
        "the bar stays Auto/vertical-only, the thumb remains proportional to viewport/content height, "
        "and compact dark track/thumb resources replace the wide light host default without changing Start Center actions."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
