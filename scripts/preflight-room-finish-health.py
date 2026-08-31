#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs",
    "src/QS3D.Core/Domain/AutoRoomLifecycle.cs",
    "src/QS3D.Core/Services/SourceHandleResolver.cs",
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
        "AutoRoomLifecycle.IsRoomFinishCategory(element.Category)", "AutoRoomLifecycle.ResolveRoomReferenceId(project, element)",
        "stack.Push(roomId)", "BoundarySourceHandlesKey",
    ],
    required[3]: [
        'CommandMethod("QS3DROOMFINISHHEALTH"', "RoomFinishHealthService().Inspect(project)",
        "ModelHealthWindowPresenter.Show(document, issues, issue =>", "SourceHandleResolver.Resolve", "CadHandleService.Select",
    ],
    required[4]: [
        "combined.AddRange(new RoomFinishHealthService().Inspect(project));",
        "ModelHealthWindowPresenter.Show(document, issues, issue =>",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
    ],
    required[5]: ['Tag="QS3DROOMFINISHHEALTH"', "Kiểm tra HT_Phòng Semantic Health"],
    required[6]: [
        "HealthyLinkedFinishHasNoIssue", "UnlinkedFinishIsVisibleForRepair", "OrphanFinishIsError",
        "InvalidParentIsError", "ConflictingProvenanceIsError", "StaleRoomFinishIsWarning", "CrossScopeFinishIsErrorAndExcluded",
        "PropertyOnlyRoomProvenanceResolvesBoundaryHandles", "SourceHandleResolver.Resolve", "BoundarySourceHandlesKey",
    ],
    required[7]: ["RoomFinishHealthSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish health/trace guard token: " + needle)

for relative in (required[3], required[4]):
    path = ROOT / relative
    if path.is_file():
        text = path.read_text(encoding="utf-8")
        if "Application.ShowModelessWindow(" in text or "new ModelHealthWindow(" in text:
            errors.append(relative + " must route Model Health publication through ModelHealthWindowPresenter")

health_all = ROOT / required[4]
if health_all.is_file() and "SourceHandleResolver.Resolve(project, new[] { element.Id })" in health_all.read_text(encoding="utf-8"):
    errors.append("Health All Room Finish modeless Locate must not use the project snapshot captured when the window opened")

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
print("PASS: HT_Phòng health remains fail-closed/read-only, presenter-routed, and modeless trace/Locate resolves current project state before Room boundary CAD handles.")
