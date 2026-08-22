#!/usr/bin/env python3
"""Guard V25 Quantity Settings against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
PARTIAL = UI / "QuantitySettingsWindow.DarkHostTheme.cs"
WINDOW = UI / "QuantitySettingsWindow.xaml"
THEME = UI / "Theme.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def main() -> None:
    partial = read(PARTIAL)
    window = read(WINDOW)
    theme = read(THEME)

    for token, label in (
        ("PinQuantitySettingsSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        ("PinQuantitySettingsSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        ("PinQuantitySettingsSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        ("PinQuantitySettingsSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window selection resource boundary"),
        ("PrimaryCategoryList.Resources[key] = brush;", "primary category local pin"),
        ("ReferenceCategoryList.Resources[key] = brush;", "reference category local pin"),
        ('TryFindResource("BgSelectedBrush") is Brush selectionBrush', "QS3D selected brush lookup"),
        ('TryFindResource("TextBrush") is Brush selectionTextBrush', "QS3D selected text lookup"),
    ):
        require(partial, token, label)

    for forbidden in (
        "Click +=",
        "SendStringToExecute",
        "CommandMethod(",
        "Application.DocumentManager",
        "Transaction",
        "ProjectContextCoordinator",
        "Save(",
        "Reset(",
        "Import",
        "Export",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: Quantity Settings dark-host partial must remain presentation-only: {forbidden!r}")

    for token, label in (
        ('x:Name="PrimaryCategoryList"', "primary category list contract"),
        ('x:Name="ReferenceCategoryList"', "reference category list contract"),
        ('SelectionChanged="IntersectionCategorySelectionChanged"', "intersection category handler"),
        ('x:Name="MissingCategoryRuleList"', "missing category rule selector"),
        ('SelectionChanged="MissingCategoryRuleSelectionChanged"', "missing category handler"),
        ("<DataGrid ", "DataGrid inheritance surface"),
    ):
        require(window, token, label)

    if window.count("<DataGrid ") < 1:
        raise SystemExit("FAIL: expected at least one Quantity Settings DataGrid surface")

    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListBoxItem}">', "ListBoxItem style contract")
    require(theme, '<Style TargetType="{x:Type DataGridRow}">', "DataGridRow style contract")
    require(theme, '<Style TargetType="{x:Type DataGridCell}">', "DataGridCell style contract")

    print("PASS: V25 Quantity Settings dark host-selection contract")


if __name__ == "__main__":
    main()
