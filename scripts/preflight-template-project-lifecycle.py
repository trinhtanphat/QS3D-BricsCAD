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
    import_end = text.find("private static void FinalizeExportUi", import_start)
    if export_start < 0 or import_start <= export_start or import_end <= import_start:
        errors.append("cannot isolate template export/import command regions")
    else:
        export = text[export_start:import_start]
        imp = text[import_start:import_end]
        for token in (
            "if (dialog.ShowDialog() != true) return;",
            "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
            "store.ExportProject(project",
        ):
            if token not in export:
                errors.append("template export missing lifecycle token: " + token)
        if "ProjectContextCoordinator.GetOrCreate(doc)" in export:
            errors.append("template export must not create/cache project state")

        for token in (
            "MessageBox.Show(confirmText",
            'ExistingProjectMutationContext.Require(doc, "Template Import")',
            "ProjectStateSnapshot.Capture(project)",
            "store.Apply(project, profile)",
            "RegenerateDirty(project)",
            "rollback.Restore(project)",
        ):
            if token not in imp:
                errors.append("template import missing existing-project lifecycle token: " + token)
        if "ProjectContextCoordinator.GetOrCreate(doc)" in imp:
            errors.append("template import must not create/cache a replacement project")

        confirm = imp.find("MessageBox.Show(confirmText")
        bind = imp.find('ExistingProjectMutationContext.Require(doc, "Template Import")')
        snapshot = imp.find("ProjectStateSnapshot.Capture(project)")
        apply = imp.find("store.Apply(project, profile)")
        regen = imp.find("RegenerateDirty(project)")
        restore = imp.find("rollback.Restore(project)")
        if min(confirm, bind, snapshot, apply, regen, restore) >= 0 and not confirm < bind < snapshot < apply < regen < restore:
            errors.append("template import must confirm before binding canonical existing project, snapshot before mutation, regenerate, and retain rollback coverage")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: template export is cancel-first/read-only and template import confirms before binding canonical existing project state with snapshot rollback coverage.")
