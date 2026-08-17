#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
activation = read(ACTIVATION)
layout = read(LAYOUT)

# The explicit QS3D owner-facing command and BIM ribbon activation restore the coordinated BLT3D
# surface. The dedicated ShowWorkspace() helper itself remains isolated for callers that explicitly
# request only the Workspace palette.
for token in (
    "public static void Show() => ShowBimWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "_workspace.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
    "Mô hình + Thuộc tính QS3D bên trái",
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

for token in (
    "DispatcherPriority.SystemIdle",
    "ApplyBlt3dFiveZoneRuntimeLayout",
    "Grid.GetColumn(child) == 0",
    "Grid.GetColumn(child) == 2",
    "IsVisualDescendant(child, FamilyList)",
    "IsVisualDescendant(child, PropertyList)",
    "Grid.SetRow(modelPane, 0);",
    "Grid.SetRow(verticalSplitter, 1);",
    "Grid.SetRow(familyPropertiesPane, 2);",
    "verticalSplitter.ResizeDirection = GridResizeDirection.Rows;",
    "familyPropertiesPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);",
):
    if token not in layout:
        errors.append("left Model/Properties region contract missing: " + token)

if "public static void Show() => ShowWorkspace();" in palette:
    errors.append("regression: owner-facing QS3D activation must restore the coordinated BIM surface")

if "new Viewport" in layout or "Viewport3D" in layout:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D runtime five-zone regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: owner-facing QS3D/BIM activation restores the coordinated BLT3D surface while the dedicated ShowWorkspace helper remains isolated; Model + QS3D Properties stay distinct on the left and Management + Quantity stay on the right of native BricsCAD modelspace.")
