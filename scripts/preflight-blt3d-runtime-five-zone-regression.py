#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
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
compact = read(COMPACT)

# #2396 is specifically the BIM-tab host-settle repair. Keep the ordinary QS3D/Workspace entry
# point isolated; the separate #2399 follow-up owns any owner-facing activation change and a
# distinct QS3D Properties plugin region/palette.
for token in (
    "public static void Show() => ShowWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "_workspace.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
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
# CompactShell owns that constructor; its presence removes beforefieldinit for every partial file,
# making the BLT3D layout/repair static registrations run before the first panel instance.
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
    "familyPropertiesPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);",
):
    if token not in layout:
        errors.append("left Model/Properties region contract missing: " + token)

# ApplyBlt3dFiveZoneRuntimeLayout is intentionally called repeatedly during the bounded host-docking
# settle window. After pass 1, Family/Properties has already moved from column 2 to column 0; tying
# rediscovery to the original column would make every later reassert a silent no-op.
if "Grid.GetColumn(child) == 2" in layout:
    errors.append("runtime reassert must rediscover Family/Properties independently of its original column")

if "public static void Show() => ShowBimWorkspace();" in palette:
    errors.append("#2396 must not absorb #2399 owner-facing QS3D activation semantics")

if "new Viewport" in layout or "Viewport3D" in layout:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D runtime five-zone regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM-tab activation restores the coordinated BLT3D settle layout while ordinary QS3D/Workspace activation remains isolated for #2396; first-load class handlers are registered deterministically through the existing WorkspacePanel type initializer, repeated settle passes remain idempotent after the Family/Properties pane moves, and native BricsCAD modelspace remains the center viewport. Dedicated owner-facing QS3D activation / Properties-palette work remains #2399.")
