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

start = source.find("private static void SetVisibility(bool workspace, bool properties, bool right, bool quantityInsight)")
end = source.find("private static void ReportPaletteFailure", start if start >= 0 else 0)
body = source[start:end if end >= 0 else len(source)] if start >= 0 else ""

if start < 0:
    errors.append("missing four-surface SetVisibility helper")
else:
    required = [
        "var workspacePalette = _workspace;",
        "var propertiesPalette = _properties;",
        "var rightPalette = _right;",
        "var quantityPalette = _quantityInsight;",
        "var workspaceRead = TryReadPaletteVisibility(workspacePalette, out var workspaceWasVisible);",
        "var propertiesRead = TryReadPaletteVisibility(propertiesPalette, out var propertiesWereVisible);",
        "var rightRead = TryReadPaletteVisibility(rightPalette, out var rightWasVisible);",
        "var quantityRead = TryReadPaletteVisibility(quantityPalette, out var quantityWasVisible);",
        "if (!workspaceRead || !propertiesRead || !rightRead || !quantityRead)",
        "SetPaletteVisibility(workspacePalette, _workspace, workspace, \"Workspace\");",
        "SetPaletteVisibility(propertiesPalette, _properties, properties, \"Properties\");",
        "SetPaletteVisibility(rightPalette, _right, right, \"Right\");",
        "SetPaletteVisibility(quantityPalette, _quantityInsight, quantityInsight, \"QuantityInsight\");",
        "EnsurePaletteOwnership(workspacePalette, _workspace, \"Workspace\");",
        "TryRestorePaletteVisibility(quantityPalette, _quantityInsight, quantityWasVisible);",
        "TryRestorePaletteVisibility(workspacePalette, _workspace, workspaceWasVisible);",
    ]
    for needle in required:
        if needle not in body:
            errors.append("SetVisibility snapshot-containment contract missing: " + needle)

    for forbidden in [
        "workspacePalette?.Visible",
        "propertiesPalette?.Visible",
        "rightPalette?.Visible",
        "quantityPalette?.Visible",
    ]:
        if forbidden in body:
            errors.append("SetVisibility must not dereference fallible native visibility outside the contained helper: " + forbidden)

    snapshot_positions = [
        body.find("var workspaceRead = TryReadPaletteVisibility"),
        body.find("var propertiesRead = TryReadPaletteVisibility"),
        body.find("var rightRead = TryReadPaletteVisibility"),
        body.find("var quantityRead = TryReadPaletteVisibility"),
    ]
    admission = body.find("if (!workspaceRead || !propertiesRead || !rightRead || !quantityRead)")
    first_apply = body.find("SetPaletteVisibility(workspacePalette")
    if min(snapshot_positions + [admission, first_apply]) < 0 or not (max(snapshot_positions) < admission < first_apply):
        errors.append("SetVisibility must complete a trustworthy contained four-surface snapshot and fail closed before the first native setter")

helper_start = source.find("private static bool TryReadPaletteVisibility")
helper_end = source.find("public static void Dispose()", helper_start if helper_start >= 0 else 0)
helper = source[helper_start:helper_end if helper_end >= 0 else len(source)] if helper_start >= 0 else ""
if helper_start < 0:
    errors.append("missing TryReadPaletteVisibility helper")
else:
    for needle in ["visible = false;", "visible = palette.Visible;", "return true;", "catch", "return false;"]:
        if needle not in helper:
            errors.append("contained native visibility helper missing: " + needle)
    for forbidden in [
        "MdiActiveDocument",
        "ProjectContextCoordinator",
        "DocumentLock",
        "StartTransaction",
        "SendStringToExecute",
        "Dispatcher.BeginInvoke",
        "DisposeCore(",
        "EnsureCreated(",
    ]:
        if forbidden in helper:
            errors.append("visibility read helper must stay exact-instance, side-effect free and UI-state-only: " + forbidden)

print("QS3D V25 palette SetVisibility snapshot containment preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: SetVisibility acquires a contained trustworthy exact-instance native visibility snapshot before mutation and keeps rollback on the existing exact-owner transaction boundary.")
