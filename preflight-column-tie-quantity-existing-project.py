#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ColumnTieQuantityCommands.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

for token in (
    'CommandMethod("QS3DREBARTIEQTY"',
    "if (selected.Count == 0) return;",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "lệnh không tạo project mới từ selection",
    "ColumnTieProjectQuantityService.Calculate",
    "ProjectStateSnapshot.Capture(project)",
    "snapshot.Restore(project)",
    "FinalizeUi(document, message)",
):
    if token not in source:
        errors.append("Column Tie Quantity lifecycle missing token: " + token)

if "ProjectContextCoordinator.GetOrCreate(document)" in source:
    errors.append("Column Tie Quantity must not create/cache an empty project from a CAD selection")

selection = source.find("if (selected.Count == 0) return;")
lookup = source.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
snapshot = source.find("ProjectStateSnapshot.Capture(project)")
finalize = source.find("FinalizeUi(document, message)")
if min(selection, lookup, snapshot, finalize) >= 0 and not selection < lookup < snapshot < finalize:
    errors.append("Tie Quantity lifecycle must remain selection -> existing project -> semantic snapshot -> best-effort UI")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DREBARTIEQTY requires an existing tracked QS3D project after non-empty selection and preserves semantic rollback/UI isolation.")
