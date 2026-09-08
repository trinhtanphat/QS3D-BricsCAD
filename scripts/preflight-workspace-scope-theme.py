#!/usr/bin/env python3
"""Guard Workspace model-tree contrast and Zone/Floor empty-state contracts."""

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
        "Workspace scope empty state",
        scope,
        (
            'private const string EmptyZoneOption = "Không có Zone";',
            'private const string EmptyFloorOption = "Không có Tầng";',
            "DataContextChanged += OnWorkspaceScopeEmptyStateDataContextChanged;",
            "ZoneCombo.SelectionChanged += OnWorkspaceScopeZoneSelectionChanged;",
            "Zones.CollectionChanged += OnWorkspaceScopeCollectionChanged;",
            "Floors.CollectionChanged += OnWorkspaceScopeCollectionChanged;",
            "DispatcherPriority.DataBind",
            "NormalizeWorkspaceScopeCollection(",
            "EnsureWorkspaceScopeSelection(ZoneCombo, viewModel.ActiveZoneIndex());",
            "EnsureWorkspaceScopeSelection(FloorCombo, viewModel.ActiveFloorIndex());",
            "_loadingContext = true;",
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

    # The empty-state rows are presentation-only. They must not be persisted as project
    # Zone/Floor definitions or routed through the normal mutation handlers.
    if "ProjectZoneService" in scope or "ProjectFloorService" in scope:
        errors.append("Workspace scope empty-state partial must remain presentation-only")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("Workspace scope/theme preflight PASS: ModelTree dark contrast and Zone/Floor empty states are pinned.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
