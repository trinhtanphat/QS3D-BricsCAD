#!/usr/bin/env python3
"""Guard RightPanel against bright BricsCAD/WPF host selection/menu chrome."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RightPanel.DarkHostTheme.cs"
PANEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RightPanel.xaml"
THEME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Theme.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def main() -> None:
    partial = read(PARTIAL)
    panel = read(PANEL)
    theme = read(THEME)

    for token, label in (
        ("PinRightSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        ("PinRightSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        ("PinRightSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        ("PinRightSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("DrawingList.Resources[key] = brush;", "DrawingList local resource pin"),
        ("LayerList.Resources[key] = brush;", "LayerList local resource pin"),
        ("ApplyRightDarkContextMenu(DrawingList.ContextMenu);", "Drawing context menu coverage"),
        ("ApplyRightDarkContextMenu(LayerList.ContextMenu);", "Layer context menu coverage"),
        ("BuildRightDarkMenuItemStyle()", "MenuItem owned template"),
        ("BuildRightDarkSeparatorStyle()", "separator owned template"),
        ("Property = MenuItem.IsHighlightedProperty", "menu hover trigger"),
        ("Property = MenuItem.IsSubmenuOpenProperty", "submenu selected trigger"),
        ("menu.HasDropShadow = false;", "host popup shadow suppression"),
        ('TryFindResource("BgHoverBrush")', "QS3D hover brush"),
        ('TryFindResource("BgSelectedBrush")', "QS3D selected brush"),
        ('TryFindResource("TextBrush")', "QS3D text brush"),
    ):
        require(partial, token, label)

    for forbidden in (
        "Click +=",
        "SendStringToExecute",
        "CommandMethod(",
        "Application.DocumentManager",
        "Transaction",
        "ProjectContextCoordinator",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: RightPanel dark-host partial must remain presentation-only: {forbidden!r}")

    require(panel, 'x:Name="DrawingList"', "DrawingList contract")
    require(panel, 'x:Name="LayerList"', "LayerList contract")
    if panel.count("<ListView.ContextMenu>") != 2:
        raise SystemExit("FAIL: expected exactly two RightPanel ListView context menus")
    if panel.count("<MenuItem ") < 10:
        raise SystemExit("FAIL: RightPanel context-menu command surface unexpectedly shrank")

    require(theme, '<Style TargetType="{x:Type ListViewItem}">', "canonical ListViewItem style")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<SolidColorBrush x:Key="BgHoverBrush"', "canonical hover brush")

    print("PASS: V25 RightPanel dark host chrome contract")


if __name__ == "__main__":
    main()
