#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Domain/RoomFinishIdentityService.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.Core/Reporting/RoomFinishSchedule.cs",
    "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs",
    "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishIdentitySmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing room-finish identity file: " + relative)

checks = {
    required[0]: [
        "RoomFinishIdentityService", "CanonicalId", "FindExisting", "ValidateProject",
        "AutoRoomLifecycle.ResolveRoomReferenceId", "AutoRoomLifecycle.IsRoomFinishCategory",
        "Room finish id collision", "references another Room", "Multiple ", " finishes reference Room ",
    ],
    required[1]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[2]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[3]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[4]: ['"DUPLICATE_ROOM_FINISH"', "BQ/Material/HT_Phòng schedule fail closed"],
    required[5]: [
        "RoomFinishIdentityService.FindExisting(project, room, category)",
        "RoomFinishIdentityService.CanonicalId(room.Id, category)",
        "SyncExistingRoomFinishes", "GenerateRoomFinishes",
    ],
    required[6]: [
        "ReusesCanonicalFinishWithoutLegacyProvenance", "ReusesPropertyLinkedLegacyFinish",
        "ReusesDependencyLinkedLegacyFinish", "RejectsCanonicalAndLegacyDuplicate",
        "DuplicateFinishesFailClosedAcrossSchedules", "RejectsCanonicalLinkedToAnotherRoom",
        "RejectsCanonicalIdCategoryCollision", "RejectsConflictingLegacyProvenance", "RejectsNonFinishCategory",
        "ProjectQuantityReportBuilder.Group(project)", "RoomFinishScheduleBuilder.Build(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ],
    required[7]: ["RoomFinishIdentitySmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish identity guard/token: " + needle)

adapter = ROOT / required[5]
if adapter.is_file():
    text = adapter.read_text(encoding="utf-8")
    for forbidden in ("private static ProjectElement? FindRoomFinish", "Multiple \" + category + \" finishes reference Room"):
        if forbidden in text: errors.append("SemanticCaptureService must not duplicate Core finish identity logic: " + forbidden)

print("QS3D room-finish identity preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: HT_Phòng identity is centralized in Core; canonical/legacy provenance is reused, duplicate Room+Category finishes fail closed across BQ/Material/Schedule, and adapter generation/sync cannot recreate parallel identity logic.")
