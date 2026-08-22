#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
PROPERTIES = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DedicatedPropertiesPalette.cs"
COMPACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.CompactShell.cs"
STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
activation = read(ACTIVATION)
layout = read(LAYOUT)
properties = read(PROPERTIES)
compact = read(COMPACT)
store = read(STORE)

for token in (
    "public static void Show() => ShowBimWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "SetVisibility(workspace: true, right: true, quantityInsight: false);",
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "public static bool IsPropertiesVisible",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    "CreatePropertiesPaletteVisual()",
    "_workspace.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator owner-reference contract missing: " + token)

bim_start = palette.find("public static bool ShowBimWorkspace()")
bim_end = palette.find("public static void ShowDrawingManagement()", bim_start)
bim = palette[bim_start:bim_end]
if "SetDedicatedPropertiesPaletteActive(false)" not in bim:
    errors.append("default BIM must keep the real Properties editor embedded")
if "SetDedicatedPropertiesPaletteActive(true)" in bim:
    errors.append("default BIM must not activate the dedicated Properties host")
if "quantityInsight: true" in bim:
    errors.append("default BIM must not auto-open Quantity Insight")

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation contract missing: " + token)

for token in (
    "CreatePropertiesPaletteVisual",
    "SetDedicatedPropertiesPaletteActive(bool active)",
    "ownerGrid.Children.Remove(region);",
    "host.Children.Add(region);",
    "host.Children.Remove(region);",
    "ownerGrid.Children.Add(region);",
    "BindingOperations.SetBinding",
    "RestoreEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("optional dedicated Properties capability missing: " + token)

for token in (
    "PropertiesPaletteWidth",
    "PropertiesPaletteHeight",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
):
    if token not in store:
        errors.append("dedicated Properties persistence missing: " + token)

if "static WorkspacePanel()" not in compact:
    errors.append("WorkspacePanel deterministic type initializer missing from CompactShell")
if "static WorkspacePanel()" in layout or "static WorkspacePanel()" in properties:
    errors.append("BLT3D partials must not declare duplicate WorkspacePanel static constructors")

for token in (
    "DispatcherPriority.SystemIdle",
    "ApplyBlt3dFiveZoneRuntimeLayout",
    "_blt3dRuntimeColumnSplitter",
    "columns[0].Width = new GridLength(38, GridUnitType.Star);",
    "columns[1].Width = new GridLength(4);",
    "columns[2].Width = new GridLength(62, GridUnitType.Star);",
    "Grid.SetColumn(modelPane, 0);",
    "Grid.SetColumn(columnSplitter, 1);",
    "Grid.SetColumn(familyPane, 2);",
    "columnSplitter.ResizeDirection = GridResizeDirection.Columns;",
    "familyPane.RowDefinitions[0].Height = new GridLength(56, GridUnitType.Star);",
    "familyPane.RowDefinitions[2].Height = new GridLength(44, GridUnitType.Star);",
    "PropertyList.MinHeight = 120;",
):
    if token not in layout:
        errors.append("side-by-side Model/Family/Properties runtime layout missing: " + token)

if "Grid.SetRow(familyPane, 2);" in layout or "ResizeDirection = GridResizeDirection.Rows" in layout:
    errors.append("regression: owner Workspace must not stack Model above Family/Properties")
if "new Viewport" in layout or "Viewport3D" in layout:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D embedded BIM workspace regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM activation keeps BricsCAD modelspace host-owned, restores side-by-side Model and Family/embedded-Properties columns on the left plus Management on the right, while dedicated Properties/Quantity stay opt-in capabilities.")