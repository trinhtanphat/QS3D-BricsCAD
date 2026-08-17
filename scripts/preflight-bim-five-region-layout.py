#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"


def method_slice(text: str, signature: str, next_signature: str) -> str:
    start = text.find(signature)
    end = text.find(next_signature, start + len(signature))
    if start < 0 or end <= start:
        return ""
    return text[start:end]


def validate(text: str) -> list[str]:
    errors: list[str] = []

    bim = method_slice(text, "public static void ShowBimWorkspace()", "public static void ShowDrawingManagement()")
    workspace = method_slice(text, "public static void ShowWorkspace()", "public static void ShowBimWorkspace()")
    drawing = method_slice(text, "public static void ShowDrawingManagement()", "public static void ShowQuantityInsight()")
    quantity = method_slice(text, "public static void ShowQuantityInsight()", "public static void Hide()")
    safe_mode = method_slice(text, "public static void ShowSafeMode()", "public static void SetInspection")
    reset = method_slice(text, "private static void ResetPreservingVisibility()", "public static void Dispose()")
    dock = method_slice(text, "private static void EnsureBimDockContract()", "private static void SetVisibility")

    for section, label in (
        (bim, "ShowBimWorkspace"),
        (workspace, "ShowWorkspace"),
        (drawing, "ShowDrawingManagement"),
        (quantity, "ShowQuantityInsight"),
        (safe_mode, "ShowSafeMode"),
        (reset, "ResetPreservingVisibility"),
        (dock, "EnsureBimDockContract"),
    ):
        if not section:
            errors.append("missing recognizable method section: " + label)

    if errors:
        return errors

    bim_required = (
        "EnsureCreated();",
        "EnsureBimDockContract();",
        "SetVisibility(workspace: true, right: true, quantityInsight: true);",
        "SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);",
        "_rightPanel?.Refresh();",
        "_quantityInsightPanel?.RefreshQuantityInsights();",
    )
    for token in bim_required:
        if token not in bim:
            errors.append("BIM five-region contract missing: " + token)

    dock_pos = bim.find("EnsureBimDockContract();")
    visibility_pos = bim.find("SetVisibility(workspace: true, right: true, quantityInsight: true);")
    if min(dock_pos, visibility_pos) < 0 or dock_pos > visibility_pos:
        errors.append("BIM dock contract must be applied before showing the full palette set")

    if "quantityInsight: false" in bim:
        errors.append("BIM workspace must not hide Quantity Insight")

    isolated_contracts = (
        (workspace, "SetVisibility(workspace: true, right: false, quantityInsight: false);", "ordinary Workspace"),
        (drawing, "SetVisibility(workspace: false, right: true, quantityInsight: false);", "drawing management"),
        (quantity, "SetVisibility(workspace: false, right: false, quantityInsight: true);", "selected-object Quantity Insight"),
        (safe_mode, "SetVisibility(workspace: true, right: false, quantityInsight: false);", "Safe Mode"),
    )
    for section, token, label in isolated_contracts:
        if token not in section:
            errors.append(label + " visibility isolation regressed")

    reset_required = (
        "var workspaceVisible = IsWorkspaceVisible;",
        "var rightVisible = IsRightPanelVisible;",
        "var quantityVisible = IsQuantityInsightVisible;",
        "if (workspaceVisible && rightVisible && quantityVisible)",
        "EnsureBimDockContract();",
        "SetVisibility(workspaceVisible, rightVisible, quantityVisible);",
    )
    for token in reset_required:
        if token not in reset:
            errors.append("BIM reset/recreation contract missing: " + token)

    dock_required = (
        "_workspace.Dock != DockSides.Left",
        "_workspace.Dock = DockSides.Left;",
        "_right.Dock != DockSides.Right",
        "_right.Dock = DockSides.Right;",
        "_quantityInsight.Dock != DockSides.Right",
        "_quantityInsight.Dock = DockSides.Right;",
    )
    for token in dock_required:
        if token not in dock:
            errors.append("BIM dock contract missing: " + token)

    return errors


def run_mutation_self_checks(pristine: str) -> list[str]:
    failures: list[str] = []
    mutations = {
        "hide quantity insight in BIM": (
            "SetVisibility(workspace: true, right: true, quantityInsight: true);",
            "SetVisibility(workspace: true, right: true, quantityInsight: false);",
        ),
        "lose quantity refresh": (
            "_quantityInsightPanel?.RefreshQuantityInsights();",
            "// quantity refresh removed",
        ),
        "lose BIM reset detection": (
            "if (workspaceVisible && rightVisible && quantityVisible)",
            "if (workspaceVisible && rightVisible && !quantityVisible)",
        ),
        "lose Quantity Insight right dock": (
            "if (_quantityInsight != null && _quantityInsight.Dock != DockSides.Right)",
            "if (false)",
        ),
        "break ordinary Workspace isolation": (
            "SetVisibility(workspace: true, right: false, quantityInsight: false);",
            "SetVisibility(workspace: true, right: true, quantityInsight: true);",
        ),
    }

    for label, (needle, replacement) in mutations.items():
        if needle not in pristine:
            failures.append("self-check fixture missing mutation anchor: " + label)
            continue
        mutated = pristine.replace(needle, replacement, 1)
        if not validate(mutated):
            failures.append("guard did not detect mutation: " + label)

    return failures


def main() -> int:
    if not SOURCE.is_file():
        print("ERROR: missing " + str(SOURCE.relative_to(ROOT)))
        return 1

    text = SOURCE.read_text(encoding="utf-8")
    errors = validate(text)
    errors.extend(run_mutation_self_checks(text))

    print("QS3D BIM five-region palette layout preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print("FAILED with", len(errors), "error(s).")
        return 1

    print(
        "PASS: BIM workspace shows Workspace + Management + Quantity Insight around the native viewport, "
        "reapplies left/right docking after palette recreation, and preserves isolated non-BIM workflows."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
