#!/usr/bin/env python3
"""Guard V25 Model Health/Audit Log grids against bright host selection chrome."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
MODEL_PARTIAL = UI / "ModelHealthWindow.DarkHostTheme.cs"
AUDIT_PARTIAL = UI / "AuditLogWindow.DarkHostTheme.cs"
MODEL_XAML = UI / "ModelHealthWindow.xaml"
AUDIT_XAML = UI / "AuditLogWindow.xaml"
THEME = UI / "Theme.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def check_guard(text: str, prefix: str, grid_name: str) -> None:
    for token, label in (
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background"),
        (f"Pin{prefix}SelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground"),
        (f"Pin{prefix}SelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground"),
        ("Resources[key] = brush;", "window selection resource boundary"),
        (f"{grid_name}.Resources[key] = brush;", "DataGrid local selection pin"),
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
        "Locate",
        "Refresh",
        "Filter",
    ):
        if forbidden in text:
            raise SystemExit(f"FAIL: {prefix} dark-host partial must remain presentation-only: {forbidden!r}")


def main() -> None:
    model_partial = read(MODEL_PARTIAL)
    audit_partial = read(AUDIT_PARTIAL)
    model_xaml = read(MODEL_XAML)
    audit_xaml = read(AUDIT_XAML)
    theme = read(THEME)

    check_guard(model_partial, "ModelHealth", "IssueGrid")
    check_guard(audit_partial, "AuditLog", "Grid")

    require(model_xaml, 'x:Name="IssueGrid"', "IssueGrid contract")
    require(model_xaml, 'MouseDoubleClick="OnGridDoubleClick"', "Model Health locate gesture")
    require(audit_xaml, 'x:Name="Grid"', "Audit Grid contract")
    require(audit_xaml, 'TextChanged="OnSearchChanged"', "Audit search contract")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type DataGridRow}">', "DataGridRow style contract")
    require(theme, '<Style TargetType="{x:Type DataGridCell}">', "DataGridCell style contract")

    print("PASS: V25 diagnostic DataGrid dark host-selection contract")


if __name__ == "__main__":
    main()
