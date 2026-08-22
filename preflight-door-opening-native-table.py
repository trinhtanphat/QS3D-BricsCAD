#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SHARED = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/DoorOpeningNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningNativeTableCommands.cs"
SCHEDULE = ROOT / "src/QS3D.Core/Reporting/DoorOpeningSchedule.cs"
AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
DOC = ROOT / "docs/NATIVE-DOOR-OPENING-TABLE-P0.md"
errors = []

for path in (SHARED, BUILDER, COMMANDS, SCHEDULE, AGGREGATOR, RELEASE, DOC):
    if not path.is_file():
        errors.append("missing Door/Opening native Table file: " + str(path.relative_to(ROOT)))

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in (
        'RegAppName = "QS3DDOC"',
        'ProjectStateSnapshot.Capture(project)',
        'ErasePrevious(document, transaction, project, definition)',
        'table.SetSize(',
        'table.SetTextString(',
        'table.TextString(row, column)',
        'table.Rows.Count',
        'table.Columns.Count',
        'table.GenerateLayout()',
        'CadUnitService.MetersToDrawingUnits',
        'documentation.table.replace',
        'documentation.table.remove',
        'MaxRows = 5000',
        'MaxColumns = 32',
        'MaxDetailedCellIssues = 32',
        'OpenMode.ForRead',
        'HasMatchingOwnership(table, project.ProjectId, definition, storedFingerprint)',
    ):
        if token not in text:
            errors.append("ProjectOwnedNativeTableArtifactService.cs missing shared contract token: " + token)

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        '"DoorOpeningSchedule"',
        '"DoorOpeningTable"',
        '"GeneratedDoorOpeningTable"',
        'DoorOpeningScheduleBuilder.Build(project)',
        'ProjectOwnedNativeTableArtifactService.Build',
        'ProjectOwnedNativeTableArtifactService.Remove',
        'ProjectOwnedNativeTableArtifactService.Inspect',
        '"Rộng (m)"',
        '"Diện tích lỗ mở (m²)"',
        'row.OpeningAreaM2',
        'row.HostCount',
    ):
        if token not in text:
            errors.append("DoorOpeningNativeTableBuilder.cs missing authoritative adapter token: " + token)
    if 'SemanticDocumentationTableBuilder' in text or 'SemanticTagRenderer' in text:
        errors.append("Door/Opening native Table must consume DoorOpeningScheduleBuilder, not recalculate through generic semantic templates")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DDOOROPENINGTABLE"',
        '[CommandMethod("QS3DDOOROPENINGTABLEREFRESH"',
        '[CommandMethod("QS3DDOOROPENINGTABLEREMOVE"',
        '[CommandMethod("QS3DDOOROPENINGTABLEHEALTH"',
        'DoorOpeningNativeTableBuilder.StoredPosition(project)',
        'DoorOpeningNativeTableBuilder.Inspect(document, project)',
        'if (!document.Database.TileMode)',
        'CurrentUserCoordinateSystem',
    ):
        if token not in text:
            errors.append("DoorOpeningNativeTableCommands.cs missing command/scope token: " + token)

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    for token in ('DoorOpeningScheduleBuilder', 'OpeningAreaM2', 'HostWallId', 'ElementCategory.Door', 'ElementCategory.WallOpening'):
        if token not in text:
            errors.append("DoorOpeningSchedule.cs lost authoritative schedule token: " + token)

if AGGREGATOR.is_file() and 'DoorOpeningNativeTableBuilder.Inspect(document, project)' not in AGGREGATOR.read_text(encoding="utf-8"):
    errors.append("runtime health aggregator must include Door/Opening native Table health")
if RELEASE.is_file() and 'GeneratedSolidRuntimeHealthService.Inspect(document, project)' not in RELEASE.read_text(encoding="utf-8"):
    errors.append("Release Check must consume native runtime health aggregator")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in ('QS3DDOOROPENINGTABLE', 'DoorOpeningScheduleBuilder', 'QS3DDOC', 'ModelSpace', 'licensed BricsCAD V25'):
        if token not in text:
            errors.append("NATIVE-DOOR-OPENING-TABLE-P0.md missing product/runtime boundary: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Door/Opening native Table consumes the authoritative DoorOpeningScheduleBuilder, uses reusable project-level QS3DDOC ownership/rollback/live drift health, remains ModelSpace P0, and is wired into Release Check without claiming licensed V25 qualification.")
