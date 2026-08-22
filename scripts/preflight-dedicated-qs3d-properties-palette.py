#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
PROPERTIES = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DedicatedPropertiesPalette.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
WORKSPACE_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
WORKSPACE_CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml.cs"
SELECTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
properties = read(PROPERTIES)
layout = read(LAYOUT)
store = read(STORE)
workspace_xaml = read(WORKSPACE_XAML)
workspace_code = read(WORKSPACE_CODE)
selection = read(SELECTION)

for token in (
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "public static bool IsPropertiesVisible",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    '_properties.AddVisual("Thuộc tính", _propertiesVisual, true);',
    "_workspacePanel.CreatePropertiesPaletteVisual()",
    "_properties.Dock = DockSides.Left;",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
    "layout.PropertiesPaletteWidth",
    "layout.PropertiesPaletteHeight",
    "propertiesVisible = IsPropertiesVisible",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(propertiesVisible);",
    "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
):
    if token not in palette:
        errors.append("PaletteCoordinator optional dedicated Properties contract missing: " + token)

bim_start = palette.find("public static bool ShowBimWorkspace()")
bim_end = palette.find("public static void ShowDrawingManagement()", bim_start)
bim = palette[bim_start:bim_end]
if "SetDedicatedPropertiesPaletteActive(false)" not in bim:
    errors.append("default BIM must keep authoritative Properties embedded")
if "SetDedicatedPropertiesPaletteActive(true)" in bim:
    errors.append("default BIM must not auto-open/reparent into dedicated Properties")

for token in (
    "EnsurePaletteSize(",
    "new WpfSize(layout.PropertiesPaletteWidth, layout.PropertiesPaletteHeight)",
    "UserUiLayoutStore.PropertiesPaletteMinWidth",
    "UserUiLayoutStore.PropertiesPaletteMinHeight",
    "TryGetPersistableSize",
    "hasPropertiesSize",
):
    if token not in palette:
        errors.append("dedicated Properties size/persistence capability missing: " + token)

for token in (
    "CreatePropertiesPaletteVisual",
    "SetDedicatedPropertiesPaletteActive(bool active)",
    "ownerGrid.Children.Remove(region);",
    "host.Children.Add(region);",
    "host.Children.Remove(region);",
    "ownerGrid.Children.Add(region);",
    "BindingOperations.SetBinding",
    "new Binding(nameof(DataContext))",
    "CollapseEmbeddedPropertiesSlot",
    "RestoreEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("dynamic real QS3D Properties reparenting missing: " + token)

for token in (
    'x:Name="PropertyList"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'x:Name="PropertySearch"',
    'Text="{Binding Value, UpdateSourceTrigger=LostFocus}"',
    'IsChecked="{Binding BooleanValue, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'IsEnabled="{Binding IsEditable}"',
    'Click="OnResetPropertyClick"',
):
    if token not in workspace_xaml:
        errors.append("real editable QS3D Properties editor contract missing: " + token)

for token in ("OnResetPropertyClick", "row.ResetValue();"):
    if token not in workspace_code:
        errors.append("real QS3D Properties reset/write-back handler missing: " + token)

for token in ("PropertiesPaletteWidth", "PropertiesPaletteHeight", "PropertiesPaletteMinWidth", "PropertiesPaletteMinHeight"):
    if token not in store:
        errors.append("dedicated Properties layout persistence missing: " + token)

for token in (
    "if (_dedicatedPropertiesPaletteActive)",
    "familyPane.RowDefinitions[2].Height = new GridLength(0);",
    "PropertyList.MinHeight = 0;",
    "familyPane.RowDefinitions[2].Height = new GridLength(44, GridUnitType.Star);",
    "PropertyList.MinHeight = 120;",
):
    if token not in layout:
        errors.append("embedded/optional-dedicated layout contract missing: " + token)

if "DedicatedPropertiesPaletteCoordinator.SyncVisibility();" in selection:
    errors.append("selection changes must not reopen a manually closed Properties palette")
if "new WorkspaceViewModel" in properties:
    errors.append("dedicated host must not create a second WorkspaceViewModel")
if "new Viewport" in properties or "Viewport3D" in properties:
    errors.append("dedicated Properties must not create/replace BricsCAD modelspace")

print("QS3D optional dedicated real Properties palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the dedicated QS3D Properties PaletteSet remains an optional single-editor host with persisted sizing, while default BIM keeps the same authoritative editor embedded under Family to match the owner BLT3D reference.")