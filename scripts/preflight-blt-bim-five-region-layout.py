#!/usr/bin/env python3
from pathlib import Path
import sys

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

    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in workspace:
        errors.append("ordinary Workspace must restore the real Properties editor in-place")
    if "SetVisibility(workspace: true, right: false, quantityInsight: false);" not in workspace:
        errors.append("ordinary Workspace must remain Ribbon-first and isolated")
    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(true);" not in bim:
        errors.append("BIM workspace must move the real Properties editor into the dedicated palette")
    if "EnsureBimDockContract();" not in bim:
        errors.append("BIM workspace must repair the dock contract before visibility")
    if "SetVisibility(workspace: true, right: true, quantityInsight: true);" not in bim:
        errors.append("BIM workspace must show Workspace + dedicated QS3D Properties + Management + Quantity Insight together")
    if "_quantityInsightPanel?.RefreshQuantityInsights();" not in bim:
        errors.append("BIM workspace must refresh Quantity Insight when it becomes visible")
    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in management:
        errors.append("standalone Management must restore the embedded Properties editor before isolating Workspace")
    if "SetVisibility(workspace: false, right: true, quantityInsight: false);" not in management:
        errors.append("standalone Management command isolation changed")
    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in quantity:
        errors.append("standalone Quantity Insight must restore the embedded Properties editor before isolating Workspace")
    if "SetVisibility(workspace: false, right: false, quantityInsight: true);" not in quantity:
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
    )
    for token in required_dock:
        if token not in dock:
            errors.append("BIM dock contract missing: " + token)

    for token in (
        "var bimSurfaceActive = workspaceVisible && rightVisible && quantityVisible;",
        "_workspacePanel?.SetDedicatedPropertiesPaletteActive(bimSurfaceActive);",
        "if (bimSurfaceActive)",
        "EnsureBimDockContract();",
        "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
    ):
        if token not in reset:
            errors.append("palette recreation lost dynamic four-palette BIM restore contract: " + token)

    if "var properties = workspace && right && quantityInsight;" not in text:
        errors.append("legacy three-argument BIM visibility path must enable dedicated QS3D Properties only for the coordinated BIM state")
    if "if (_properties != null) _properties.Visible = properties;" not in text:
        errors.append("dedicated QS3D Properties visibility must be controlled by the central visibility helper")

    legacy_hidden_bim = "SetVisibility(workspace: true, right: true, quantityInsight: false);"
    if legacy_hidden_bim in bim:
        errors.append("regression: BIM workspace still hides Quantity Insight")

print("QS3D BLT3D BIM five-region layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: explicit BIM mode dynamically reparents the real QS3D Properties editor into a distinct palette, preserves isolated commands, and restores four-palette docking/visibility after recreation around native BricsCAD modelspace.")
