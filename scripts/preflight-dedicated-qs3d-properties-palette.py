#!/usr/bin/env python3
from pathlib import Path
import sys

# Keep this guard on the clean #2399 carrier so exact-head CI validates the scoped palette contract.
ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
PROPERTIES = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DedicatedPropertiesPalette.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
WORKSPACE_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
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
selection = read(SELECTION)

for token in (
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "public static bool IsPropertiesVisible",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    'AddVisual("Thuộc tính", _propertiesVisual, true)',
    "_workspacePanel.CreatePropertiesPaletteVisual()",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(true);",
    "_properties.Dock = DockSides.Left;",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
    "layout.PropertiesPaletteWidth",
    "layout.PropertiesPaletteHeight",
    "propertiesVisible = IsPropertiesVisible",
    "bimSurfaceActive = workspaceVisible && rightVisible && quantityVisible",
    "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
):
    if token not in palette:
        errors.append("PaletteCoordinator dedicated Properties contract missing: " + token)

# The exact existing editor visual moves between Workspace and the dedicated palette. This preserves
# ordinary ShowWorkspace behavior while BIM gets a distinct native plugin palette without cloning
# ViewModel state or edit handlers.
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

# Lock the real editor semantics. A generic reflection-only inspector must not substitute for the
# existing project-aware QS3D property editor with scope, search, typed editors and override reset.
for token in (
    'x:Name="PropertyList"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'x:Name="PropertySearch"',
    'Click="OnResetPropertyClick"',
    'Value="Boolean"',
    'Value="Choice"',
):
    if token not in workspace_xaml:
        errors.append("real QS3D Properties editor contract missing from WorkspacePanel.xaml: " + token)

for token in (
    "PropertiesPaletteWidth",
    "PropertiesPaletteHeight",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
):
    if token not in store:
        errors.append("dedicated Properties layout persistence missing: " + token)

for token in (
    "if (_dedicatedPropertiesPaletteActive)",
    "familyPane.RowDefinitions[2].Height = new GridLength(0);",
    "PropertyList.MinHeight = 0;",
    "familyPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);",
    "PropertyList.MinHeight = 120;",
):
    if token not in layout:
        errors.append("dynamic embedded/dedicated layout contract missing: " + token)

if "DedicatedPropertiesPaletteCoordinator.SyncVisibility();" in selection:
    errors.append("selection changes must not reopen a manually closed Properties palette")
if "DedicatedPropertiesPaletteCoordinator.SetInspection(snapshots);" in selection:
    errors.append("selection must not maintain a duplicate reflection inspector state")
if "DedicatedPropertiesPanel" in palette or "QS3D plugin inspector" in palette:
    errors.append("regression: dedicated palette must host the real QS3D editor, not a reflection inspector")
if "new Viewport" in properties or "Viewport3D" in properties:
    errors.append("dedicated Properties palette must not create or replace native BricsCAD modelspace")

print("QS3D dedicated real Properties palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM mode owns a distinct QS3D Properties PaletteSet by dynamically reparenting the existing project-aware PropertyList editor with its original WorkspaceViewModel/scope/search/typed-edit/reset behavior; ordinary ShowWorkspace restores the same editor in-place, selection changes respect manual close, and native BricsCAD Properties is not used as a substitute.")
