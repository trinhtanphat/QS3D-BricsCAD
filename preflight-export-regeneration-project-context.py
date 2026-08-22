#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"

EXPORTS = {
    "BbsCsvCommands.cs": "QS3DBBSCSV",
    "CurtainWallScheduleCommands.cs": "QS3DCURTAINXLSX",
    "DoorOpeningScheduleCommands.cs": "QS3DDOORXLSX",
    "MaterialUsageScheduleCommands.cs": "QS3DMATERIALXLSX",
    "RoomFinishScheduleCommands.cs": "QS3DFINISHXLSX",
}

errors = []
for filename, command in EXPORTS.items():
    path = SRC / filename
    if not path.is_file():
        errors.append(f"missing {filename}")
        continue
    text = path.read_text(encoding="utf-8")
    if f'CommandMethod("{command}"' not in text:
        errors.append(f"{filename}: missing {command}")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(f"{filename}: export must require an existing project without binding/creating live state")
    if "ProjectStateSnapshot.CreateDetachedCopy(project)" not in text:
        errors.append(f"{filename}: export must create a detached project snapshot")
    if "RegenerateDirty(snapshot)" not in text:
        errors.append(f"{filename}: export must regenerate only detached state")
    if "ExistingProjectMutationContext" in text:
        errors.append(f"{filename}: pure export must not promote read-only state to mutation context")
    if "RegenerateDirty(project)" in text:
        errors.append(f"{filename}: pure export must not mutate the live/read-only project")

    cancel = text.find("if (dialog.ShowDialog() != true) return;")
    lookup = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    snapshot = text.find("ProjectStateSnapshot.CreateDetachedCopy(project)")
    regen = text.find("RegenerateDirty(snapshot)")
    if min(cancel, lookup, snapshot, regen) < 0 or not cancel < lookup < snapshot < regen:
        errors.append(f"{filename}: lifecycle order must be dialog cancel -> read-only lookup -> detached copy -> regenerate")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: regeneration-based CSV/XLSX exports regenerate detached snapshots and never mutate/bind live project state.")
