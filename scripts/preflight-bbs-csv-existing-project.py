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
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "export không tạo project mới",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RegenerateDirty(snapshot)",
    "ProjectRebarScheduleBuilder.Build(snapshot)",
    "rows.Count == 0",
    "var totals = RebarScheduleBuilder.CalculateTotals(rows);",
    "var dialog = new SaveFileDialog",
    "if (dialog.ShowDialog() != true) return;",
    "RebarCsvExporter.Export(dialog.FileName, rows)",
    "FinalizeUi(document, status)",
):
    if token not in source:
        errors.append("BBS CSV lifecycle missing token: " + token)

if "ProjectContextCoordinator.GetOrCreate(document)" in source:
    errors.append("BBS CSV export must not create/cache an empty QS3D project")
if "ExistingProjectMutationContext" in source:
    errors.append("BBS CSV export must stay read-only and must not bind mutation context")
if "RegenerateDirty(project)" in source:
    errors.append("BBS CSV export must not regenerate the live/read-only project")
if "ProjectRebarScheduleBuilder.Build(project)" in source:
    errors.append("BBS CSV export must build from the detached snapshot, not the live/read-only project")
if 'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS CSV total weight")' in source:
    errors.append("BBS CSV export must not restore pairwise status aggregation after canonical validation")

lookup = source.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
snapshot = source.find("ProjectStateSnapshot.CreateDetachedCopy(project)", lookup + 1)
regenerate = source.find("RegenerateDirty(snapshot)", snapshot + 1)
build = source.find("ProjectRebarScheduleBuilder.Build(snapshot)", regenerate + 1)
rows = source.find("rows.Count == 0", build + 1)
aggregate = source.find("var totals = RebarScheduleBuilder.CalculateTotals(rows);", rows + 1)
dialog = source.find("var dialog = new SaveFileDialog", aggregate + 1)
confirm = source.find("if (dialog.ShowDialog() != true) return;", dialog + 1)
export = source.find("RebarCsvExporter.Export(dialog.FileName, rows)", confirm + 1)
finalize = source.find("FinalizeUi(document, status)", export + 1)
if min(lookup, snapshot, regenerate, build, rows, aggregate, dialog, confirm, export, finalize) < 0:
    errors.append("BBS CSV lifecycle is missing an existing-project/detached-validation/canonical-aggregate/save/export stage")
elif not lookup < snapshot < regenerate < build < rows < aggregate < dialog < confirm < export < finalize:
    errors.append("BBS CSV lifecycle order must be existing project -> detached copy -> regenerate -> build/validate -> canonical aggregate -> Save dialog -> confirm -> export -> best-effort UI")

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

print("PASS: QS3DBBSCSV validates an existing project on a detached regenerated snapshot, validates canonical compensated totals before SaveFileDialog, writes only after confirmation, and isolates UI failures after export.")
