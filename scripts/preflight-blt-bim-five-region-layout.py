#!/usr/bin/env python3
from pathlib import Path
import sys

# #2450 intentionally left owner-facing QS3D activation to #2399; this guard owns that follow-up contract.
ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
errors = []

if not PALETTE.is_file():
    errors.append("missing PaletteCoordinator.cs")
else:
    text = PALETTE.read_text(encoding="utf-8")

    def method(name: str, next_name: str) -> str:
        start = text.find(name)
        end = text.find(next_name, start + 1) if start >= 0 else -1
        return text[start:end] if start >= 0 and end > start else ""

    workspace = method("public static void ShowWorkspace()", "public static void ShowBimWorkspace()")
    bim = method("public static void ShowBimWorkspace()", "public static void ShowDrawingManagement()")
    management = method("public static void ShowDrawingManagement()", "public static void ShowQuantityInsight()")
    quantity = method("public static void ShowQuantityInsight()", "public static void Hide()")
    reset = method("private static void ResetPreservingVisibility()", "public static void Dispose()")
    dock = method("private static void EnsureBimDockContract()", "private static void SetVisibility(")

    if "SetVisibility(workspace: true, properties: false, right: false, quantityInsight: false);" not in workspace:
        errors.append("ordinary Workspace must remain Ribbon-first and isolated from the dedicated Properties/side palettes")
    if "EnsureBimDockContract();" not in bim:
        errors.append("BIM workspace must repair the dock contract before visibility")
    if "SetVisibility(workspace: true, properties: true, right: true, quantityInsight: true);" not in bim:
        errors.append("BIM workspace must show Workspace + QS3D Properties + Management + Quantity Insight together")
    if "_quantityInsightPanel?.RefreshQuantityInsights();" not in bim:
        errors.append("BIM workspace must refresh Quantity Insight when it becomes visible")
    if "SetVisibility(workspace: false, properties: false, right: true, quantityInsight: false);" not in management:
        errors.append("standalone Management command isolation changed")
    if "SetVisibility(workspace: false, properties: false, right: false, quantityInsight: true);" not in quantity:
        errors.append("standalone Quantity Insight command isolation changed")

    required_dock = (
        "_workspace.Dock != DockSides.Left",
        "_workspace.Dock = DockSides.Left;",
        "_properties.Dock != DockSides.Left",
        "_properties.Dock = DockSides.Left;",
        "_right.Dock != DockSides.Right",
        "_right.Dock = DockSides.Right;",
        "_quantityInsight.Dock != DockSides.Right",
        "_quantityInsight.Dock = DockSides.Right;",
        "ReassertPersistedPaletteSizes();",
    )
    for token in required_dock:
        if token not in dock:
            errors.append("BIM dock contract missing: " + token)

    if "if (workspaceVisible && propertiesVisible)" not in reset:
        errors.append("palette recreation must preserve the paired left QS3D surfaces")
    if "if (rightVisible && quantityVisible) EnsureBimDockContract();" not in reset:
        errors.append("palette recreation must reapply the full BIM dock contract when all coordinated palettes remain visible")
    if "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);" not in reset:
        errors.append("palette recreation must preserve the user's actual four-palette visibility state")

    legacy_hidden_bim = "SetVisibility(workspace: true, properties: true, right: true, quantityInsight: false);"
    if legacy_hidden_bim in bim:
        errors.append("regression: BIM workspace still hides Quantity Insight")

print("QS3D BLT3D BIM five-region layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: explicit BIM mode shows Model + dedicated QS3D Properties + native BricsCAD viewport + Management + Quantity regions, preserves isolated commands, and reapplies deterministic left/right docking after palette recreation.")
