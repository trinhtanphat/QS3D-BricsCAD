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
    if "ProjectContextCoordinator.GetOrCreate(document);" not in text:
        errors.append("explicit QS3DMATERIALS entry point must initialize the authoring project before opening the modeless catalog")
    if "new MaterialCatalogWindow(document)" not in text:
        errors.append("QS3DMATERIALS must keep binding the catalog to its source Document")

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project)",
    ):
        if token not in text:
            errors.append("MaterialCatalogWindow.xaml.cs missing lifecycle/rollback token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("modeless Material Catalog callbacks must not create/cache replacement project state after reload/unload")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Material Catalog creates project only on explicit open; modeless callbacks re-resolve existing state and retain rollback")
