#!/usr/bin/env python3
"""Guard V25 Start Center lists against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
PARTIAL = UI / "StartCenterWindow.DarkHostTheme.cs"
WINDOW = UI / "StartCenterWindow.xaml"
THEME = UI / "Theme.xaml"
LISTS = ("CommandList", "FavoriteList", "RecentCommandList", "RecentProjectList")


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
        ("PinStartCenterSelectionResource(SystemColors.HighlightBrushKey, bg);", "active selection background"),
        ("PinStartCenterSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, bg);", "inactive selection background"),
        ("PinStartCenterSelectionResource(SystemColors.HighlightTextBrushKey, fg);", "active selection foreground"),
        ("PinStartCenterSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, fg);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window resource boundary"),
        ('TryFindResource("BgSelectedBrush") is Brush bg', "selected brush lookup"),
        ('TryFindResource("TextBrush") is Brush fg', "selected text lookup"),
    ):
        require(partial, token, label)

    for name in LISTS:
        require(partial, f"{name}.Resources[key] = brush;", f"{name} local selection pin")
        require(window, f'x:Name="{name}"', f"{name} XAML contract")

    for token in (
        'MouseDoubleClick="OnCommandDoubleClick"',
        'MouseDoubleClick="OnFavoriteDoubleClick"',
        'MouseDoubleClick="OnRecentCommandDoubleClick"',
        'MouseDoubleClick="OnRecentProjectDoubleClick"',
    ):
        require(window, token, "Start Center double-click contract")

    for forbidden in (
        "Click +=",
        "SendStringToExecute",
        "CommandMethod(",
        "Application.DocumentManager",
        "Process.Start",
        "File.",
        "ProjectContextCoordinator",
    ):
        if forbidden in partial:
            raise SystemExit(f"FAIL: Start Center dark-host partial must remain presentation-only: {forbidden!r}")

    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type ListBoxItem}">', "ListBoxItem style contract")

    print("PASS: V25 Start Center dark host-selection contract")


if __name__ == "__main__":
    main()
