#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def require(text, token, label):
    if token not in text:
        fail(label + ": expected source contract not found: " + token)


def forbid(text, token, label):
    if token in text:
        fail(label + ": stale/forbidden source contract found: " + token)


def main():
    if not PALETTE.is_file():
        fail("missing PaletteCoordinator.cs")
    if not V26_PROJECT.is_file():
        fail("missing V26 project file")

    source = PALETTE.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")

    require(source, "public static void ShowBimWorkspace()", "BIM workspace entry point")
    require(source, "EnsureBimDockContract();", "BIM dock contract")
    require(
        source,
        "SetVisibility(workspace: true, right: true, quantityInsight: true);",
        "BIM full palette visibility",
    )
    forbid(
        source,
        "SetVisibility(workspace: true, right: true, quantityInsight: false);",
        "BIM quantity palette regression",
    )

    for token in (
        "if (_workspace != null && _workspace.Dock != DockSides.Left)",
        "_workspace.Dock = DockSides.Left;",
        "if (_right != null && _right.Dock != DockSides.Right)",
        "_right.Dock = DockSides.Right;",
        "if (_quantityInsight != null && _quantityInsight.Dock != DockSides.Right)",
        "_quantityInsight.Dock = DockSides.Right;",
        "if (workspaceVisible && rightVisible && quantityVisible)",
    ):
        require(source, token, "BIM docking/reset contract")
    forbid(
        source,
        "if (workspaceVisible && rightVisible && !quantityVisible)",
        "stale BIM reset visibility contract",
    )

    for token in (
        "SetVisibility(workspace: true, right: false, quantityInsight: false);",
        "SetVisibility(workspace: false, right: true, quantityInsight: false);",
        "SetVisibility(workspace: false, right: false, quantityInsight: true);",
    ):
        require(source, token, "isolated palette command contract")

    require(
        v26_project,
        '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
        "V26 shared adapter source",
    )

    print(
        "PASS: MÔ HÌNH BIM restores Workspace + Management + Quantity Insight around the native "
        "BricsCAD viewport, preserves left/right docking after palette recreation, and leaves "
        "ordinary isolated palette commands unchanged."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
