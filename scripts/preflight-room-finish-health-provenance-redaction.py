#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RoomFinishHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "AutoRoomLifecycle.ResolveRoomReferenceId(project, finish)",
        "catch (InvalidOperationException)",
        '"ROOM_PROVENANCE_CONFLICT"',
        "HealthSeverity.Error",
        '"HT_Phòng có Room provenance mâu thuẫn và không thể phân giải an toàn. Cần sửa Room provenance trước khi quantity/release."',
        '"UNLINKED_ROOM_FINISH"',
        '"AMBIGUOUS_ROOM_FINISH_PARENT"',
        '"ORPHAN_ROOM_FINISH"',
        '"ROOM_FINISH_SCOPE_MISMATCH"',
        '"DUPLICATE_ROOM_FINISH"',
    )
    for token in required:
        if token not in text:
            errors.append("missing Room Finish health provenance/redaction token: " + token)

    forbidden = (
        "catch (InvalidOperationException ex)",
        "+ ex.Message",
        "ex.Message +",
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "project.Touch(",
        ".Save(",
        ".Erase(",
    )
    for token in forbidden:
        if token in text:
            errors.append("Room Finish health regressed provenance redaction/read-only contract: " + token)

print("QS3D Room Finish health provenance-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room Finish provenance conflicts remain fail-visible without raw resolver exception detail.")
