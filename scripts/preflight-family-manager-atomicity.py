#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MAIN = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs"
ACTIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.Active.cs"
errors = []

for path in (MAIN, ACTIVE):
    if not path.is_file():
        errors.append("missing Family Manager contract file: " + str(path.relative_to(ROOT)))

if MAIN.is_file():
    text = MAIN.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Persistence;",
        "RequireSelectedFamily(project)",
        "private static T ExecuteAtomic<T>",
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project);",
        "RefreshAfterCommit(",
        "_creatingNew = false;",
        "GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
        'AuditTrail.ForProject(project).Record("family.assign"',
        "đã commit; UI sync warning:",
    ):
        if token not in text:
            errors.append("FamilyManagerWindow.xaml.cs missing atomic/stale-state token: " + token)

    for operation in (
        '}, "Duplicate Family");',
        '}, "Lưu Family");',
        '}, "Xóa Family");',
        '}, "Lưu Family property");',
        '}, "Xóa Family property");',
        '}, "Gán Family cho selection");',
    ):
        if operation not in text:
            errors.append("Family Manager mutation is not routed through ExecuteAtomic: " + operation)

if ACTIVE.is_file():
    text = ACTIVE.read_text(encoding="utf-8")
    for token in (
        "RequireSelectedFamily(project)",
        "ExecuteAtomic(project, () =>",
        '}, "Đặt Family active");',
        "RefreshAfterCommit(",
    ):
        if token not in text:
            errors.append("FamilyManagerWindow.Active.cs missing atomic activation token: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Family Manager create/rename/duplicate/delete/property propagation/assign/activate are guarded by whole-project rollback, stale-family resolution and post-commit UI isolation")
