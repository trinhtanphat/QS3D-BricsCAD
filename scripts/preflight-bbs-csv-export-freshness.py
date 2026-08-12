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
    project = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    snapshot = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)", project + 1)
    regen = text.find("RegenerateDirty(snapshot)", snapshot + 1)
    build = text.find("ProjectRebarScheduleBuilder.Build(snapshot)", regen + 1)
    rows = text.find("rows.Count == 0", build + 1)
    total = text.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS CSV total weight")', rows + 1)
    dialog = text.find("var dialog = new SaveFileDialog", total + 1)
    confirmed = text.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
    export = text.find("RebarCsvExporter.Export(dialog.FileName, rows)", confirmed + 1)
    finalize = text.find("FinalizeUi(document, status);", export + 1)

    if min(project, snapshot, regen, build, rows, total, dialog, confirmed, export, finalize) < 0:
        errors.append("BbsCsvCommands.cs missing read-only-project/detached-regenerate/build-validation/save/export contract token")
    elif not project < snapshot < regen < build < rows < total < dialog < confirmed < export < finalize:
        errors.append("BBS CSV must validate fresh detached exportability before SaveFileDialog, then export only after Save confirmation")

    pre_confirm = text[:confirmed if confirmed >= 0 else 0]
    if "RebarCsvExporter.Export(" in pre_confirm:
        errors.append("BBS CSV must not write the export before Save confirmation")

    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "ProjectRebarScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("BBS CSV read-only export must not mutate/bind the live project: " + forbidden)

    if "FinalizeUi(document, status);" not in text:
        errors.append("BBS CSV post-export UI reporting must remain isolated through FinalizeUi")

print("QS3D BBS CSV export-freshness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BBS CSV resolves existing state read-only, regenerates/builds and validates a detached snapshot before SaveFileDialog, writes only after confirmation, and isolates post-export UI reporting.")
