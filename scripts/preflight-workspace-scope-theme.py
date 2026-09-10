#!/usr/bin/env python3
"""Guard Workspace model-tree contrast and truthful Zone/Floor scope presentation."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
DARK_THEME = UI / "WorkspacePanel.DarkHostTheme.cs"
SCOPE = UI / "WorkspacePanel.ScopeDropdownHostInteraction.cs"
XAML = UI / "WorkspacePanel.xaml"


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

    errors += require_tokens(
        "Workspace scope presentation",
        scope,
        (
            "WireWorkspaceScopeCombo(ZoneCombo);",
            "WireWorkspaceScopeCombo(FloorCombo);",
            "!combo.HasItems",
            '"Không có Zone"',
            '"Chưa chọn Zone"',
            '"Không có Tầng"',
            '"Chưa chọn Tầng"',
            "WrapWorkspaceScopeCombo(",
            "ItemContainerGenerator.ItemsChanged += OnWorkspaceScopeItemsChanged;",
            "IsHitTestVisible = false",
            "UpdateWorkspaceScopePlaceholders();",
        ),
    )

    # Programmatic Refresh/Clear must never visually manufacture an active first item when
    # ActiveZoneId/ActiveFloorId are absent or stale. User-driven changes are deliberately
    # left alone because _loadingContext is false for a real click.
    errors += require_tokens(
        "Workspace active-scope truth",
        scope,
        (
            "NormalizeWorkspaceProgrammaticScopeSelection(ZoneCombo, isZone: true);",
            "NormalizeWorkspaceProgrammaticScopeSelection(FloorCombo, isZone: false);",
            "if (!_loadingContext)",
            "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
            "string.IsNullOrWhiteSpace(project.ActiveZoneId)",
            "string.IsNullOrWhiteSpace(project.ActiveFloorId)",
            "return zone == null ? -1 : _viewModel.Zones.IndexOf(zone.Name);",
            "return floor == null ? -1 : _viewModel.Floors.IndexOf(floor.Name);",
            "combo.SelectedIndex = expectedIndex;",
        ),
    )

    # Presentation text must never become a fake row in the bound data collections.
    errors += forbid_tokens(
        "Workspace scope presentation",
        scope,
        (
            "EmptyZoneOption",
            "EmptyFloorOption",
            "NormalizeWorkspaceScopeCollection",
            "ObservableCollection<string>",
            "items.Add(emptyLabel)",
            "Zones.Add(EmptyZoneOption)",
            "Floors.Add(EmptyFloorOption)",
            "DispatcherPriority.DataBind",
        ),
    )

    errors += require_tokens(
        "Workspace scope bindings",
        xaml,
        (
            'x:Name="ZoneCombo" ItemsSource="{Binding Zones}"',
            'x:Name="FloorCombo" ItemsSource="{Binding Floors}"',
            'x:Name="ModelTree" SelectedItemChanged="OnModelTreeSelectedItemChanged"',
            'Text="MÔ HÌNH"',
        ),
    )

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Workspace scope/theme preflight PASS: dark contrast, pure placeholders, and active-scope truth are pinned.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
