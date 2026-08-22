#!/usr/bin/env python3
"""Guard V25 Quantity Summary against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.DarkHostTheme.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.xaml"
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
    window = read(WINDOW)
    theme = read(THEME)

    for token, label in (
        ("PinQuantitySummarySelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        ("PinQuantitySummarySelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        ("PinQuantitySummarySelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        ("PinQuantitySummarySelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window selection resource boundary"),
        ("CategoryList.Resources[key] = brush;", "CategoryList local selection pin"),
        ("QuantityGrid.Resources[key] = brush;", "QuantityGrid local selection pin"),
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
        "SemanticCaptureService",
        "RegenerationEngine",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: Quantity Summary dark-host partial must remain presentation-only: {forbidden!r}")

    for token, label in (
        ('x:Name="CategoryList"', "CategoryList contract"),
        ('SelectionChanged="OnCategoryChanged"', "category selection handler"),
        ('x:Name="QuantityGrid"', "QuantityGrid contract"),
        ('SelectionChanged="OnQuantityGridSelectionChanged"', "grid selection handler"),
        ('MouseDoubleClick="OnQuantityGridDoubleClick"', "grid double-click handler"),
    ):
        require(window, token, label)

    for token, label in (
        ('<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush"),
        ('<Style TargetType="{x:Type ListBoxItem}">', "ListBoxItem style contract"),
        ('<Style TargetType="{x:Type DataGridRow}">', "DataGridRow style contract"),
        ('<Style TargetType="{x:Type DataGridCell}">', "DataGridCell style contract"),
    ):
        require(theme, token, label)

    print("PASS: V25 Quantity Summary dark host-selection contract")


if __name__ == "__main__":
    main()
