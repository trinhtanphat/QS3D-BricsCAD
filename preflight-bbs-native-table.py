#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/BbsNativeTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/BbsNativeTableCommands.cs"
BBS = ROOT / "src/QS3D.Core/Rebar/RebarSchedule.cs"
BBS_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs"
CSV_COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
SHARED = ROOT / "src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs"
AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs"
HUB = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml"
DOC = ROOT / "docs/NATIVE-BBS-TABLE-P0.md"
errors = []

for path in (BUILDER, COMMANDS, BBS, BBS_SMOKE, CSV_COMMANDS, SHARED, AGGREGATOR, RELEASE, HUB, DOC):
    if not path.is_file(): errors.append("missing BBS native Table file: " + str(path.relative_to(ROOT)))

row_tokens = (
    'row.ElementId', 'row.BarMark', 'row.ShapeCode', 'row.Notation', 'row.DiameterMm',
    'row.Quantity', 'row.CuttingLengthM', 'row.TotalLengthM', 'row.UnitWeightKgM',
    'row.NetWeightKg', 'row.WastePercent', 'row.TotalWeightKg', 'row.FabricationStatus',
    'row.FabricationStandardCode', 'row.FabricationDetailingRevision'
)

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        '"RebarBbsSchedule"', '"RebarBbsTable"', '"GeneratedBbsTable"',
        'ProjectRebarScheduleBuilder.Build(project)',
        'ProjectOwnedNativeTableArtifactService.Build',
        'ProjectOwnedNativeTableArtifactService.Remove',
        'ProjectOwnedNativeTableArtifactService.Inspect',
        '"BBS_" + x.Code', '"BBS_TABLE_PROJECT_DIRTY"',
        'x.Properties.TryGetValue("RebarNotation"',
        '"QS3D BBS • Bar Bending Schedule"',
    ) + row_tokens:
        if token not in text: errors.append("BbsNativeTableBuilder.cs missing authoritative/lifecycle token: " + token)
    for forbidden in (
        'new RebarScheduleInput',
        'RebarNotationParser',
        'RebarMath',
        'RebarWeight',
        'KilogramsPerMeter(',
        'TotalKilograms(',
    ):
        if forbidden in text:
            errors.append("BBS native Table must consume ProjectRebarScheduleBuilder without duplicating Core rebar calculation/parsing: " + forbidden)

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DBBSTABLE"', '[CommandMethod("QS3DBBSTABLEREFRESH"',
        '[CommandMethod("QS3DBBSTABLEREMOVE"', '[CommandMethod("QS3DBBSTABLEHEALTH"',
        'new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)',
        'BbsNativeTableBuilder.StoredPosition(project)', 'BbsNativeTableBuilder.Inspect(document, project)',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'RequireModelSpace(document)', 'RequireSupportedUcs(document)',
    ):
        if token not in text: errors.append("BbsNativeTableCommands.cs missing lifecycle/regen/read-only-health token: " + token)

if BBS.is_file():
    text = BBS.read_text(encoding="utf-8")
    for token in (
        'public static class ProjectRebarScheduleBuilder',
        'foreach (var element in ValidateProjectElements(project))',
        'private static IReadOnlyList<ProjectElement> ValidateProjectElements(ProjectState project)',
        'Project contains a null semantic element entry.',
        'Project contains a semantic element with a blank id.',
        'Project contains duplicate semantic element id:',
        'new HashSet<string>(StringComparer.OrdinalIgnoreCase)',
        'RebarScheduleBuilder.Build(inputs)',
        'RebarFabricationQualificationHealthService.StatusPropertyKey',
        'RebarFabricationQualificationHealthService.StandardCodePropertyKey',
        'RebarFabricationQualificationHealthService.DetailingRevisionPropertyKey',
    ):
        if token not in text: errors.append("RebarSchedule.cs lost authoritative BBS/identity/provenance token: " + token)

