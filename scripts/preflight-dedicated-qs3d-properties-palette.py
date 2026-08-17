#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
PROPERTIES = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DedicatedPropertiesPalette.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
WORKSPACE_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"
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

for token in (
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "public static bool IsPropertiesVisible",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    'AddVisual("Thuộc tính", _propertiesVisual, true)',
    "_workspacePanel.DetachPropertiesPaletteVisual()",
    "_properties.Dock = DockSides.Left;",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
    "layout.PropertiesPaletteWidth",
    "layout.PropertiesPaletteHeight",
    "propertiesVisible = IsPropertiesVisible",
    "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
):
    if token not in palette:
        errors.append("PaletteCoordinator dedicated Properties contract missing: " + token)

for token in (
    "DetachPropertiesPaletteVisual",
    "PropertyList",
    "ownerGrid.Children.Remove(propertiesRegion);",
    "BindingOperations.SetBinding",
    "new Binding(nameof(DataContext))",
    "CollapseEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("real QS3D Properties reparenting missing: " + token)

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

if "IsVisualDescendant(child, PropertyList)" in layout:
    errors.append("regression: five-zone Workspace layout still treats Properties as embedded")
if "PropertyList.MinHeight" in layout:
    errors.append("regression: Workspace layout still sizes the detached Properties editor")
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

print("PASS: BIM mode owns a distinct QS3D Properties PaletteSet that reparents the existing project-aware PropertyList editor with its original WorkspaceViewModel, scope/search/typed-edit/reset behavior and deterministic persisted fallback sizing; native BricsCAD Properties is not used as a substitute.")
