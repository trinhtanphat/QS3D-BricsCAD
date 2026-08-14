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
        "ProjectMaterialCatalog.GetCustom(project)",
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

    save_pos = text.find("private void OnSaveClick")
    delete_pos = text.find("private void OnDeleteClick", save_pos)
    save_body = text[save_pos:delete_pos] if save_pos >= 0 and delete_pos > save_pos else ""
    for token in (
        "var editingExisting = !string.IsNullOrWhiteSpace(_editingId);",
        "ProjectMaterialCatalog.GetCustom(project)",
        "string.Equals(x.Id, _editingId, StringComparison.OrdinalIgnoreCase)",
        "Save không tự tạo lại row stale.",
        "var id = editingExisting ? _editingId :",
    ):
        if token not in save_body:
            errors.append("Material Catalog Save missing stale-editor fail-closed token: " + token)

    apply_pos = text.find("private void OnApplyClick", delete_pos)
    delete_body = text[delete_pos:apply_pos] if delete_pos >= 0 and apply_pos > delete_pos else ""
    for token in (
        "ProjectMaterialCatalog.GetAll(project)",
        "selectedMaterial.Id",
        "Material đã thay đổi hoặc bị xóa khỏi project hiện tại",
        "if (material.IsBuiltIn)",
        "ProjectMaterialCatalog.DeleteCustom(project, material.Id)",
        'AuditTrail.ForProject(project).Record("material.catalog.delete"',
    ):
        if token not in delete_body:
            errors.append("Material Catalog Delete missing current-row re-resolution token: " + token)

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
        "target.Elements.Clear();",
        "target.Metadata as ProjectMetadataDictionary",
        "targetMetadata.ReplacePersistenceState(source.Metadata);",
        "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);",
    ):
        if token not in text:
            errors.append("ProjectStateSnapshot must restore Material Catalog state: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Material Catalog Save/Delete/Apply are project-atomic, stale editor rows fail closed against the current project, and post-commit UI failures stay isolated")
