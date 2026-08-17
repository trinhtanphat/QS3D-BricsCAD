#!/usr/bin/env python3
from pathlib import Path
import re
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


def method_block(text, signature, next_signature, label):
    start = text.find(signature)
    if start < 0:
        fail(label + ": method signature not found: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        fail(label + ": method boundary not found: " + next_signature)
    return text[start:end]


def require_reset_dock_contract(source):
    reset = method_block(
        source,
        "private static void ResetPreservingVisibility()",
        "public static void Dispose()",
        "BIM reset contract",
    )

    captures = {}
    for local_name, property_name in re.findall(
        r"\bvar\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*"
        r"(IsWorkspaceVisible|IsRightPanelVisible|IsQuantityInsightVisible)\s*;",
        reset,
    ):
        captures[property_name] = local_name

    expected_properties = (
        "IsWorkspaceVisible",
        "IsRightPanelVisible",
        "IsQuantityInsightVisible",
    )
    missing = [name for name in expected_properties if name not in captures]
    if missing:
        fail("BIM reset contract: missing preserved visibility source(s): " + ", ".join(missing))

    condition_match = re.search(
        r"if\s*\(([^)]*)\)\s*EnsureBimDockContract\(\);",
        reset,
        flags=re.MULTILINE,
    )
    if condition_match is None:
        fail("BIM reset contract: EnsureBimDockContract must be guarded after palette recreation")

    actual_terms = [term.strip() for term in condition_match.group(1).split("&&")]
    expected_terms = [captures[name] for name in expected_properties]
    if len(actual_terms) != len(expected_terms) or set(actual_terms) != set(expected_terms):
        fail(
            "BIM reset contract: re-dock condition must require preserved Workspace + Management + "
            "Quantity visibility, independent of local variable names"
        )

    visibility_match = re.search(
        r"SetVisibility\s*\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*([^)]+)\s*\)\s*;",
        reset,
    )
    if visibility_match is None:
        fail("BIM reset contract: preserved visibility must be restored after recreation")

    actual_args = [group.strip() for group in visibility_match.groups()]
    if actual_args != expected_terms:
        fail(
            "BIM reset contract: SetVisibility must restore the captured Workspace, Management, "
            "and Quantity visibility values in order"
        )


def main():
    if not PALETTE.is_file():
        fail("missing PaletteCoordinator.cs")
    if not V26_PROJECT.is_file():
        fail("missing V26 project file")

    source = PALETTE.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")

    # MÔ HÌNH BIM owns the coordinated BLT3D-familiar side workspace while the real
    # BricsCAD viewport remains native in the center. All three QS3D palettes must be visible.
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

    # The BIM layout is left Workspace + native viewport + right Management + right Quantity
    # explanation. Recreated palettes must restore the same docking contract.
    for token in (
        "if (_workspace != null && _workspace.Dock != DockSides.Left)",
        "_workspace.Dock = DockSides.Left;",
        "if (_right != null && _right.Dock != DockSides.Right)",
        "_right.Dock = DockSides.Right;",
        "if (_quantityInsight != null && _quantityInsight.Dock != DockSides.Right)",
        "_quantityInsight.Dock = DockSides.Right;",
    ):
        require(source, token, "BIM docking contract")
    require_reset_dock_contract(source)

    # Keep the ribbon-first isolated commands unchanged: only MÔ HÌNH BIM shows the full set.
    for token in (
        "SetVisibility(workspace: true, right: false, quantityInsight: false);",
        "SetVisibility(workspace: false, right: true, quantityInsight: false);",
        "SetVisibility(workspace: false, right: false, quantityInsight: true);",
    ):
        require(source, token, "isolated palette command contract")

    # V26 shares the V25 adapter sources; this source correction must not fork by host version.
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
