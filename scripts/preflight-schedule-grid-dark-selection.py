#!/usr/bin/env python3
"""Guard V25 schedule DataGrids against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI / "Theme.xaml"
SURFACES = (
    ("DoorOpeningScheduleWindow", "ScheduleGrid", "OnSearchChanged"),
    ("RoomFinishScheduleWindow", "ScheduleGrid", "OnSearchChanged"),
    ("RebarScheduleWindow", "Grid", "OnGridDoubleClick"),
)


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def main() -> None:
    theme = read(THEME)
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type DataGridRow}">', "DataGridRow style contract")
    require(theme, '<Style TargetType="{x:Type DataGridCell}">', "DataGridCell style contract")

    for class_name, grid_name, behavior_token in SURFACES:
        partial = read(UI / f"{class_name}.DarkHostTheme.cs")
        xaml = read(UI / f"{class_name}.xaml")

        for token, label in (
            ("PinSelectionResource(SystemColors.HighlightBrushKey, bg);", "active selection background"),
            ("PinSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);", "inactive selection background"),
            ("PinSelectionResource(SystemColors.HighlightTextBrushKey, fg);", "active selection foreground"),
            ("PinSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);", "inactive selection foreground"),
            ("Resources[key] = brush;", "window resource boundary"),
            (f"{grid_name}.Resources[key] = brush;", "DataGrid local resource pin"),
            ('TryFindResource("BgSelectedBrush") is Brush bg', "selected brush lookup"),
            ('TryFindResource("TextBrush") is Brush fg', "selected text lookup"),
        ):
            require(partial, token, f"{class_name}: {label}")

        for forbidden in (
            "Click +=",
            "SendStringToExecute",
            "CommandMethod(",
            "Application.DocumentManager",
            "Transaction",
            "ProjectContextCoordinator",
            "Export",
            "Locate",
            "Refresh",
        ):
            if forbidden in partial:
                raise SystemExit(f"FAIL: {class_name} dark-host partial must remain presentation-only: {forbidden!r}")

        require(xaml, f'x:Name="{grid_name}"', f"{class_name} DataGrid contract")
        require(xaml, behavior_token, f"{class_name} behavior contract")

    print("PASS: V25 schedule DataGrid dark host-selection contract")


if __name__ == "__main__":
    main()
