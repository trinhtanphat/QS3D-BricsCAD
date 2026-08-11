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
    project = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)", confirmed + 1)
    snapshot = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)", project + 1)
    regen = text.find("RegenerateDirty(snapshot)", snapshot + 1)
    build = text.find("ProjectRebarScheduleBuilder.Build(snapshot)", regen + 1)
    export = text.find("RebarCsvExporter.Export(dialog.FileName, rows)", build + 1)

    if min(dialog, confirmed, project, snapshot, regen, build, export) < 0:
        errors.append("BbsCsvCommands.cs missing save/read-only-project/detached-regenerate/build/export contract token")
    elif not dialog < confirmed < project < snapshot < regen < build < export:
        errors.append("BBS CSV must confirm Save before read-only lookup, detached regeneration, fresh schedule build, and export")

    pre_confirm = text[:confirmed if confirmed >= 0 else 0]
    for forbidden in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
    ):
        if forbidden in pre_confirm:
            errors.append("BBS CSV Cancel path must not execute before Save confirmation: " + forbidden)

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

print("PASS: BBS CSV confirms the destination, resolves existing state read-only, regenerates/builds a detached snapshot, exports fresh rows, and isolates post-export UI reporting.")
