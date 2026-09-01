#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "MaterialCatalogCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "MaterialCatalogWindow.xaml.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing MaterialCatalogCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DMATERIALS"',
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "new MaterialCatalogWindow(document, project)",
        "private static PublishedManager? _pending;",
        "_pending = reserved;",
        "Application.ShowModelessWindow",
        "ReferenceEquals(_pending, reserved)",
        "_published = reserved;",
    ):
        if token not in text:
            errors.append("Material Catalog launcher missing token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("opening Material Catalog must not create/cache project state")
    if '"QS3DMATERIALS lỗi: " + ex.Message' in text:
        errors.append("Material Catalog launcher must not surface raw exception text")

    if not errors:
        reserve_at = text.index("_pending = reserved;")
        show_at = text.index("Application.ShowModelessWindow", reserve_at)
        loaded_at = text.index("if (!window.IsLoaded)", show_at)
        owner_at = text.index("if (!ReferenceEquals(_pending, reserved))", loaded_at)
        publish_at = text.index("_published = reserved;", owner_at)
        if not (reserve_at < show_at < loaded_at < owner_at < publish_at):
            errors.append("Material Catalog publication must remain reserve -> host show -> loaded -> exact owner -> publish")

if not WINDOW.is_file():
    errors.append("missing MaterialCatalogWindow.xaml.cs")
else:
    text = WINDOW.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly" not in text:
        errors.append("Material Catalog window must resolve display state read-only")
    if "ExistingProjectMutationContext.TryGet" not in text:
        errors.append("Material Catalog write actions must bind canonical existing project state")
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("Material Catalog modeless window must not create/cache project state")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: opening Material Catalog is non-creating, pending-first, exact-owner published, redacted, and binds one canonical existing project identity.")
