#!/usr/bin/env python3
"""Guard V25 Revision/Recognition/Wall Takeoff collection selection against bright host chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI / "Theme.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def check_guard(partial: str, prefix: str, controls: tuple[str, ...]) -> None:
    for token, label in (
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightBrushKey, bg);", "active selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);", "inactive selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightTextBrushKey, fg);", "active selection foreground"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window resource boundary"),
        ('TryFindResource("BgSelectedBrush") is Brush bg', "selected brush lookup"),
        ('TryFindResource("TextBrush") is Brush fg', "selected text lookup"),
    ):
        require(partial, token, f"{prefix}: {label}")
    for control in controls:
        require(partial, f"{control}.Resources[key] = brush;", f"{prefix}: {control} local pin")

    for forbidden in (
        "Click +=",
        "SendStringToExecute",
        "CommandMethod(",
        "Application.DocumentManager",
        "Transaction",
        "ProjectContextCoordinator",
        "Export",
        "Locate",
        "ApplyClick",
        "Refresh",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: {prefix} dark-host partial must remain presentation-only: {forbidden!r}")


def main() -> None:
    revision = read(UI / "RevisionWindow.DarkHostTheme.cs")
    recognition = read(UI / "RecognitionWindow.DarkHostTheme.cs")
    wall = read(UI / "WallQuantityWindow.DarkHostTheme.cs")
    revision_xaml = read(UI / "RevisionWindow.xaml")
    recognition_xaml = read(UI / "RecognitionWindow.xaml")
    wall_xaml = read(UI / "WallQuantityWindow.xaml")
    theme = read(THEME)

    check_guard(revision, "Revision", ("Grid", "SemanticGrid"))
    check_guard(recognition, "Recognition", ("Grid",))
    check_guard(wall, "WallQuantity", ("WallList", "TakeoffGrid"))

    require(revision_xaml, 'x:Name="Grid"', "revision quantity grid")
    require(revision_xaml, 'MouseDoubleClick="OnGridDoubleClick"', "revision quantity locate gesture")
    require(revision_xaml, 'x:Name="SemanticGrid"', "revision semantic grid")
    require(revision_xaml, 'MouseDoubleClick="OnSemanticGridDoubleClick"', "revision semantic locate gesture")
    require(recognition_xaml, 'x:Name="Grid"', "recognition grid")
    require(recognition_xaml, 'MouseDoubleClick="OnGridDoubleClick"', "recognition locate gesture")
    require(wall_xaml, 'x:Name="WallList"', "wall list")
    require(wall_xaml, 'SelectionChanged="OnWallSelectionChanged"', "wall list selection handler")
    require(wall_xaml, 'x:Name="TakeoffGrid"', "wall takeoff grid")
    require(wall_xaml, 'SelectionChanged="OnGridSelectionChanged"', "wall grid selection handler")
    require(wall_xaml, 'MouseDoubleClick="OnGridDoubleClick"', "wall grid locate gesture")

    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListBoxItem}">', "ListBoxItem style contract")
    require(theme, '<Style TargetType="{x:Type DataGridRow}">', "DataGridRow style contract")
    require(theme, '<Style TargetType="{x:Type DataGridCell}">', "DataGridCell style contract")

    print("PASS: V25 review/takeoff dark host-selection contract")


if __name__ == "__main__":
    main()
