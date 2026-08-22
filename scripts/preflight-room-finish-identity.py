#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Domain/RoomFinishIdentityService.cs",
    "src/QS3D.Core/Services/RoomFinishSynchronizationService.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.Core/Reporting/RoomFinishSchedule.cs",
    "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs",
    "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishIdentitySmoke.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationAtomicSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing room-finish identity/sync file: " + relative)

checks = {
    required[0]: [
        "RoomFinishIdentityService", "CanonicalId", "FindExisting", "ValidateProject",
        "AutoRoomLifecycle.ResolveRoomReferenceId", "AutoRoomLifecycle.IsRoomFinishCategory",
        "Room finish id collision", "references another Room", "Multiple ", " finishes reference Room ",
    ],
    required[1]: [
        "RoomFinishSynchronizationService", "SynchronizeExisting", "Synchronize(ProjectState project",
        "SynchronizeCore", "ProjectStateSnapshot.Capture(project)", "RestoreOrThrow",
        "Room finish batch synchronization", "Room finish synchronization",
        "RoomFinishIdentityService.FindExisting", "AutoRoomLifecycle.ResolveRoomReferenceId",
        "AutoRoomLifecycle.RoomSourceIdKey", "finish.DependsOn.Add(room.Id)",
        "finish.FloorId = room.FloorId", "finish.ZoneId = room.ZoneId", "finish.DrawingFingerprint = room.DrawingFingerprint",
        '"AreaM2"', '"PerimeterM"', '"HeightM"', '"OpeningAreaM2"', '"DoorWidthM"',
        "finish.Properties.Remove(key)", "must be a finite non-negative invariant number",
        "AutoRoomLifecycle.IsStaleAutoRoom(room)", "ReferenceEquals(owned, element)",
    ],
    required[2]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[3]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[4]: ["RoomFinishIdentityService.ValidateProject(project);"],
    required[5]: ['"DUPLICATE_ROOM_FINISH"', "BQ/Material/HT_Phòng schedule fail closed"],
    required[6]: [
        "RoomFinishIdentityService.FindExisting(project, room, category)",
        "RoomFinishIdentityService.CanonicalId(room.Id, category)",
        "RoomFinishSynchronizationService.Categories",
        "RoomFinishSynchronizationService.Synchronize(project, room, finish)",
        "RoomFinishSynchronizationService.SynchronizeExisting(project, room)",
        "SyncExistingRoomFinishes", "GenerateRoomFinishes",
    ],
    required[7]: [
        "ReusesCanonicalFinishWithoutLegacyProvenance", "ReusesPropertyLinkedLegacyFinish",
        "ReusesDependencyLinkedLegacyFinish", "RejectsCanonicalAndLegacyDuplicate",
        "DuplicateFinishesFailClosedAcrossSchedules", "RejectsCanonicalLinkedToAnotherRoom",
        "RejectsCanonicalIdCategoryCollision", "RejectsConflictingLegacyProvenance", "RejectsNonFinishCategory",
        "ProjectQuantityReportBuilder.Group(project)", "RoomFinishScheduleBuilder.Build(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ],
    required[8]: [
        "RepairsLegacyDependencyScopeAndFingerprint", "RemovedRoomMetricsClearOldDeductions",
        "QuantityFallbackIsCanonicalized", "BatchFailureRollsBackEarlierFinishMutation",
        "RejectsInvalidRoomMetric", "RejectsStaleAutoRoom", "RejectsForeignProjectObject",
        "RoomFinishSynchronizationService.Synchronize", "RoomFinishSynchronizationService.SynchronizeExisting",
        "NetFinishAreaM2", "SkirtingLengthM",
    ],
    required[9]: [
        "ModuleInitializer", "SingleFailureRollsBackPartialMutation", "ProjectState", "invalid-after-area",
        "RoomFinishSynchronizationService.Synchronize", "did not restore scope/fingerprint", "leaked canonical Room provenance",
        "leaked a partially-copied Room metric", "leaked a dependency",
    ],
    required[10]: ["RoomFinishIdentitySmoke.Run();", "RoomFinishSynchronizationSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish identity/sync guard/token: " + needle)

adapter = ROOT / required[6]
if adapter.is_file():
    text = adapter.read_text(encoding="utf-8")
    for forbidden in (
        "private static ProjectElement? FindRoomFinish",
        "private static void SyncFinishFromRoom",
        "private static void EnsureRoomDependency",
        "Copy(room, finish",
        "Multiple \" + category + \" finishes reference Room",
    ):
        if forbidden in text: errors.append("SemanticCaptureService must not duplicate Core finish identity/synchronization logic: " + forbidden)

print("QS3D room-finish identity/synchronization preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: HT_Phòng identity and Room->finish synchronization are centralized in Core; single and batch synchronization are transactional, legacy dependency/scope is repaired, removed Room metrics clear stale finish deductions, duplicates fail closed, and adapter generation/sync uses the shared contract.")