if BBS_SMOKE.is_file():
    text = BBS_SMOKE.read_text(encoding="utf-8")
    for token in (
        'ProjectScheduleRejectsNullSemanticEntry();',
        'ProjectScheduleRejectsDuplicateSemanticIdentity();',
        'project.Elements.Add(null!);',
        'new ProjectElement("b1", ElementCategory.Room',
        'ProjectRebarScheduleBuilder.Build(project)',
    ):
        if token not in text: errors.append("BbsRegressionSmoke.cs missing project identity fail-closed regression: " + token)

if CSV_COMMANDS.is_file():
    text = CSV_COMMANDS.read_text(encoding="utf-8")
    for token in (
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
<<<<<<< HEAD
        'RegenerateDirty(snapshot)',
        'ProjectRebarScheduleBuilder.Build(snapshot)',
    ):
        if token not in text: errors.append("BBS CSV command lost read-only detached schedule token: " + token)
    for forbidden in (
        'ProjectContextCoordinator.GetOrCreate(document)',
        'RegenerateDirty(project)',
        'ProjectRebarScheduleBuilder.Build(project)',
    ):
        if forbidden in text: errors.append("BBS CSV command must not mutate/build from the live project: " + forbidden)
=======
        'ProjectRebarScheduleBuilder.Build(snapshot)',
    ):
        if token not in text:
            errors.append("BBS CSV command no longer shares detached ProjectRebarScheduleBuilder authority: " + token)
    if 'ProjectRebarScheduleBuilder.Build(project)' in text:
        errors.append("BBS CSV command must not build from live read-only project state")
>>>>>>> origin/main

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in (
        'QS3DDOC', 'ProjectStateSnapshot.Capture(project)', 'ErasePrevious(document, transaction, project, definition)',
        'table.TextString(row, column)', 'CAD_POSITION_DRIFT', 'MaxDetailedCellIssues = 32'
    ):
        if token not in text: errors.append("shared native Table service lost ownership/rollback/live-health token: " + token)

if AGGREGATOR.is_file():
    text = AGGREGATOR.read_text(encoding="utf-8")
    if 'BbsNativeTableBuilder.Inspect(document, project)' not in text:
        errors.append("runtime health aggregator missing BBS Table provider")
    pos = text.find('BbsNativeTableBuilder.Inspect(document, project)')
    if pos >= 0 and text.rfind('AddProviderSafely(', 0, pos) < 0:
        errors.append("BBS runtime health provider must be fail-isolated")

if RELEASE.is_file() and 'GeneratedSolidRuntimeHealthService.Inspect(document, project)' not in RELEASE.read_text(encoding="utf-8"):
    errors.append("Release Check must consume native runtime health aggregator")

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for token in ('Tag="QS3DBBSTABLE"', 'Tag="QS3DBBSTABLEREFRESH"', 'Tag="QS3DBBSTABLEHEALTH"', 'Tag="QS3DBBSTABLEREMOVE"'):
        if token not in text: errors.append("Schedule Hub missing BBS native Table action: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'QS3DBBSTABLE', 'ProjectRebarScheduleBuilder', '15 columns', 'QS3DDOC',
        'Fabrication qualification remains a separate authoritative Health/Release concern',
        'ModelSpace', 'licensed BricsCAD V25 runtime qualification is still required'
    ):
        if token not in text: errors.append("NATIVE-BBS-TABLE-P0.md missing authority/product/runtime boundary: " + token)

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS native Table consumes authoritative ProjectRebarScheduleBuilder rows across all 15 schedule/provenance fields, rejects corrupt project semantic identities before extraction, forbids native rebar calculation/parsing duplication, keeps CSV authority on a detached read-only snapshot, regenerates semantic state before native build/refresh, uses shared project-level QS3DDOC ownership/rollback/live drift health, remains read-only in health, is fail-isolated in Release Check runtime health and is discoverable from Schedule Hub without claiming licensed V25 qualification.")
