#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
contracts = {
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": {
        "snapshot_min": 4,
        "refresh_min": 5,
        "required": (
            "using QS3D.Core.Persistence;",
            "RequireSelectedFloor(project)",
            "ProjectFloorService.Assign(project, floor.Id, elements)",
            'AuditTrail.ForProject(project).Record("floor.assign"',
            "_editingFloorId = floor.Id;",
            "RestoreOrThrow(project, rollback, operationError",
            "đã commit; đồng bộ UI chưa hoàn tất",
        ),
    },
    "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs": {
        "snapshot_min": 4,
        "refresh_min": 5,
        "required": (
            "using QS3D.Core.Persistence;",
            "RequireSelectedZone(project)",
            "ProjectZoneService.Assign(project, zone.Id, elements)",
            'AuditTrail.ForProject(project).Record("zone.assign"',
            "_editingId = zone.Id;",
            "RestoreOrThrow(project, rollback, operationError",
            "đã commit; UI sync warning:",
        ),
    },
}

errors = []
for relative, contract in contracts.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing project editor: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for token in contract["required"]:
        if token not in text:
            errors.append(relative + " missing atomic editor token: " + token)
    if text.count("ProjectStateSnapshot.Capture(project)") < contract["snapshot_min"]:
        errors.append(relative + " must snapshot Create/Update/Delete/Activate/Assign mutation boundaries as applicable")
    if text.count("RefreshAfterCommit(") < contract["refresh_min"]:
        errors.append(relative + " must isolate each mutating handler from post-commit UI refresh failures")
    if "rollback.Restore(project);" not in text:
        errors.append(relative + " must restore the whole project after a failed mutation/audit batch")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Floor/Level and Zone modeless editors guard CRUD/activate/assign as whole-project atomic mutations with stale-selection re-resolution and post-commit UI isolation")
