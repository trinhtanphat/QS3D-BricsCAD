#!/usr/bin/env python3
"""Guard V25 Zone/Floor managers against bright host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
ZONE_PARTIAL = UI / "ZoneManagerWindow.DarkHostTheme.cs"
FLOOR_PARTIAL = UI / "FloorLevelWindow.DarkHostTheme.cs"
ZONE_XAML = UI / "ZoneManagerWindow.xaml"
FLOOR_XAML = UI / "FloorLevelWindow.xaml"
THEME = UI / "Theme.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def check_guard(text: str, prefix: str, list_name: str) -> None:
    for token, label in (
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window resource boundary"),
        (f"{list_name}.Resources[key] = brush;", "ListView local resource pin"),
        ('TryFindResource("BgSelectedBrush") is Brush selectionBrush', "selected brush lookup"),
        ('TryFindResource("TextBrush") is Brush selectionTextBrush', "selected text lookup"),
    ):
        require(text, token, f"{prefix}: {label}")

    for forbidden in (
        "Click +=",
        "SendStringToExecute",
        "CommandMethod(",
        "Application.DocumentManager",
        "Transaction",
        "ProjectContextCoordinator",
        "Assign",
        "Activate",
        "Delete",
    ):
        if forbidden in text:
            raise SystemExit(f"FAIL: {prefix} dark-host partial must remain presentation-only: {forbidden!r}")


def main() -> None:
    zone_partial = read(ZONE_PARTIAL)
    floor_partial = read(FLOOR_PARTIAL)
    zone_xaml = read(ZONE_XAML)
    floor_xaml = read(FLOOR_XAML)
    theme = read(THEME)

    check_guard(zone_partial, "Zone", "ZoneList")
    check_guard(floor_partial, "Floor", "FloorList")

    require(zone_xaml, 'x:Name="ZoneList"', "ZoneList contract")
    require(zone_xaml, 'SelectionChanged="OnZoneSelectionChanged"', "Zone selection handler")
    require(floor_xaml, 'x:Name="FloorList"', "FloorList contract")
    require(floor_xaml, 'SelectionChanged="OnFloorSelectionChanged"', "Floor selection handler")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListViewItem}">', "ListViewItem style contract")

    print("PASS: V25 Zone/Floor manager dark host-selection contract")


if __name__ == "__main__":
    main()
