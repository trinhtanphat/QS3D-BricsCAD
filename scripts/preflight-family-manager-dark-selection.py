#!/usr/bin/env python3
"""Guard V25 Family Manager against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.DarkHostTheme.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.xaml"
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
        ("PinFamilyManagerSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        ("PinFamilyManagerSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        ("PinFamilyManagerSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        ("PinFamilyManagerSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window selection resource boundary"),
        ("FamilyList.Resources[key] = brush;", "FamilyList local selection pin"),
        ("PropertyList.Resources[key] = brush;", "PropertyList local selection pin"),
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
        "SetActiveFamily(",
        "Assign",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: Family Manager dark-host partial must remain presentation-only: {forbidden!r}")

    for token, label in (
        ('x:Name="FamilyList"', "FamilyList contract"),
        ('SelectionChanged="OnFamilySelectionChanged"', "Family selection handler"),
        ('x:Name="PropertyList"', "PropertyList contract"),
        ('SelectionChanged="OnPropertySelectionChanged"', "Property selection handler"),
    ):
        require(window, token, label)

    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListViewItem}">', "ListViewItem style contract")

    print("PASS: V25 Family Manager dark host-selection contract")


if __name__ == "__main__":
    main()
