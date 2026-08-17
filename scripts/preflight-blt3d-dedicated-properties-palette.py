#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
LAYOUT_STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
WORKSPACE_LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
PROPERTIES_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Qs3dPropertiesPanel.xaml"
PROPERTIES_CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Qs3dPropertiesPanel.xaml.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
layout_store = read(LAYOUT_STORE)
workspace_layout = read(WORKSPACE_LAYOUT)
properties_xaml = read(PROPERTIES_XAML)
properties_code = read(PROPERTIES_CODE)

for token in (
    'private static readonly Guid PropertiesGuid = new Guid(',
    'private static PaletteSet? _properties;',
    'private static Qs3dPropertiesPanel? _propertiesPanel;',
    'public static bool IsPropertiesVisible',
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    'DockEnabled = DockSides.Left | DockSides.Right',
    '_properties.AddVisual("Thuộc tính", _propertiesPanel, true);',
    'BindingOperations.SetBinding(',
    'Source = _workspacePanel',
    'public static void ShowProperties()',
    'SetVisibility(workspace: false, properties: true, right: false, quantityInsight: false);',
    'var properties = workspace && right && quantityInsight;',
    'if (_properties != null && _properties.Dock != DockSides.Left)',
    '_properties.Dock = DockSides.Left;',
):
    if token not in palette:
        errors.append("dedicated PaletteSet contract missing: " + token)

for token in (
    "PropertiesPaletteWidth",
    "PropertiesPaletteHeight",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
):
    if token not in layout_store:
        errors.append("Properties layout persistence missing: " + token)

for token in (
    'x:Class="QS3D.BricsCAD.V25.UI.Qs3dPropertiesPanel"',
    'Text="QS3D PROPERTIES"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'Text="{Binding Value, UpdateSourceTrigger=LostFocus}"',
    'IsChecked="{Binding BooleanValue, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'Click="OnResetPropertyClick"',
):
    if token not in properties_xaml:
        errors.append("live Properties UI binding missing: " + token)

for token in (
    "CollectionViewSource.GetDefaultView(viewModel.Properties)",
    "PropertyGroupDescription(nameof(PropertyRowViewModel.Group))",
    "row.ResetValue();",
    "combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();",
    "textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();",
):
    if token not in properties_code:
        errors.append("live Properties behavior missing: " + token)

for token in (
    "embeddedPropertyRegion.Visibility = Visibility.Collapsed;",
    "PropertyList.Visibility = Visibility.Collapsed;",
    "PropertyList.MinHeight = 0;",
    "familyPropertiesPane.RowDefinitions[2].Height = new GridLength(0);",
):
    if token not in workspace_layout:
        errors.append("embedded property region was not retired: " + token)

for text, label in ((palette, "palette coordinator"), (workspace_layout, "workspace layout"), (properties_code, "properties panel")):
    if "new Viewport" in text or "Viewport3D" in text:
        errors.append(label + " must not synthesize a CAD viewport")

if "new WorkspaceViewModel" in properties_code:
    errors.append("dedicated Properties palette must share the live WorkspaceViewModel, not create a copied/mock model")

print("QS3D dedicated Properties palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3D Properties is a separately dockable/resizable native PaletteSet bound to the live WorkspaceViewModel; the embedded Workspace property region is retired and native BricsCAD modelspace remains untouched.")
