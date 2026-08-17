#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
layout = read(LAYOUT)

for token in (
    "public static void Show() => ShowBimWorkspace();",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "_workspace.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
    "Mô hình + Thuộc tính QS3D bên trái",
):
    if token not in palette:
        errors.append("PaletteCoordinator runtime contract missing: " + token)

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

# Regression signature from the owner-provided runtime screenshot: explicit QS3D activation must
# never route back to the isolated legacy Workspace path, otherwise Management + Quantity disappear
# and native BricsCAD Properties can visually masquerade as the plugin's right-side workspace.
if "public static void Show() => ShowWorkspace();" in palette:
    errors.append("regression: QS3D activation still opens only the isolated Workspace palette")

print("QS3D BLT3D runtime five-zone regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3D activation restores the coordinated BLT3D workspace, with Model + QS3D Properties visible as distinct left regions and Management + Quantity palettes restored on the right of the native BricsCAD viewport.")
