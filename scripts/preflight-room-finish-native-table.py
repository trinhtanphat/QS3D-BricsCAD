#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SHARED = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/RoomFinishNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/RoomFinishNativeTableCommands.cs"
SCHEDULE = ROOT / "src/QS3D.Core/Reporting/RoomFinishSchedule.cs"
AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
DOC = ROOT / "docs/NATIVE-ROOM-FINISH-TABLE-P0.md"
errors = []

for path in (SHARED, BUILDER, COMMANDS, SCHEDULE, AGGREGATOR, RELEASE, DOC):
    if not path.is_file():
        errors.append("missing Room Finish native Table file: " + str(path.relative_to(ROOT)))

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        '"RoomFinishSchedule"',
        '"RoomFinishTable"',
        '"GeneratedRoomFinishTable"',
        'RoomFinishScheduleBuilder.Build(project)',
        'ProjectOwnedNativeTableArtifactService.Build',
        'ProjectOwnedNativeTableArtifactService.Remove',
        'ProjectOwnedNativeTableArtifactService.Inspect',
        '"Phòng"',
        '"Vật liệu"',
        '"Khối lượng chính"',
        'row.PrimaryQuantity',
        '"ROOM_FINISH_" + x.Code',
    ):
        if token not in text:
            errors.append("RoomFinishNativeTableBuilder.cs missing authoritative adapter token: " + token)
    if 'SemanticDocumentationTableBuilder' in text or 'SemanticTagRenderer' in text:
        errors.append("Room Finish native Table must consume RoomFinishScheduleBuilder, not recalculate through generic semantic templates")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DFINISHTABLE"',
        '[CommandMethod("QS3DFINISHTABLEREFRESH"',
        '[CommandMethod("QS3DFINISHTABLEREMOVE"',
        '[CommandMethod("QS3DFINISHTABLEHEALTH"',
        'RoomFinishNativeTableBuilder.StoredPosition(project)',
        'RoomFinishNativeTableBuilder.Inspect(document, project)',
        'RequireModelSpace(document)',
        'RequireSupportedUcs(document)',
    ):
        if token not in text:
            errors.append("RoomFinishNativeTableCommands.cs missing command/scope token: " + token)

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    for token in (
        'RoomFinishIdentityService.ValidateProject(project)',
        'AutoRoomLifecycle.IsExcludedFromQuantity(project, element)',
        'AutoRoomLifecycle.ResolveRoomReferenceId(project, element)',
        'ProjectMaterialCatalog.GetAll(project)',
        'PrimaryQuantity',
    ):
        if token not in text:
            errors.append("RoomFinishSchedule.cs lost authoritative HT_Phòng schedule token: " + token)

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in ('QS3DDOC', 'ProjectStateSnapshot.Capture(project)', 'table.TextString(row, column)', 'OpenMode.ForRead', 'MaxDetailedCellIssues = 32'):
        if token not in text:
            errors.append("shared native Table service lost ownership/rollback/live-health token: " + token)

if AGGREGATOR.is_file() and 'RoomFinishNativeTableBuilder.Inspect(document, project)' not in AGGREGATOR.read_text(encoding="utf-8"):
    errors.append("runtime health aggregator must include Room Finish native Table health")
if RELEASE.is_file() and 'GeneratedSolidRuntimeHealthService.Inspect(document, project)' not in RELEASE.read_text(encoding="utf-8"):
    errors.append("Release Check must consume native runtime health aggregator")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in ('QS3DFINISHTABLE', 'RoomFinishScheduleBuilder', 'QS3DDOC', 'ModelSpace', 'licensed BricsCAD V25'):
        if token not in text:
            errors.append("NATIVE-ROOM-FINISH-TABLE-P0.md missing product/runtime boundary: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room Finish native Table consumes authoritative RoomFinishScheduleBuilder output, uses reusable project-level QS3DDOC ownership/rollback/live drift health, remains ModelSpace P0, and is wired into Release Check without claiming licensed V25 qualification.")
