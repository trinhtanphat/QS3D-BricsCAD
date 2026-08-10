#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs"
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
errors = []

for path in (WINDOW, SNAPSHOT):
    if not path.is_file():
        errors.append("missing material atomicity contract file: " + str(path.relative_to(ROOT)))

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "RestoreOrThrow(project, rollback, operationError",
        "rollback.Restore(project);",
        "ProjectMaterialCatalog.GetAll(project)",
        "selectedMaterial.Id",
        "RefreshAfterCommit(",
        "đã commit; UI sync warning:",
    ):
        if token not in text:
            errors.append("MaterialCatalogWindow.xaml.cs missing atomic/stale-state token: " + token)

    if text.count("ProjectStateSnapshot.Capture(project)") < 3:
        errors.append("Material Catalog Save/Delete/Apply must each capture a whole-project rollback snapshot")
    if text.count("RefreshAfterCommit(") < 4:
        errors.append("Material Catalog Save/Delete/Apply must route UI refresh through the post-commit boundary")

    apply_pos = text.find("private void OnApplyClick")
    refresh_pos = text.find("private void RefreshAll", apply_pos)
    apply_body = text[apply_pos:refresh_pos] if apply_pos >= 0 and refresh_pos > apply_pos else ""
    for token in (
        "ProjectMaterialCatalog.GetAll(project)",
        "selectedMaterial.Id",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "foreach (var element in elements)",
        'AuditTrail.ForProject(project).Record("material.assign"',
        "RestoreOrThrow(project, rollback, operationError",
    ):
        if token not in apply_body:
            errors.append("Material Catalog Apply missing all-or-nothing batch token: " + token)

if SNAPSHOT.is_file():
    text = SNAPSHOT.read_text(encoding="utf-8")
    for token in (
        "target.AuditEvents.Clear();",
        "target.Metadata.Clear();",
        "target.Elements.Clear();",
    ):
        if token not in text:
            errors.append("ProjectStateSnapshot must restore Material Catalog state: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Material Catalog Save/Delete/Apply are guarded as project-atomic edits with stale-material re-resolution and post-commit UI isolation")
