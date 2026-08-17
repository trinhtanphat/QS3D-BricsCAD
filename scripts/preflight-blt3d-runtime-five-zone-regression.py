#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
PROPERTIES_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Qs3dPropertiesPanel.xaml"
PROPERTIES_CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Qs3dPropertiesPanel.xaml.cs"
COMPACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.CompactShell.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
activation = read(ACTIVATION)
layout = read(LAYOUT)
properties_xaml = read(PROPERTIES_XAML)
properties_code = read(PROPERTIES_CODE)
compact = read(COMPACT)

# The explicit QS3D owner-facing command and BIM ribbon activation restore the coordinated BLT3D
# surface. The dedicated ShowWorkspace() helper itself remains isolated; full BIM mode also exposes
# the separate Properties palette through the three-argument compatibility visibility helper.
for token in (
    "public static void Show() => ShowBimWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "public static void ShowProperties()",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "var properties = workspace && right && quantityInsight;",
    "_workspace.Dock = DockSides.Left;",
    "_properties.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
    "Mô hình + Family + Thuộc tính QS3D tách riêng bên trái",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator runtime contract missing: " + token)

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation contract missing: " + token)

# WorkspacePanel is a partial type and C# permits only one static constructor for the whole type.
if "static WorkspacePanel()" not in compact:
    errors.append("WorkspacePanel deterministic type initializer missing from CompactShell")
if "static WorkspacePanel()" in layout:
    errors.append("BLT3D runtime layout must not declare a duplicate WorkspacePanel static constructor")

for token in (
    "DispatcherPriority.SystemIdle",
    "ApplyBlt3dFiveZoneRuntimeLayout",
    "Grid.GetColumn(child) == 0",
    "IsVisualDescendant(child, FamilyList)",
    "IsVisualDescendant(child, PropertyList)",
    "_blt3dRuntimeVerticalSplitter",
    "ReferenceEquals(verticalSplitter.Parent, workspace)",
    "Grid.SetRow(modelPane, 0);",
    "Grid.SetRow(verticalSplitter, 1);",
    "Grid.SetRow(familyPropertiesPane, 2);",
    "verticalSplitter.ResizeDirection = GridResizeDirection.Rows;",
    "embeddedPropertyRegion.Visibility = Visibility.Collapsed;",
    "PropertyList.Visibility = Visibility.Collapsed;",
    "PropertyList.MinHeight = 0;",
    "familyPropertiesPane.RowDefinitions[2].Height = new GridLength(0);",
):
    if token not in layout:
        errors.append("left Model/Family plus detached Properties contract missing: " + token)

for token in (
    'x:Class="QS3D.BricsCAD.V25.UI.Qs3dPropertiesPanel"',
    'Text="QS3D PROPERTIES"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'Click="OnResetPropertyClick"',
):
    if token not in properties_xaml:
        errors.append("dedicated Properties XAML contract missing: " + token)

for token in (
    "public partial class Qs3dPropertiesPanel : UserControl",
    "CollectionViewSource.GetDefaultView(viewModel.Properties)",
    "row.ResetValue();",
    "MatchesPropertyToken",
):
    if token not in properties_code:
        errors.append("dedicated Properties behavior missing: " + token)

# ApplyBlt3dFiveZoneRuntimeLayout is called repeatedly during host-docking settle. After pass 1,
# Family has already moved from column 2 to column 0, so rediscovery must not depend on column 2.
if "Grid.GetColumn(child) == 2" in layout:
    errors.append("runtime reassert must rediscover Family independently of its original column")

if "public static void Show() => ShowWorkspace();" in palette:
    errors.append("regression: owner-facing QS3D activation must restore the coordinated BIM surface")

for text, label in ((layout, "runtime layout"), (properties_code, "properties panel")):
    if "new Viewport" in text or "Viewport3D" in text:
        errors.append(label + " must not create a fake second 3D viewport")

print("QS3D BLT3D runtime six-region regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: owner-facing QS3D/BIM activation restores Model/Family and a separately dockable live QS3D Properties palette on the left, Management + Quantity on the right, and preserves native BricsCAD modelspace without a fake viewport.")
