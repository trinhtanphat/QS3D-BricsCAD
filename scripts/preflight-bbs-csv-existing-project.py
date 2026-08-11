#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing BbsCsvCommands.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

for token in (
    'CommandMethod("QS3DBBSCSV"',
    "if (dialog.ShowDialog() != true) return;",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "export không tạo project mới",
    "RegenerateDirty(project)",
    "ProjectRebarScheduleBuilder.Build(project)",
    "RebarCsvExporter.Export(dialog.FileName, rows)",
    "FinalizeUi(document, status)",
):
    if token not in source:
        errors.append("BBS CSV lifecycle missing token: " + token)

if "ProjectContextCoordinator.GetOrCreate(document)" in source:
    errors.append("BBS CSV export must not create/cache an empty QS3D project")

cancel = source.find("if (dialog.ShowDialog() != true) return;")
lookup = source.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
regenerate = source.find("RegenerateDirty(project)")
export = source.find("RebarCsvExporter.Export(dialog.FileName, rows)")
finalize = source.find("FinalizeUi(document, status)")
if min(cancel, lookup, regenerate, export, finalize) >= 0 and not cancel < lookup < regenerate < export < finalize:
    errors.append("BBS CSV lifecycle order must be cancel -> existing project -> regenerate -> export -> best-effort UI")

finalize_start = source.find("private static void FinalizeUi")
if finalize_start >= 0:
    finalize_body = source[finalize_start:]
    if "Export has already committed; UI reporting is best effort only." not in finalize_body:
        errors.append("BBS CSV must preserve post-export UI failure isolation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DBBSCSV cancels before project lookup, requires an existing project, and isolates UI failures after export.")
