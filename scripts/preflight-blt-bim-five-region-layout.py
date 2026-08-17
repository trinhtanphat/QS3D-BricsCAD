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

    workspace = method("public static void ShowWorkspace()", "public static bool ShowBimWorkspace()")
    bim = method("public static bool ShowBimWorkspace()", "public static void ShowDrawingManagement()")
    management = method("public static void ShowDrawingManagement()", "public static void ShowQuantityInsight()")
    quantity = method("public static void ShowQuantityInsight()", "public static void Hide()")
    reset = method("private static void ResetPreservingVisibility()", "public static void Dispose()")
    dock = method("private static void EnsureBimDockContract()", "private static void EnsurePaletteSize(")

    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in workspace:
        errors.append("ordinary Workspace must keep the real Properties editor embedded")
    if "SetVisibility(workspace: true, right: false, quantityInsight: false);" not in workspace:
        errors.append("ordinary Workspace must remain isolated")

    for token in (
        "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);",
        "EnsureBimDockContract();",
        "SetVisibility(workspace: true, right: true, quantityInsight: false);",
        "return true;",
        "return false;",
        "viewport BricsCAD native ở giữa",
    ):
        if token not in bim:
            errors.append("owner-reference BIM default missing: " + token)

    if "SetDedicatedPropertiesPaletteActive(true)" in bim:
        errors.append("default BIM must not pull Properties out of the embedded Family/Properties column")
    if "SetVisibility(workspace: true, right: true, quantityInsight: true);" in bim:
        errors.append("default BIM must not auto-open Quantity Insight")
    if "_quantityInsightPanel?.RefreshQuantityInsights();" in bim:
        errors.append("default BIM must not refresh an auto-hidden Quantity Insight surface")

    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in management:
        errors.append("standalone Management must keep Properties embedded")
    if "SetVisibility(workspace: false, right: true, quantityInsight: false);" not in management:
        errors.append("standalone Management isolation changed")
    if "_workspacePanel?.SetDedicatedPropertiesPaletteActive(false);" not in quantity:
        errors.append("standalone Quantity Insight must not steal the Workspace Properties editor")
    if "SetVisibility(workspace: false, right: false, quantityInsight: true);" not in quantity:
        errors.append("standalone Quantity Insight isolation changed")

    for token in (
        "_workspace.Dock != DockSides.Left",
        "_workspace.Dock = DockSides.Left;",
        "_right.Dock != DockSides.Right",
        "_right.Dock = DockSides.Right;",
        "_properties.Dock != DockSides.Left",
        "_quantityInsight.Dock != DockSides.Right",
    ):
        if token not in dock:
            errors.append("palette dock capability missing: " + token)

    for token in (
        "var ownerReferenceBimActive = workspaceVisible && rightVisible && !propertiesVisible && !quantityVisible;",
        "_workspacePanel?.SetDedicatedPropertiesPaletteActive(propertiesVisible);",
        "if (ownerReferenceBimActive)",
        "EnsureBimDockContract();",
        "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
    ):
        if token not in reset:
            errors.append("palette recreation lost owner-reference visibility restore: " + token)

    if "SetVisibility(workspace, properties: false, right, quantityInsight);" not in text:
        errors.append("legacy three-argument visibility must keep dedicated Properties opt-in only")
    if "if (_properties != null) _properties.Visible = properties;" not in text:
        errors.append("dedicated Properties visibility must remain centrally controllable")

print("QS3D BLT3D BIM owner-reference layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: default BIM shows the integrated two-column QS3D Workspace plus Drawing/Layer Management around native BricsCAD modelspace; dedicated Properties and Quantity remain optional isolated palettes, and visibility/docking restores deterministically.")