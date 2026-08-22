#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
REPAIR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dRuntimeLayoutRepair.cs"
FIVE_ZONE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
PROPERTIES = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.DedicatedPropertiesPalette.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
activation = read(ACTIVATION)
repair = read(REPAIR)
five_zone = read(FIVE_ZONE)
properties = read(PROPERTIES)

for token in (
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "SetVisibility(workspace: true, right: true, quantityInsight: false);",
    "private static readonly Guid PropertiesGuid",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    "public static bool IsPropertiesVisible",
    "_workspace.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator embedded BIM contract missing: " + token)

bim_start = palette.find("public static bool ShowBimWorkspace()")
bim_end = palette.find("public static void ShowDrawingManagement()", bim_start)
bim = palette[bim_start:bim_end]
for token in (
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);",
    'ReportPaletteFailure("MÔ HÌNH BIM");',
    "return true;",
    "return false;",
):
    if token not in bim:
        errors.append("BIM palette success/default contract missing: " + token)
if "quantityInsight: true" in bim or "SetDedicatedPropertiesPaletteActive(true)" in bim:
    errors.append("default BIM reassert must not reopen optional dedicated Properties/Quantity palettes")

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "private const int BimSettleTicks = 2;",
    "_bimSettleTicksRemaining--",
    "private static bool ReassertBimWorkspace()",
    "StartCenterPaletteCoordinator.Hide();",
    "return PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation settle contract missing: " + token)

compact_activation = " ".join(activation.split())
if "if (ReassertBimWorkspace()) { _bimSettleTicksRemaining--; }" not in compact_activation:
    errors.append("same-tab settle retry must be consumed only after a successful BIM workspace reassert")
if "_bimSettleTicksRemaining = BimSettleTicks; ReassertBimWorkspace();" not in compact_activation:
    errors.append("initial BIM activation must preserve follow-up settle retries")

for token in (
    "Blt3dRuntimeSettlePasses = 2",
    "TimeSpan.FromMilliseconds(250)",
    "DispatcherPriority.ApplicationIdle",
    "ReassertBlt3dRuntimeLayout",
    "ApplyBlt3dFiveZoneRuntimeLayout();",
    "if (!IsLoaded)",
    "StopBlt3dRuntimeLayoutRepairTimer();",
    "FrameworkElement.UnloadedEvent",
    "OnBlt3dRuntimeLayoutUnloaded",
):
    if token not in repair:
        errors.append("WorkspacePanel runtime repair missing: " + token)

for token in (
    "_blt3dRuntimeColumnSplitter",
    "Grid.SetColumn(modelPane, 0);",
    "Grid.SetColumn(columnSplitter, 1);",
    "Grid.SetColumn(familyPane, 2);",
    "columnSplitter.ResizeDirection = GridResizeDirection.Columns;",
    "familyPane.RowDefinitions[0].Height = new GridLength(56, GridUnitType.Star);",
    "familyPane.RowDefinitions[2].Height = new GridLength(44, GridUnitType.Star);",
):
    if token not in five_zone:
        errors.append("owner side-by-side runtime layout missing: " + token)

for token in (
    "CreatePropertiesPaletteVisual",
    "SetDedicatedPropertiesPaletteActive(bool active)",
    "RestoreEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("optional dedicated Properties support missing: " + token)

if "Grid.SetRow(familyPane, 2);" in five_zone or "ResizeDirection = GridResizeDirection.Rows" in five_zone:
    errors.append("runtime settle must not restore the obsolete vertically stacked left workspace")
if "new Viewport" in repair or "Viewport3D" in repair or "new Viewport" in five_zone or "Viewport3D" in five_zone:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D BIM embedded runtime layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM activation/retry reasserts the integrated side-by-side Workspace plus Management around native BricsCAD modelspace without reopening optional dedicated Properties/Quantity surfaces.")