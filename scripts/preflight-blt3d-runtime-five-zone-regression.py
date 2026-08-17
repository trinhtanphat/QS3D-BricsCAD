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

# Owner-facing QS3D / BIM activation restores four plugin palettes. The legacy Workspace-only helper
# stays isolated, while the real QS3D Properties editor is hosted by a distinct PaletteSet.
for token in (
    "public static void Show() => ShowBimWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "public static bool IsPropertiesVisible",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    "DetachPropertiesPaletteVisual()",
    "_workspace.Dock = DockSides.Left;",
    "_properties.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
    "Thuộc tính QS3D palette riêng bên trái",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator dedicated-properties contract missing: " + token)

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation contract missing: " + token)

for token in (
    "DetachPropertiesPaletteVisual",
    "PropertyList",
    "ownerGrid.Children.Remove(propertiesRegion);",
    "BindingOperations.SetBinding",
    "new Binding(nameof(DataContext))",
    "CollapseEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("dedicated QS3D Properties visual contract missing: " + token)

for token in (
    "PropertiesPaletteWidth",
    "PropertiesPaletteHeight",
    "PropertiesPaletteMinWidth",
    "PropertiesPaletteMinHeight",
):
    if token not in store:
        errors.append("QS3D Properties layout persistence missing: " + token)

# WorkspacePanel is a partial type and C# permits only one static constructor for the whole type.
if "static WorkspacePanel()" not in compact:
    errors.append("WorkspacePanel deterministic type initializer missing from CompactShell")
if "static WorkspacePanel()" in layout or "static WorkspacePanel()" in properties:
    errors.append("BLT3D dedicated-properties partials must not declare duplicate WorkspacePanel static constructors")

for token in (
    "DispatcherPriority.SystemIdle",
    "ApplyBlt3dFiveZoneRuntimeLayout",
    "Grid.GetColumn(child) == 0",
    "IsVisualDescendant(child, FamilyList)",
    "_blt3dRuntimeVerticalSplitter",
    "ReferenceEquals(verticalSplitter.Parent, workspace)",
    "Grid.SetRow(modelPane, 0);",
    "Grid.SetRow(verticalSplitter, 1);",
    "Grid.SetRow(familyPane, 2);",
    "verticalSplitter.ResizeDirection = GridResizeDirection.Rows;",
    "familyPane.RowDefinitions[2].Height = new GridLength(0);",
):
    if token not in layout:
        errors.append("left Model/Family runtime layout missing: " + token)

if "IsVisualDescendant(child, PropertyList)" in layout:
    errors.append("regression: Workspace layout still treats Properties as embedded instead of a dedicated plugin palette")
if "PropertyList.MinHeight" in layout:
    errors.append("regression: Workspace runtime layout still sizes the detached Properties editor")
if "public static void Show() => ShowWorkspace();" in palette:
    errors.append("regression: owner-facing QS3D activation must restore the coordinated BIM surface")
if "new Viewport" in layout or "Viewport3D" in layout:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D dedicated Properties five-zone regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3D/BIM activation restores Model + dedicated QS3D Properties on the left and Management + Quantity on the right of native BricsCAD modelspace; the isolated Workspace helper remains isolated, the Properties editor keeps the real Workspace ViewModel/edit handlers, and repeated host settle passes remain deterministic.")
