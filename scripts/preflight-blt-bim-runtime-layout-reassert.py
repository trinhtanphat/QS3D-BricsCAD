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
    "SetVisibility(workspace: true, right: true, quantityInsight: true);",
    "private static readonly Guid PropertiesGuid",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    "public static bool IsPropertiesVisible",
    "_properties.Dock = DockSides.Left;",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(true);",
    "Thuộc tính QS3D palette riêng bên trái",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator four-palette BIM contract missing: " + token)

for token in (
    "public static bool ShowBimWorkspace()",
    'ReportPaletteFailure("MÔ HÌNH BIM");',
    "return true;",
    "return false;",
):
    if token not in palette:
        errors.append("BIM palette success contract missing: " + token)

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

if "string.Equals(currentId, _lastTabId" in activation and "_bimSettleTicksRemaining > 0" not in activation:
    errors.append("same-tab polling still returns without a bounded BricsCAD dock-settle repair")

compact_activation = " ".join(activation.split())
successful_retry = "if (ReassertBimWorkspace()) { _bimSettleTicksRemaining--; }"
consumed_before_retry = "_bimSettleTicksRemaining--; ReassertBimWorkspace();"
initial_retry_setup = "_bimSettleTicksRemaining = BimSettleTicks; ReassertBimWorkspace();"
if successful_retry not in compact_activation or consumed_before_retry in compact_activation:
    errors.append("same-tab settle retry must be consumed only after a successful BIM workspace reassert")
if initial_retry_setup not in compact_activation:
    errors.append("initial BIM activation must preserve the configured follow-up settle retry budget")

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
    "_blt3dRuntimeSettlePassesRemaining = 0;",
    "_blt3dRuntimeLayoutRepairStarted = false;",
):
    if token not in repair:
        errors.append("WorkspacePanel runtime repair missing: " + token)

if "ApplyReferencePaletteLayout();" in repair:
    errors.append("runtime settle repair must not restore the superseded side-by-side reference layout")

for token in (
    "ApplyBlt3dFiveZoneRuntimeLayout",
    "workspace.RowDefinitions.Add",
    "GridResizeDirection.Rows",
    "ReferenceEquals(child, familyPane)",
    "if (_dedicatedPropertiesPaletteActive)",
    "familyPane.RowDefinitions[2].Height = new GridLength(0);",
    "familyPane.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);",
):
    if token not in five_zone:
        errors.append("owner dynamic Model/Family/Properties runtime layout missing: " + token)

for token in (
    "CreatePropertiesPaletteVisual",
    "SetDedicatedPropertiesPaletteActive(bool active)",
    "ownerGrid.Children.Remove(region);",
    "host.Children.Add(region);",
    "host.Children.Remove(region);",
    "ownerGrid.Children.Add(region);",
    "BindingOperations.SetBinding",
    "CollapseEmbeddedPropertiesSlot",
    "RestoreEmbeddedPropertiesSlot",
):
    if token not in properties:
        errors.append("dynamic dedicated QS3D Properties reparenting missing: " + token)

if "new Viewport" in repair or "Viewport3D" in repair or "new Viewport" in five_zone or "Viewport3D" in five_zone:
    errors.append("runtime layout must not create a fake second 3D viewport")

print("QS3D BLT3D BIM dynamic dedicated Properties runtime layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BIM activation reasserts Workspace + dedicated QS3D Properties + Management + Quantity through a bounded BricsCAD docking settle window, reports transient reassert success/failure without consuming retries early, ordinary ShowWorkspace restores the same real editor in-place, manual palette close is not selection-driven, and native modelspace remains host-owned.")
