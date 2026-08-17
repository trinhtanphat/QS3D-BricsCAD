#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DedicatedPropertiesPaletteCoordinator.cs"
SELECTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
BASE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


dedicated = read(PALETTE)
selection = read(SELECTION)
base = read(BASE)

for token in (
    "internal static class DedicatedPropertiesPaletteCoordinator",
    'new Guid("43E4BCFA-1697-43D4-95EF-90B88C59D61A")',
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    'AddVisual("Thuộc tính QS3D", _panel, true)',
    "Dock = DockSides.Left",
    "MinimumSize = new DrawingSize(260, 320)",
    "_palette.DeviceIndependentSize = DefaultSize;",
    "if (_palette.Dock != DockSides.Left)",
    "PaletteCoordinator.IsWorkspaceVisible",
    "PaletteCoordinator.IsRightPanelVisible",
    "PaletteCoordinator.IsQuantityInsightVisible",
    "internal sealed class DedicatedPropertiesPanel : UserControl",
    "QS3D plugin inspector",
    "— nhiều giá trị —",
):
    if token not in dedicated:
        errors.append("dedicated QS3D Properties contract missing: " + token)

for token in (
    "DedicatedPropertiesPaletteCoordinator.SyncVisibility();",
    "DedicatedPropertiesPaletteCoordinator.SetInspection(snapshots);",
    "DedicatedPropertiesPaletteCoordinator.Hide();",
    "DedicatedPropertiesPaletteCoordinator.Dispose();",
):
    if token not in selection:
        errors.append("selection/BIM integration missing: " + token)

# Existing #2396 behavior must remain intact: explicit QS3D activation restores the coordinated
# BIM surface, while the standalone Workspace command remains isolated. The fourth dedicated
# Properties palette is layered on through selection/BIM synchronization instead of weakening it.
for token in (
    "public static void Show() => ShowBimWorkspace();",
    "SetVisibility(workspace: true, right: false, quantityInsight: false);",
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
):
    if token not in base:
        errors.append("existing BIM/isolated-workspace contract regressed: " + token)

if "new Viewport" in dedicated or "Viewport3D" in dedicated:
    errors.append("dedicated Properties palette must not create or replace the native BricsCAD viewport")

print("QS3D dedicated Properties palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM activation owns a distinct left-docked QS3D Properties plugin palette with deterministic size fallback and selection inspection, while isolated Workspace behavior and native BricsCAD modelspace remain unchanged.")
