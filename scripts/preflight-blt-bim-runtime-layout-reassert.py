#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
REPAIR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dRuntimeLayoutRepair.cs"
REFERENCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferencePaletteLayout.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
activation = read(ACTIVATION)
repair = read(REPAIR)
reference = read(REFERENCE)

for token in (
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator contract missing: " + token)

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "private const int BimSettleTicks = 2;",
    "_bimSettleTicksRemaining--",
    "ReassertBimWorkspace();",
    "StartCenterPaletteCoordinator.Hide();",
    "PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation settle contract missing: " + token)

if "string.Equals(currentId, _lastTabId" in activation and "_bimSettleTicksRemaining > 0" not in activation:
    errors.append("same-tab polling still returns without a bounded BricsCAD dock-settle repair")

for token in (
    "Blt3dRuntimeSettlePasses = 2",
    "TimeSpan.FromMilliseconds(250)",
    "DispatcherPriority.ApplicationIdle",
    "ReassertBlt3dRuntimeLayout",
    "ApplyReferencePaletteLayout();",
    "if (!IsLoaded)",
    "StopBlt3dRuntimeLayoutRepairTimer();",
):
    if token not in repair:
        errors.append("WorkspacePanel runtime repair missing: " + token)

# The authoritative reference layout must continue to expose Menu/model tree + Family/Properties
# in the left plugin palette and retire only legacy duplicate bands. The centre remains host-owned.
for token in (
    "modelColumn.Width = new GridLength(168);",
    "familyColumn.Width = new GridLength(1, GridUnitType.Star);",
    "RestoreReferenceFamilyRows();",
    "Grid.GetColumn(child) <= 2",
):
    if token not in reference:
        errors.append("BLT3D reference layout contract missing: " + token)

if "new Viewport" in repair or "Viewport3D" in repair:
    errors.append("runtime repair must not create a fake second 3D viewport")

print("QS3D BLT3D BIM runtime layout reassert preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM activation reasserts all QS3D side palettes through a bounded BricsCAD docking settle window, while WorkspacePanel replays the authoritative Menu + Family/Properties layout and keeps native modelspace as the centre viewport.")
