#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    dialog = text.find("var dialog = new SaveFileDialog")
    confirmed = text.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
    project = text.find("ProjectContextCoordinator.GetOrCreate(document)")
    regen = text.find("RegenerateDirty(project)", project + 1)
    build = text.find("ProjectRebarScheduleBuilder.Build(project)", regen + 1)
    export = text.find("RebarCsvExporter.Export(dialog.FileName, rows)", build + 1)

    if min(dialog, confirmed, project, regen, build, export) < 0:
        errors.append("BbsCsvCommands.cs missing save/current-project/regenerate/build/export contract token")
    elif not dialog < confirmed < project < regen < build < export:
        errors.append("BBS CSV must confirm Save before current-project lookup, regeneration, fresh schedule build, and export")

    pre_dialog = text[:confirmed if confirmed >= 0 else 0]
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "RegenerateDirty(project)",
        "ProjectRebarScheduleBuilder.Build(project)",
    ):
        if forbidden in pre_dialog:
            errors.append("BBS CSV Cancel path must not execute before Save confirmation: " + forbidden)

    if "FinalizeUi(document, status);" not in text:
        errors.append("BBS CSV post-export UI reporting must remain isolated through FinalizeUi")

print("QS3D BBS CSV export-freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BBS CSV confirms the destination before any project/regeneration work, then rebuilds from the current project and isolates post-export UI reporting.")
