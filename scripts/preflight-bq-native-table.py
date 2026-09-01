#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/BqNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/BqNativeTableCommands.cs"
REPORT = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
ROW = ROOT / "src/QS3D.Core/Reporting/QuantityReportRow.cs"
XLSX = ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs"
SHARED = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
DOC = ROOT / "docs/NATIVE-BQ-TABLE-P0.md"
errors = []

for path in (BUILDER, COMMANDS, REPORT, ROW, XLSX, SHARED, AGGREGATOR, RELEASE, DOC):
    if not path.is_file(): errors.append("missing BQ native Table file: " + str(path.relative_to(ROOT)))

metric_tokens = (
    'row.GrossConcreteM3', 'row.DeductionM3', 'row.NetConcreteM3',
    'row.FormworkM2', 'row.LengthM', 'row.OuterPerimeterM', 'row.InnerPerimeterM',
    'row.DoorAreaM2', 'row.SideAreaM2', 'row.BottomAreaM2', 'row.TopAreaM2', 'row.OtherAreaM2'
)
trace_tokens = ('row.ElementIdText', 'row.SourceHandleText', 'row.DrawingFingerprint')

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        '"QuantityReportSchedule"', '"BqQuantityTable"', '"GeneratedBqTable"',
        'ProjectQuantityReportBuilder.Group(project)',
        'ProjectOwnedNativeTableArtifactService.Build',
        'ProjectOwnedNativeTableArtifactService.Remove',
        'ProjectOwnedNativeTableArtifactService.Inspect',
        '"Zone"', 'row.Zone',
        '"QS3D Element ID"', '"CAD Handle (hex)"', '"QS3D Drawing Fingerprint"',
        '"BQ_" + x.Code', '"BQ_TABLE_PROJECT_DIRTY"',
    ) + metric_tokens + trace_tokens:
        if token not in text: errors.append("BqNativeTableBuilder.cs missing authoritative/XLSX-parity token: " + token)
    if 'SemanticDocumentationTableBuilder' in text or 'SemanticTagRenderer' in text:
        errors.append("BQ native Table must consume ProjectQuantityReportBuilder, not generic semantic templates")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DBQTABLE"', '[CommandMethod("QS3DBQTABLEREFRESH"',
        '[CommandMethod("QS3DBQTABLEREMOVE"', '[CommandMethod("QS3DBQTABLEHEALTH"',
        'new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)',
        'BqNativeTableBuilder.StoredPosition(project)', 'BqNativeTableBuilder.Inspect(document, project)',
        'RequireModelSpace(document)', 'RequireSupportedUcs(document)',
        'native Table đã commit nhưng viewport/palette không refresh đầy đủ.',
    ):
        if token not in text: errors.append("BqNativeTableCommands.cs missing lifecycle/regen/redaction token: " + token)
    if 'ex.Message' in text:
        errors.append("BqNativeTableCommands.cs must not expose raw caught exception messages")

if REPORT.is_file():
    text = REPORT.read_text(encoding="utf-8")
    for token in ('RoomFinishIdentityService.ValidateProject(project)','AutoRoomLifecycle.IsExcludedFromQuantity(project, element)','QuantityReportMath.Add','SourceHandleResolver.Resolve'):
        if token not in text: errors.append("ProjectQuantityReportBuilder.cs lost authoritative BQ token: " + token)

if ROW.is_file():
    text = ROW.read_text(encoding="utf-8")
    for token in ('GrossConcreteM3','DeductionM3','NetConcreteM3','FormworkM2','OtherAreaM2','ElementIdText','SourceHandleText','DrawingFingerprint'):
        if token not in text: errors.append("QuantityReportRow.cs lost BQ field: " + token)

if XLSX.is_file():
    text = XLSX.read_text(encoding="utf-8")
    for token in metric_tokens + trace_tokens + ('"Zone"','row.Zone','"QS3D Element ID"','"CAD Handle (hex)"','"QS3D Drawing Fingerprint"','var range = "A1:T"'):
        if token not in text: errors.append("XlsxQuantityExporter.cs lost BQ parity token: " + token)

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in ('QS3DDOC','ProjectStateSnapshot.Capture(project)','table.TextString(row, column)','MaxDetailedCellIssues = 32'):
        if token not in text: errors.append("shared native Table service lost ownership/rollback/live-health token: " + token)

if AGGREGATOR.is_file():
    text = AGGREGATOR.read_text(encoding="utf-8")
    if 'BqNativeTableBuilder.Inspect(document, project)' not in text: errors.append("runtime health aggregator missing BQ Table provider")
    pos = text.find('BqNativeTableBuilder.Inspect(document, project)')
    if pos >= 0 and text.rfind('AddProviderSafely(', 0, pos) < 0: errors.append("BQ runtime health provider must be fail-isolated")
if RELEASE.is_file() and 'GeneratedSolidRuntimeHealthService.Inspect(document, project)' not in RELEASE.read_text(encoding="utf-8"):
    errors.append("Release Check must consume native runtime health aggregator")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in ('QS3DBQTABLE','ProjectQuantityReportBuilder','XlsxQuantityExporter','20 columns','QS3DDOC','ModelSpace','licensed BricsCAD V25'):
        if token not in text: errors.append("NATIVE-BQ-TABLE-P0.md missing parity/product/runtime boundary: " + token)

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BQ native Table preserves authoritative quantity/lifecycle semantics and redacts raw command/post-commit UI exception detail.")
