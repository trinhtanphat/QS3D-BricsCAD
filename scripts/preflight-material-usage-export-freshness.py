#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    dialog = text.find("var dialog = new SaveFileDialog")
    confirmed = text.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
    project = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)", confirmed + 1)
    snapshot = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)", project + 1)
    regen = text.find("RegenerateDirty(snapshot)", snapshot + 1)
    build = text.find("MaterialUsageScheduleBuilder.Build(snapshot)", regen + 1)
    export = text.find("MaterialUsageXlsxExporter.Export(dialog.FileName, rows)", build + 1)

    if min(dialog, confirmed, project, snapshot, regen, build, export) < 0:
        errors.append("MaterialUsageScheduleCommands.cs missing save/read-only-project/detached-regenerate/build/export contract token")
    elif not dialog < confirmed < project < snapshot < regen < build < export:
        errors.append("Material Usage XLSX must confirm Save before read-only lookup, detached regeneration, fresh schedule build, and export")

    pre_confirm = text[:confirmed if confirmed >= 0 else 0]
    for forbidden in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "MaterialUsageScheduleBuilder.Build(snapshot)",
    ):
        if forbidden in pre_confirm:
            errors.append("Material Usage Cancel path must not execute before Save confirmation: " + forbidden)

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("Material Usage read-only export must not mutate/bind the live project: " + forbidden)

    for token in (
        "FinalizeUi(document, status, dialog.FileName);",
        "private static void Report(Document document, string status)",
        "try { PaletteCoordinator.SetStatus(status); } catch { }",
    ):
        if token not in text:
            errors.append("Material Usage post-export/error UI isolation missing token: " + token)

print("QS3D Material Usage export-freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Material Usage XLSX confirms the destination, resolves existing state read-only, regenerates/builds a detached snapshot, exports fresh rows, and isolates UI reporting.")
