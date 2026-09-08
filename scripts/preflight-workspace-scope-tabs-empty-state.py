#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
BROWSER = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ProjectBrowser.cs"
VIEW_MODEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ViewModels" / "WorkspaceViewModel.cs"


def fail(message: str) -> None:
    print("ERROR: Workspace scope/tab theme preflight failed: " + message)
    raise SystemExit(1)


def require(text: str, marker: str, scope: str) -> None:
    if marker not in text:
        fail(scope + " is missing required marker: " + marker)


def require_order(text: str, markers: tuple[str, ...], scope: str) -> None:
    cursor = -1
    for marker in markers:
        position = text.find(marker, cursor + 1)
        if position < 0:
            fail(scope + " is missing ordered marker: " + marker)
        if position <= cursor:
            fail(scope + " has invalid marker order around: " + marker)
        cursor = position


def main() -> None:
    for path in (XAML, BROWSER, VIEW_MODEL):
        if not path.is_file():
            fail("missing source file " + str(path.relative_to(ROOT)))

    xaml = XAML.read_text(encoding="utf-8")
    browser = BROWSER.read_text(encoding="utf-8")
    view_model = VIEW_MODEL.read_text(encoding="utf-8")

    # The runtime-created Project Browser tabs must opt into host-independent dark chrome.
    require(xaml, 'x:Key="WorkspaceBrowserTabItem"', "Workspace-local tab style")
    for marker in (
        'TargetType="{x:Type TabItem}"',
        'Foreground" Value="{StaticResource TextBrush}"',
        'Background" Value="{StaticResource Bg2Brush}"',
        'BorderBrush" Value="{StaticResource BorderStrongBrush}"',
        '<ControlTemplate TargetType="{x:Type TabItem}">',
        '<Trigger Property="IsSelected" Value="True">',
        '<Trigger Property="IsMouseOver" Value="True">',
    ):
        require(xaml, marker, "Workspace-local tab style")

    require_order(
        browser,
        (
            "var tabs = new TabControl",
            'ItemContainerStyle = TryFindResource("WorkspaceBrowserTabItem") as Style,',
            'new TabItem { Header = "Mô hình", Content = ModelTree }',
            'new TabItem { Header = "Project Browser", Content = CreateProjectBrowserSurface() }',
        ),
        "Project Browser tab-style binding",
    )

    # Empty scope collections must remain real empty collections while the UI communicates no-data.
    for collection, text in (("Zones", "Không có Zone"), ("Floors", "Không có Tầng")):
        require(xaml, f'Binding="{{Binding {collection}.Count}}" Value="0"', f"{collection} empty-state trigger")
        require(xaml, f'Text="{text}"', f"{collection} empty-state copy")

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
