#!/usr/bin/env python3
"""Guard V25 Material Catalog against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
PARTIAL = UI / "MaterialCatalogWindow.DarkHostTheme.cs"
WINDOW = UI / "MaterialCatalogWindow.xaml"
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
        ("PinMaterialCatalogSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        ("PinMaterialCatalogSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        ("PinMaterialCatalogSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        ("PinMaterialCatalogSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window selection resource boundary"),
        ("MaterialList.Resources[key] = brush;", "MaterialList local selection pin"),
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
        "OnSaveClick",
        "OnDeleteClick",
        "OnApplyClick",
        "OnExportClick",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: Material Catalog dark-host partial must remain presentation-only: {forbidden!r}")

    require(window, 'x:Name="MaterialList"', "MaterialList contract")
    require(window, 'SelectionChanged="OnMaterialSelectionChanged"', "material selection handler")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListViewItem}">', "ListViewItem style contract")

    print("PASS: V25 Material Catalog dark host-selection contract")


if __name__ == "__main__":
    main()
