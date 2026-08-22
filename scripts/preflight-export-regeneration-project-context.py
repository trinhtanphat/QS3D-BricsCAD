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
    if "ExistingProjectMutationContext.TryGet(document, out var project)" not in text:
        errors.append(f"{filename}: regeneration export must bind canonical existing project")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" in text:
        errors.append(f"{filename}: export must not regenerate a detached read-only project")
    if "RegenerateDirty(project)" not in text:
        errors.append(f"{filename}: expected semantic regeneration before export")
    cancel = text.find("if (dialog.ShowDialog() != true) return;")
    bind = text.find("ExistingProjectMutationContext.TryGet(document, out var project)")
    regen = text.find("RegenerateDirty(project)")
    if min(cancel, bind, regen) < 0 or not cancel < bind < regen:
        errors.append(f"{filename}: lifecycle order must be dialog cancel -> canonical bind -> regenerate")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: regeneration-based CSV/XLSX exports bind the canonical existing project after dialog confirmation.")
