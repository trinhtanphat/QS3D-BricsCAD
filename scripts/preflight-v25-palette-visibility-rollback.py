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

method_start = source.find("private static void SetVisibility(bool workspace, bool properties, bool right, bool quantityInsight)")
method_end = source.find("private static void ReportPaletteFailure", method_start if method_start >= 0 else 0)
body = source[method_start:method_end if method_end >= 0 else len(source)] if method_start >= 0 else ""
if method_start < 0:
    errors.append("missing four-surface SetVisibility helper")
else:
    required = [
        "var workspacePalette = _workspace;",
        "var propertiesPalette = _properties;",
        "var rightPalette = _right;",
        "var quantityPalette = _quantityInsight;",
        "bool? workspaceWasVisible = workspacePalette?.Visible;",
        "bool? propertiesWereVisible = propertiesPalette?.Visible;",
        "bool? rightWasVisible = rightPalette?.Visible;",
        "bool? quantityWasVisible = quantityPalette?.Visible;",
        "SetPaletteVisibility(workspacePalette, _workspace, workspace, \"Workspace\");",
        "SetPaletteVisibility(propertiesPalette, _properties, properties, \"Properties\");",
        "SetPaletteVisibility(rightPalette, _right, right, \"Right\");",
        "SetPaletteVisibility(quantityPalette, _quantityInsight, quantityInsight, \"QuantityInsight\");",
        "TryRestorePaletteVisibility(quantityPalette, _quantityInsight, quantityWasVisible);",
        "TryRestorePaletteVisibility(rightPalette, _right, rightWasVisible);",
        "TryRestorePaletteVisibility(propertiesPalette, _properties, propertiesWereVisible);",
        "TryRestorePaletteVisibility(workspacePalette, _workspace, workspaceWasVisible);",
        "throw;",
    ]
    for needle in required:
        if needle not in body:
            errors.append("SetVisibility rollback contract missing: " + needle)

    first_apply = body.find("SetPaletteVisibility(workspacePalette")
    last_snapshot = max(
        body.find("bool? workspaceWasVisible"),
        body.find("bool? propertiesWereVisible"),
        body.find("bool? rightWasVisible"),
        body.find("bool? quantityWasVisible"),
    )
    catch_pos = body.find("catch")
    rollback_pos = body.find("TryRestorePaletteVisibility(quantityPalette")
    rethrow_pos = body.find("throw;", rollback_pos if rollback_pos >= 0 else 0)
    if min(first_apply, last_snapshot, catch_pos, rollback_pos, rethrow_pos) < 0 or not (last_snapshot < first_apply < catch_pos < rollback_pos < rethrow_pos):
        errors.append("SetVisibility must snapshot before mutation and rollback/rethrow only from the failure path")

for helper_name in ["private static void SetPaletteVisibility", "private static void TryRestorePaletteVisibility"]:
    if helper_name not in source:
        errors.append("missing helper: " + helper_name)

set_helper_start = source.find("private static void SetPaletteVisibility")
restore_helper_start = source.find("private static void TryRestorePaletteVisibility")
report_start = source.find("private static void ReportPaletteFailure", restore_helper_start if restore_helper_start >= 0 else 0)
set_helper = source[set_helper_start:restore_helper_start if restore_helper_start >= 0 else len(source)] if set_helper_start >= 0 else ""
restore_helper = source[restore_helper_start:report_start if report_start >= 0 else len(source)] if restore_helper_start >= 0 else ""

for needle in ["ReferenceEquals(expected, current)", "expected.Visible = visible;"]:
    if needle not in set_helper:
        errors.append("apply helper must fence exact native PaletteSet ownership: " + needle)
for needle in ["ReferenceEquals(expected, current)", "expected.Visible = priorVisibility.Value;", "catch"]:
    if needle not in restore_helper:
        errors.append("rollback helper must be exact-instance and best-effort: " + needle)

for forbidden in [
    "DisposeCore(",
    "EnsureCreated(",
    "ProjectContextCoordinator",
    "DocumentLock",
    "StartTransaction",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
    "UserUiLayoutStore.Update",
]:
    if forbidden in body + set_helper + restore_helper:
        errors.append("visibility rollback must remain UI-state-only: " + forbidden)

print("QS3D V25 palette visibility rollback preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: palette visibility transitions snapshot, exact-fence, rollback best-effort, and rethrow on native failure.")
