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
        "EnsurePaletteOwnership(propertiesPalette, _properties, \"Properties\");",
        "EnsurePaletteOwnership(rightPalette, _right, \"Right\");",
        "EnsurePaletteOwnership(quantityPalette, _quantityInsight, \"QuantityInsight\");",
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
    last_apply = body.find("SetPaletteVisibility(quantityPalette")
    final_ownership = body.find("EnsurePaletteOwnership(workspacePalette", last_apply if last_apply >= 0 else 0)
    last_snapshot = max(
        body.find("var workspaceRead = TryReadPaletteVisibility"),
        body.find("var propertiesRead = TryReadPaletteVisibility"),
        body.find("var rightRead = TryReadPaletteVisibility"),
        body.find("var quantityRead = TryReadPaletteVisibility"),
    )
    admission = body.find("if (!workspaceRead || !propertiesRead || !rightRead || !quantityRead)")
    # Search for the real failure-path catch only after the final ownership fence. This avoids
    # false negatives if explanatory comments before the fence happen to contain the word "catch".
    catch_pos = body.find("catch", final_ownership if final_ownership >= 0 else 0)
    rollback_pos = body.find("TryRestorePaletteVisibility(quantityPalette", catch_pos if catch_pos >= 0 else 0)
    rethrow_pos = body.find("throw;", rollback_pos if rollback_pos >= 0 else 0)
    if min(first_apply, last_apply, final_ownership, last_snapshot, admission, catch_pos, rollback_pos, rethrow_pos) < 0 or not (
        last_snapshot < admission < first_apply <= last_apply < final_ownership < catch_pos < rollback_pos < rethrow_pos
    ):
        errors.append("SetVisibility must snapshot before mutation, revalidate ownership after native setters, then rollback/rethrow only from the failure path")

for helper_name in [
    "private static void SetPaletteVisibility",
    "private static void EnsurePaletteOwnership",
    "private static void TryRestorePaletteVisibility",
]:
    if helper_name not in source:
        errors.append("missing helper: " + helper_name)

set_helper_start = source.find("private static void SetPaletteVisibility")
ownership_helper_start = source.find("private static void EnsurePaletteOwnership")
restore_helper_start = source.find("private static void TryRestorePaletteVisibility")
report_start = source.find("private static void ReportPaletteFailure", restore_helper_start if restore_helper_start >= 0 else 0)
set_helper_end = ownership_helper_start if ownership_helper_start >= 0 else restore_helper_start
set_helper = source[set_helper_start:set_helper_end if set_helper_end >= 0 else len(source)] if set_helper_start >= 0 else ""
ownership_helper = source[ownership_helper_start:restore_helper_start if restore_helper_start >= 0 else len(source)] if ownership_helper_start >= 0 else ""
restore_helper = source[restore_helper_start:report_start if report_start >= 0 else len(source)] if restore_helper_start >= 0 else ""

for needle in [
    "if (expected == null) return;",
    "if (!ReferenceEquals(expected, current))",
    "throw new InvalidOperationException",
    "expected.Visible = visible;",
]:
    if needle not in set_helper:
        errors.append("apply helper must fail closed on stale native PaletteSet ownership: " + needle)
stale_guard = set_helper.find("if (!ReferenceEquals(expected, current))")
stale_throw = set_helper.find("throw new InvalidOperationException", stale_guard if stale_guard >= 0 else 0)
apply_pos = set_helper.find("expected.Visible = visible;")
if min(stale_guard, stale_throw, apply_pos) < 0 or not (stale_guard < stale_throw < apply_pos):
    errors.append("apply helper must reject stale ownership before native visibility mutation")

for needle in ["if (expected == null) return;", "if (!ReferenceEquals(expected, current))", "throw new InvalidOperationException"]:
    if needle not in ownership_helper:
        errors.append("post-apply ownership helper must fail closed on native setter reentrancy: " + needle)

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
    if forbidden in body + set_helper + ownership_helper + restore_helper:
        errors.append("visibility rollback must remain UI-state-only: " + forbidden)

print("QS3D V25 palette visibility rollback preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: palette visibility transitions snapshot, exact-fence before apply, revalidate ownership after native setters, rollback best-effort, and rethrow on failure.")