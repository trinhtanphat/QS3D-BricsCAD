#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ScopePresentation.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
VIEW_MODEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ViewModels" / "WorkspaceViewModel.cs"


def fail(message: str) -> None:
    print("ERROR: Workspace scope/tab theme preflight failed: " + message)
    raise SystemExit(1)


def require(text: str, marker: str, scope: str) -> None:
    if marker not in text:
        fail(scope + " is missing required marker: " + marker)


def main() -> None:
    for path in (PRESENTATION, XAML, VIEW_MODEL):
        if not path.is_file():
            fail("missing source file " + str(path.relative_to(ROOT)))

    presentation = PRESENTATION.read_text(encoding="utf-8")
    xaml = XAML.read_text(encoding="utf-8")
    view_model = VIEW_MODEL.read_text(encoding="utf-8")

    # Runtime-created TabItems must not inherit host/system light chrome.
    for marker in (
        "ApplyWorkspaceBrowserTabStyle",
        "new Style(typeof(TabItem))",
        "new ControlTemplate(typeof(TabItem))",
        "new FrameworkElementFactory(typeof(Border))",
        'TryFindResource("TextBrush")',
        'TryFindResource("Bg2Brush")',
        'TryFindResource("Bg1Brush")',
        'TryFindResource("BgHoverBrush")',
        'TryFindResource("BorderStrongBrush")',
        'TryFindResource("AccentBrush")',
        "TabItem.IsSelectedProperty",
        "UIElement.IsMouseOverProperty",
        "item.Style = style",
    ):
        require(presentation, marker, "host-independent Project Browser tab chrome")

    # Empty scope copy is presentation-only and follows actual collection changes.
    for marker in (
        "INotifyCollectionChanged",
        "CollectionChanged += OnWorkspaceScopeCollectionChanged",
        "CollectionChanged -= OnWorkspaceScopeCollectionChanged",
        'ApplyWorkspaceScopeComboState(ZoneCombo, "Không có Zone")',
        'ApplyWorkspaceScopeComboState(FloorCombo, "Không có Tầng")',
        "combo.Items.Count == 0",
        "combo.IsEditable = true",
        "combo.IsReadOnly = true",
        "combo.Text = emptyText",
        "combo.IsHitTestVisible = false",
        "combo.IsEditable = false",
        "combo.IsReadOnly = false",
        "combo.IsHitTestVisible = true",
    ):
        require(presentation, marker, "Zone/Floor empty-state presentation")

    # Keep the authoritative XAML bindings and mutation handlers untouched.
    for marker in (
        'x:Name="ZoneCombo" ItemsSource="{Binding Zones}"',
        'x:Name="FloorCombo" ItemsSource="{Binding Floors}"',
        'SelectionChanged="OnZoneChanged"',
        'SelectionChanged="OnFloorChanged"',
    ):
        require(xaml, marker, "authoritative workspace scope binding")

    for forbidden in ("NoZoneOption", "NoFloorOption", "Không có Zone", "Không có Tầng"):
        if forbidden in view_model:
            fail("presentation-only no-data copy leaked into WorkspaceViewModel/domain-facing collections: " + forbidden)

    print("PASS: Workspace Project Browser tabs use dark host-independent chrome and empty Zone/Floor scopes expose non-mutating no-data states")


if __name__ == "__main__":
    main()
