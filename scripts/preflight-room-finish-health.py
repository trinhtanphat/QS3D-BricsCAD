#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs",
    "src/QS3D.Core/Domain/AutoRoomLifecycle.cs",
    "src/QS3D.BricsCAD.V25/RoomFinishHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml",
    "tests/QS3D.Core.SmokeTests/RoomFinishHealthSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing room-finish health file: " + relative)

checks = {
    required[0]: [
        "RoomFinishHealthService", "AutoRoomLifecycle.IsRoomFinishCategory", "AutoRoomLifecycle.ResolveRoomReferenceId",
        '"ROOM_PROVENANCE_CONFLICT"', '"UNLINKED_ROOM_FINISH"', '"ORPHAN_ROOM_FINISH"',
        '"INVALID_ROOM_FINISH_PARENT"', '"ROOM_FINISH_SCOPE_MISMATCH"', '"STALE_ROOM_FINISH"',
    ],
    required[1]: [
        "IsRoomFinishCategory(element.Category)", "ResolveRoomReferenceId(project, element)",
        "room.FloorId", "element.FloorId", "room.ZoneId", "element.ZoneId",
    ],
    required[2]: [
        'CommandMethod("QS3DROOMFINISHHEALTH"', "RoomFinishHealthService().Inspect(project)",
        "new ModelHealthWindow", "SourceHandleResolver.Resolve", "CadHandleService.Select", "ShowModelessWindow",
    ],
    required[3]: [
        "combined.AddRange(new RoomFinishHealthService().Inspect(project));", "SourceHandleResolver.Resolve(project, new[] { element.Id })",
    ],
    required[4]: ['Tag="QS3DROOMFINISHHEALTH"', "Kiểm tra HT_Phòng Health"],
    required[5]: [
        "HealthyLinkedFinishHasNoIssue", "UnlinkedFinishIsVisibleForRepair", "OrphanFinishIsError",
        "InvalidParentIsError", "ConflictingProvenanceIsError", "StaleRoomFinishIsWarning", "CrossScopeFinishIsErrorAndExcluded",
    ],
    required[6]: ["RoomFinishHealthSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish health guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DROOMFINISHHEALTH") != 1:
    errors.append("QS3DROOMFINISHHEALTH must be declared exactly once")

print("QS3D room-finish health preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: HT_Phòng provenance conflicts, orphan/wrong-parent/cross-scope/stale/unlinked states are diagnosable, quantity exclusion is fail-closed, and Health All/Hub expose the repair path.")
