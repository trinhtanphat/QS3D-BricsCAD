#!/usr/bin/env python3
"""Guard the V25 Workspace against bright BricsCAD/WPF host selection chrome."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BRIDGE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DarkHostTheme.cs"
THEME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Theme.xaml"
WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label}: missing {needle!r}")


def main() -> None:
    bridge = read(BRIDGE)
    theme = read(THEME)
    workspace = read(WORKSPACE)

    # The bridge must explicitly pin the two screenshot-visible scope controls to
    # the repository-owned ComboBox style instead of relying on host implicit style lookup.
    require(bridge, "TryFindResource(typeof(ComboBox)) is Style comboStyle", "ComboBox style lookup")
    require(bridge, "PinScopeComboStyle(ZoneCombo, comboStyle);", "Zone dark style pin")
    require(bridge, "PinScopeComboStyle(FloorCombo, comboStyle);", "Floor dark style pin")
    require(bridge, 'SetResourceReference(Control.BackgroundProperty, "BgInputBrush")', "ComboBox dark background")
    require(bridge, 'SetResourceReference(Control.ForegroundProperty, "TextBrush")', "ComboBox dark foreground")
    require(bridge, 'SetResourceReference(Control.BorderBrushProperty, "BorderStrongBrush")', "ComboBox dark border")

    # The stock WPF TreeViewItem template resolves these system keys for active and
    # inactive selection. Both pairs must be shadowed at ModelTree, otherwise BricsCAD
    # can inject a bright highlight when the palette focus/selection state changes.
    require(bridge, "ModelTree.Resources[SystemColors.HighlightBrushKey]", "active tree selection background")
    require(bridge, "ModelTree.Resources[SystemColors.InactiveSelectionHighlightBrushKey]", "inactive tree selection background")
    require(bridge, "ModelTree.Resources[SystemColors.HighlightTextBrushKey]", "active tree selection foreground")
    require(bridge, "ModelTree.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey]", "inactive tree selection foreground")
    require(bridge, 'TryFindResource("BgSelectedBrush") is Brush selectionBrush', "QS3D selected brush lookup")
    require(bridge, 'TryFindResource("TextBrush") is Brush selectionTextBrush', "QS3D selected text lookup")

    # Keep the bridge tied to the existing canonical XAML theme and named controls.
    require(theme, '<Style TargetType="{x:Type ComboBox}">', "canonical ComboBox style")
    require(theme, '<ControlTemplate TargetType="{x:Type ComboBox}">', "host-independent ComboBox template")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(workspace, '<ResourceDictionary Source="Theme.xaml"/>', "Workspace theme merge")
    require(workspace, 'x:Name="ZoneCombo"', "Zone control contract")
    require(workspace, 'x:Name="FloorCombo"', "Floor control contract")
    require(workspace, 'x:Name="ModelTree"', "Model tree contract")

    print("PASS: V25 Workspace dark host-selection contract")


if __name__ == "__main__":
    main()
