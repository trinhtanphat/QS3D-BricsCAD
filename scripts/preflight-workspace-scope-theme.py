#!/usr/bin/env python3
"""Guard Workspace model-tree contrast and truthful Zone/Floor scope presentation."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
DARK_THEME = UI / "WorkspacePanel.DarkHostTheme.cs"
SCOPE = UI / "WorkspacePanel.ScopeDropdownHostInteraction.cs"
XAML = UI / "WorkspacePanel.xaml"
VIEW_MODEL = UI / "ViewModels" / "WorkspaceViewModel.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise RuntimeError(f"missing Workspace contract source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label} missing contract token: {token}" for token in tokens if token not in text]


def forbid_tokens(label: str, text: str, tokens: tuple[str, ...]) -> list[str]:
    return [f"{label} must not contain legacy token: {token}" for token in tokens if token in text]


def main() -> int:
    try:
        dark = read(DARK_THEME)
        scope = read(SCOPE)
        xaml = read(XAML)
        view_model = read(VIEW_MODEL)
    except (OSError, UnicodeError, RuntimeError) as exc:
        print(f"ERROR: workspace scope/theme preflight could not read source: {exc}", file=sys.stderr)
        return 1

    errors: list[str] = []
    errors += require_tokens(
        "Workspace dark-host theme",
        dark,
        (
            "TryFindResource(typeof(TreeViewItem)) is Style treeItemStyle",
            "PinModelTreeItemStyles(ModelTree.Items, treeItemStyle);",
            "item.Style = treeItemStyle;",
            "SystemColors.HighlightBrushKey",
            "SystemColors.HighlightTextBrushKey",
            'ModelTree.SetResourceReference(Control.ForegroundProperty, "TextBrush")',
        ),
    )

    # Scope interaction must stay host-safe but presentation-only. Empty/no-active labels
    # belong in XAML, never as fake rows inside WorkspaceViewModel collections.
    errors += require_tokens(
        "Workspace scope host interaction",
        scope,
        (
            "WireWorkspaceScopeCombo(ZoneCombo);",
            "WireWorkspaceScopeCombo(FloorCombo);",
            "!combo.HasItems",
        ),
    )
    errors += forbid_tokens(
        "Workspace scope host interaction",
        scope,
        (
            "EmptyZoneOption",
            "EmptyFloorOption",
            "NormalizeWorkspaceScopeCollection",
            "CollectionChanged",
            "ObservableCollection",
            "DispatcherPriority.DataBind",
        ),
    )

    errors += require_tokens(
        "Workspace scope bindings/placeholders",
        xaml,
        (
            'x:Name="ZoneCombo" ItemsSource="{Binding Zones}"',
            'x:Name="FloorCombo" ItemsSource="{Binding Floors}"',
            'x:Name="ZoneEmptyPlaceholder"',
            'Text="Không có Zone"',
            'x:Name="ZoneUnselectedPlaceholder"',
            'Text="Chưa chọn Zone"',
            'x:Name="FloorEmptyPlaceholder"',
            'Text="Không có Tầng"',
            'x:Name="FloorUnselectedPlaceholder"',
            'Text="Chưa chọn Tầng"',
            'Binding="{Binding HasItems, ElementName=ZoneCombo}" Value="False"',
            'Binding="{Binding SelectedIndex, ElementName=ZoneCombo}" Value="-1"',
            'Binding="{Binding HasItems, ElementName=FloorCombo}" Value="False"',
            'Binding="{Binding SelectedIndex, ElementName=FloorCombo}" Value="-1"',
            'x:Name="ModelTree" SelectedItemChanged="OnModelTreeSelectedItemChanged"',
            'Text="MÔ HÌNH"',
        ),
    )

    errors += require_tokens(
        "Workspace active-scope truth",
        view_model,
        (
            "if (_project == null) return -1;",
            "return zone == null ? -1 : Zones.IndexOf(zone.Name);",
            "return floor == null ? -1 : Floors.IndexOf(floor.Name);",
        ),
    )
    errors += forbid_tokens(
        "Workspace active-scope truth",
        view_model,
        (
            "return zone == null ? 0 : Math.Max(0, Zones.IndexOf(zone.Name));",
            "return floor == null ? 0 : Math.Max(0, Floors.IndexOf(floor.Name));",
            '"Không có Zone"',
            '"Không có Tầng"',
        ),
    )

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Workspace scope/theme preflight PASS: dark contrast, pure placeholders, and no-active selection truth are pinned.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
