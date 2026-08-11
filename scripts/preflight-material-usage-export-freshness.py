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
    project_token = "ExistingProjectMutationContext.TryGet(document, out var project)"
    dialog = text.find("var dialog = new SaveFileDialog")
    confirmed = text.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
    project = text.find(project_token, confirmed + 1)
    regen = text.find("RegenerateDirty(project)", project + 1)
    build = text.find("MaterialUsageScheduleBuilder.Build(project)", regen + 1)
    export = text.find("MaterialUsageXlsxExporter.Export(dialog.FileName, rows)", build + 1)

    if min(dialog, confirmed, project, regen, build, export) < 0:
        errors.append("MaterialUsageScheduleCommands.cs missing save/canonical-project/regenerate/build/export contract token")
    elif not dialog < confirmed < project < regen < build < export:
        errors.append("Material Usage XLSX must confirm Save before canonical existing-project binding, regeneration, fresh schedule build, and export")

    pre_confirm = text[:confirmed if confirmed >= 0 else 0]
    for forbidden in (
        project_token,
        "RegenerateDirty(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ):
        if forbidden in pre_confirm:
            errors.append("Material Usage Cancel path must not execute before Save confirmation: " + forbidden)

    export_start = text.rfind("[CommandMethod(\"QS3DMATERIALXLSX\"", 0, confirmed if confirmed >= 0 else len(text))
    export_body = text[export_start:] if export_start >= 0 else text
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" in export_body:
        errors.append("Material Usage export regeneration must not mutate a detached read-only project")
    if "ProjectContextCoordinator.GetOrCreate(document)" in export_body:
        errors.append("Material Usage export must not create replacement project state")

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

print("PASS: Material Usage XLSX confirms the destination before canonical project regeneration, exports fresh rows without creating replacement project state, and isolates UI reporting.")
