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
    project = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    regen = text.find("RegenerateDirty(project)", project + 1)
    build = text.find("MaterialUsageScheduleBuilder.Build(project)", regen + 1)
    export = text.find("MaterialUsageXlsxExporter.Export(dialog.FileName, rows)", build + 1)

    if min(dialog, confirmed, project, regen, build, export) < 0:
        errors.append("MaterialUsageScheduleCommands.cs missing save/current-project/regenerate/build/export contract token")
    elif not dialog < confirmed < project < regen < build < export:
        errors.append("Material Usage XLSX must confirm Save before current-project lookup, regeneration, fresh schedule build, and export")

    pre_confirm = text[:confirmed if confirmed >= 0 else 0]
    for forbidden in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "RegenerateDirty(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ):
        if forbidden in pre_confirm:
            errors.append("Material Usage Cancel path must not execute before Save confirmation: " + forbidden)

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

print("PASS: Material Usage XLSX confirms the destination before project/regeneration work, rebuilds from the current project, and isolates UI reporting.")
