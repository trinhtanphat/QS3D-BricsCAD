#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

reset_start = source.find("private static void ResetPreservingVisibility()")
reset_end = source.find("public static void Dispose()", reset_start if reset_start >= 0 else 0)
reset = source[reset_start:reset_end if reset_end >= 0 else len(source)] if reset_start >= 0 else ""
if reset_start < 0:
    errors.append("missing ResetPreservingVisibility")
else:
    required = [
        "var workspacePalette = _workspace;",
        "var propertiesPalette = _properties;",
        "var rightPalette = _right;",
        "var quantityPalette = _quantityInsight;",
        "var workspaceRead = TryReadPaletteVisibility(workspacePalette, out var workspaceVisible);",
        "var propertiesRead = TryReadPaletteVisibility(propertiesPalette, out var propertiesVisible);",
        "var rightRead = TryReadPaletteVisibility(rightPalette, out var rightVisible);",
        "var quantityRead = TryReadPaletteVisibility(quantityPalette, out var quantityVisible);",
        "workspaceRead && propertiesRead && rightRead && quantityRead &&",
        "Dispose();",
        "EnsureCreated();",
    ]
    for needle in required:
        if needle not in reset:
            errors.append("reset visibility-containment contract missing: " + needle)

    for forbidden in [
        "var workspaceVisible = IsWorkspaceVisible;",
        "var propertiesVisible = IsPropertiesVisible;",
        "var rightVisible = IsRightPanelVisible;",
        "var quantityVisible = IsQuantityInsightVisible;",
    ]:
        if forbidden in reset:
            errors.append("reset must not dereference uncontained native visibility property: " + forbidden)

    snapshot_pos = reset.find("var workspacePalette = _workspace;")
    read_pos = reset.find("var workspaceRead = TryReadPaletteVisibility(workspacePalette, out var workspaceVisible);")
    bim_pos = reset.find("workspaceRead && propertiesRead && rightRead && quantityRead &&")
    dispose_pos = reset.find("Dispose();")
    create_pos = reset.find("EnsureCreated();")
    restore_pos = reset.find("SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);")
    if min(snapshot_pos, read_pos, bim_pos, dispose_pos, create_pos, restore_pos) < 0 or not (
        snapshot_pos < read_pos <= bim_pos < dispose_pos < create_pos < restore_pos
    ):
        errors.append("reset must capture exact palettes, validate visibility reads, dispose, recreate, then restore")

helper_start = source.find("private static bool TryReadPaletteVisibility(PaletteSet? palette, out bool visible)")
helper_end = source.find("public static void Dispose()", helper_start if helper_start >= 0 else 0)
helper = source[helper_start:helper_end if helper_end >= 0 else len(source)] if helper_start >= 0 else ""
if helper_start < 0:
    errors.append("missing TryReadPaletteVisibility helper")
else:
    for needle in [
        "visible = false;",
        "if (palette == null) return false;",
        "try",
        "visible = palette.Visible;",
        "return true;",
        "catch",
        "return false;",
    ]:
        if needle not in helper:
            errors.append("visibility helper must distinguish a trustworthy native getter from hidden fallback: " + needle)

    for forbidden in [
        "MdiActiveDocument",
        "ProjectContextCoordinator",
        "Dispose(",
        "EnsureCreated(",
        "palette.Visible =",
    ]:
        if forbidden in helper:
            errors.append("visibility read helper must remain side-effect free and exact-instance-only: " + forbidden)

print("QS3D V25 palette reset visibility containment preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: reset snapshots exact PaletteSet instances, distinguishes failed native Visible reads from valid hidden state, and still disposes/recreates coherently.")
