#!/usr/bin/env python3
from pathlib import Path
import re
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

    if "SetVisibility(workspace: true, right: false, quantityInsight: false);" not in workspace:
        errors.append("ordinary Workspace must remain Ribbon-first and isolated")
    if "EnsureBimDockContract();" not in bim:
        errors.append("BIM workspace must repair the dock contract before visibility")
    if "SetVisibility(workspace: true, right: true, quantityInsight: true);" not in bim:
        errors.append("BIM workspace must show Workspace + Management + Quantity Insight together")
    if "_quantityInsightPanel?.RefreshQuantityInsights();" not in bim:
        errors.append("BIM workspace must refresh Quantity Insight when it becomes visible")
    if "SetVisibility(workspace: false, right: true, quantityInsight: false);" not in management:
        errors.append("standalone Management command isolation changed")
    if "SetVisibility(workspace: false, right: false, quantityInsight: true);" not in quantity:
        errors.append("standalone Quantity Insight command isolation changed")

    required_dock = (
        "_workspace.Dock != DockSides.Left",
        "_workspace.Dock = DockSides.Left;",
        "_right.Dock != DockSides.Right",
        "_right.Dock = DockSides.Right;",
        "_quantityInsight.Dock != DockSides.Right",
        "_quantityInsight.Dock = DockSides.Right;",
    )
    for token in required_dock:
        if token not in dock:
            errors.append("BIM dock contract missing: " + token)

    preserved = {}
    for local_name, property_name in re.findall(
        r"\bvar\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*"
        r"(IsWorkspaceVisible|IsRightPanelVisible|IsQuantityInsightVisible)\s*;",
        reset,
    ):
        preserved[property_name] = local_name

    required_visibility = (
        "IsWorkspaceVisible",
        "IsRightPanelVisible",
        "IsQuantityInsightVisible",
    )
    missing_visibility = [name for name in required_visibility if name not in preserved]
    redock_match = re.search(
        r"if\s*\(([^)]*)\)\s*EnsureBimDockContract\(\);",
        reset,
        flags=re.MULTILINE,
    )
    if missing_visibility or redock_match is None:
        errors.append(
            "palette recreation must preserve Workspace + Management + Quantity visibility before reapplying the BIM dock contract"
        )
    else:
        actual_terms = [term.strip() for term in redock_match.group(1).split("&&")]
        expected_terms = [preserved[name] for name in required_visibility]
        if len(actual_terms) != len(expected_terms) or set(actual_terms) != set(expected_terms):
            errors.append(
                "palette recreation must reapply the BIM dock contract only while all coordinated side palettes remain visible"
            )

    if "EnsureBimDockContract();" not in reset:
        errors.append("palette recreation must reapply the BIM dock contract after palette recreation")
    if "SetVisibility(workspaceVisible, rightVisible, quantityVisible);" not in reset:
        errors.append("palette recreation must preserve the user's actual visibility state")

    legacy_hidden_bim = "SetVisibility(workspace: true, right: true, quantityInsight: false);"
    if legacy_hidden_bim in bim:
        errors.append("regression: BIM workspace still hides Quantity Insight")

print("QS3D BLT3D BIM five-region layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: explicit BIM mode shows the complete palette set around the native BricsCAD viewport, preserves isolated commands, and reapplies left/right docking after palette recreation.")
