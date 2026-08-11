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
    marker = f'[CommandMethod("{command}"'
    start = text.find(marker)
    if start < 0:
        errors.append(f"{filename}: missing {command}")
        continue
    next_command = text.find("[CommandMethod(", start + len(marker))
    body = text[start:next_command if next_command >= 0 else len(text)]

    bind_token = "ExistingProjectMutationContext.TryGet(document, out var project)"
    read_only_token = "ProjectContextCoordinator.TryGetReadOnly(document, out var project)"
    regen_token = "RegenerateDirty(project)"
    cancel_token = "if (dialog.ShowDialog() != true) return;"

    if bind_token not in body:
        errors.append(f"{filename}: regeneration export must bind canonical existing project")
    if read_only_token in body:
        errors.append(f"{filename}: export must not regenerate a detached read-only project")
    if "ProjectContextCoordinator.GetOrCreate(document)" in body:
        errors.append(f"{filename}: export must not create replacement project state")
    if regen_token not in body:
        errors.append(f"{filename}: expected semantic regeneration before export")

    cancel = body.find(cancel_token)
    bind = body.find(bind_token)
    regen = body.find(regen_token)
    if min(cancel, bind, regen) < 0 or not cancel < bind < regen:
        errors.append(f"{filename}: lifecycle order must be dialog cancel -> canonical bind -> regenerate")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: regeneration-based CSV/XLSX export methods bind the canonical existing project after dialog confirmation without constraining separate read-only Show paths.")
