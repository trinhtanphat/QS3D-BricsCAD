#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs"
errors = []

for path in (COMMAND, WINDOW):
    if not path.is_file():
        errors.append("missing Material Catalog lifecycle file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DMATERIALS"',
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "new MaterialCatalogWindow(document, project)",
        "Application.ShowModelessWindow",
    ):
        if token not in text:
            errors.append("QS3DMATERIALS launcher missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("opening Material Catalog must not create/cache project state")

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ExistingProjectMutationContext.TryGet(_document, out var project)",
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project)",
    ):
        if token not in text:
            errors.append("MaterialCatalogWindow.xaml.cs missing lifecycle/rollback token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("modeless Material Catalog callbacks must not create/cache replacement project state after reload/unload")
    if text.count("ExistingProjectMutationContext.TryGet(_document, out var project)") < 3:
        errors.append("Material Catalog Save/Delete/Apply must bind canonical existing project state before mutation")

    for method, mutation in (
        ("private void OnSaveClick", "ProjectMaterialCatalog.UpsertCustom(project"),
        ("private void OnDeleteClick", "ProjectMaterialCatalog.DeleteCustom(project"),
        ("private void OnApplyClick", "element.SetProperty(target, material.Name)"),
    ):
        start = text.find(method)
        bind = text.find("ExistingProjectMutationContext.TryGet(_document, out var project)", start)
        mutate = text.find(mutation, bind)
        if min(start, bind, mutate) < 0 or not start < bind < mutate:
            errors.append(method + " must bind canonical existing project before mutation")

    refresh = text.find("private void RefreshAll")
    read_only = text.find("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", refresh)
    if min(refresh, read_only) < 0 or not refresh < read_only:
        errors.append("Material Catalog RefreshAll must resolve project read-only")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Material Catalog open is non-creating and binds the canonical existing project; read-only refresh stays observational; Save/Delete/Apply revalidate existing state and retain rollback")
