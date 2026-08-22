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

    # All Workspace ComboBoxes must resolve the repository-owned implicit style from
    # the nearest resource boundary; the screenshot-visible Zone/Floor controls are
    # additionally pinned locally so a style resolved earlier during palette load cannot stick.
    require(bridge, "TryFindResource(typeof(ComboBox)) is Style comboStyle", "ComboBox style lookup")
    require(bridge, "Resources[typeof(ComboBox)] = comboStyle;", "Workspace ComboBox style publication")
    require(bridge, "PinScopeComboStyle(ZoneCombo, comboStyle);", "Zone dark style pin")
    require(bridge, "PinScopeComboStyle(FloorCombo, comboStyle);", "Floor dark style pin")
    require(bridge, 'SetResourceReference(Control.BackgroundProperty, "BgInputBrush")', "ComboBox dark background")
    require(bridge, 'SetResourceReference(Control.ForegroundProperty, "TextBrush")', "ComboBox dark foreground")
    require(bridge, 'SetResourceReference(Control.BorderBrushProperty, "BorderStrongBrush")', "ComboBox dark border")

    # Stock WPF TreeViewItem/ListBoxItem/ListViewItem templates can resolve these
    # SystemColors resources. The guard must shadow them at WorkspacePanel.Resources,
    # not only on ModelTree, so every collection surface inherits dark active/inactive selection.
    require(bridge, "PinWorkspaceSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);", "active selection background")
    require(bridge, "PinWorkspaceSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);", "inactive selection background")
    require(bridge, "PinWorkspaceSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);", "active selection foreground")
    require(bridge, "PinWorkspaceSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);", "inactive selection foreground")
    require(bridge, "Resources[resourceKey] = brush;", "Workspace selection resource boundary")
    require(bridge, "ModelTree.Resources[resourceKey] = brush;", "reported ModelTree immediate pin")
    require(bridge, 'TryFindResource("BgSelectedBrush") is Brush selectionBrush', "QS3D selected brush lookup")
    require(bridge, 'TryFindResource("TextBrush") is Brush selectionTextBrush', "QS3D selected text lookup")

    # Keep the bridge tied to the current canonical theme and every Workspace collection
    # family that shares the selection-resource boundary.
    require(theme, '<Style TargetType="{x:Type ComboBox}">', "canonical ComboBox style")
    require(theme, '<ControlTemplate TargetType="{x:Type ComboBox}">', "host-independent ComboBox template")
    require(theme, '<SolidColorBrush x:Key="BgSelectedBrush"', "canonical selected brush")
    require(theme, '<Style TargetType="{x:Type TreeViewItem}">', "TreeViewItem style contract")
    require(theme, '<Style TargetType="{x:Type ListBoxItem}">', "ListBoxItem style contract")
    require(theme, '<Style TargetType="{x:Type ListViewItem}">', "ListViewItem style contract")

    require(workspace, '<ResourceDictionary Source="Theme.xaml"/>', "Workspace theme merge")
    require(workspace, 'x:Name="ZoneCombo"', "Zone control contract")
    require(workspace, 'x:Name="FloorCombo"', "Floor control contract")
    require(workspace, 'x:Name="ModelTree"', "Model tree contract")
    require(workspace, 'x:Name="FamilyList"', "Family list contract")
    require(workspace, 'x:Name="PropertyList"', "Property list contract")
    require(workspace, 'x:Name="InspectionList"', "Inspection list contract")

    if workspace.count("<TreeView") < 2:
        raise SystemExit("FAIL: room-finish TreeView coverage contract: expected both Workspace TreeViews")

    print("PASS: V25 Workspace dark host-selection coverage contract")


if __name__ == "__main__":
    main()
