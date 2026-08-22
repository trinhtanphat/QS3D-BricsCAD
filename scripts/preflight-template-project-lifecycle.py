#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "TemplateCommands.cs"
errors = []

if not COMMAND.is_file():
    errors.append("missing TemplateCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    export_start = text.find('CommandMethod("QS3DTEMPLATEEXPORT"')
    import_start = text.find('CommandMethod("QS3DTEMPLATEIMPORT"')
    if export_start < 0 or import_start <= export_start:
        errors.append("cannot isolate template export/import command regions")
    else:
        export = text[export_start:import_start]
        imp = text[import_start:]
        for token in (
            "if (dialog.ShowDialog() != true) return;",
            "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
            "store.ExportProject(project",
        ):
            if token not in export:
                errors.append("template export missing lifecycle token: " + token)
        if "ProjectContextCoordinator.GetOrCreate(doc)" in export:
            errors.append("template export must not create/cache project state")
        if "ProjectContextCoordinator.GetOrCreate(doc)" not in imp:
            errors.append("template import bootstrap semantics changed unexpectedly; import may intentionally initialize a project")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: template export is cancel-first/read-only while template import preserves intentional project-bootstrap semantics.")
