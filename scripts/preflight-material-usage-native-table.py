#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/MaterialUsageNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageNativeTableCommands.cs"
SCHEDULE = ROOT / "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs"
SHARED = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
DOC = ROOT / "docs/NATIVE-MATERIAL-USAGE-TABLE-P0.md"
errors = []

for path in (BUILDER, COMMANDS, SCHEDULE, SHARED, AGGREGATOR, RELEASE, DOC):
    if not path.is_file():
        errors.append("missing Material Usage native Table file: " + str(path.relative_to(ROOT)))

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        '"MaterialUsageSchedule"',
        '"MaterialUsageTable"',
        '"GeneratedMaterialUsageTable"',
        'MaterialUsageScheduleBuilder.Build(project)',
        'ProjectOwnedNativeTableArtifactService.Build',
        'ProjectOwnedNativeTableArtifactService.Remove',
        'ProjectOwnedNativeTableArtifactService.Inspect',
        'row.PrimaryQuantity',
        'row.VolumeM3',
        'row.MassKg',
        '"MATERIAL_USAGE_" + x.Code',
    ):
        if token not in text:
            errors.append("MaterialUsageNativeTableBuilder.cs missing authoritative adapter token: " + token)
    if 'SemanticDocumentationTableBuilder' in text or 'SemanticTagRenderer' in text:
        errors.append("Material Usage native Table must consume MaterialUsageScheduleBuilder, not generic semantic template calculations")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DMATERIALTABLE"',
        '[CommandMethod("QS3DMATERIALTABLEREFRESH"',
        '[CommandMethod("QS3DMATERIALTABLEREMOVE"',
        '[CommandMethod("QS3DMATERIALTABLEHEALTH"',
        'MaterialUsageNativeTableBuilder.StoredPosition(project)',
        'MaterialUsageNativeTableBuilder.Inspect(document, project)',
        'RequireModelSpace(document)',
        'RequireSupportedUcs(document)',
    ):
        if token not in text:
            errors.append("MaterialUsageNativeTableCommands.cs missing command/scope token: " + token)

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    for token in (
        'MaterialUsageScheduleBuilder',
        'ProjectMaterialCatalog.GetAll(project)',
        'AutoRoomLifecycle.IsExcludedFromQuantity(project, element)',
        'CurtainFrameMaterial',
        'CurtainFrameLengthM',
        'QuantityReportMath.Add',
        'PrimaryQuantity',
    ):
        if token not in text:
            errors.append("MaterialUsageSchedule.cs lost authoritative material schedule token: " + token)

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in ('QS3DDOC', 'ProjectStateSnapshot.Capture(project)', 'table.TextString(row, column)', 'MaxDetailedCellIssues = 32'):
        if token not in text:
            errors.append("shared native Table service lost ownership/rollback/live-health token: " + token)

if AGGREGATOR.is_file() and 'MaterialUsageNativeTableBuilder.Inspect(document, project)' not in AGGREGATOR.read_text(encoding="utf-8"):
    errors.append("runtime health aggregator must include Material Usage native Table health")
if RELEASE.is_file() and 'GeneratedSolidRuntimeHealthService.Inspect(document, project)' not in RELEASE.read_text(encoding="utf-8"):
    errors.append("Release Check must consume native runtime health aggregator")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in ('QS3DMATERIALTABLE', 'MaterialUsageScheduleBuilder', 'QS3DDOC', 'ModelSpace', 'licensed BricsCAD V25'):
        if token not in text:
            errors.append("NATIVE-MATERIAL-USAGE-TABLE-P0.md missing product/runtime boundary: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Material Usage native Table consumes authoritative MaterialUsageScheduleBuilder output, uses reusable project-level QS3DDOC ownership/rollback/live drift health, remains ModelSpace P0, and is wired into Release Check without claiming licensed V25 qualification.")
