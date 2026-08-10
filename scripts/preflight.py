#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    return path.read_text(encoding="utf-8") if path.exists() else ""


def require_tokens(rel, tokens, label):
    text = read(rel)
    if not text:
        return
    for token in tokens:
        if token not in text:
            errors.append(label + ": " + token)


required = [
    "Directory.Build.props", "README.md", "AGENTS.md", "CI_POLICY.md",
    "src/QS3D.Core/QS3D.Core.csproj", "src/QS3D.Core/Persistence/QsdbProjectStore.cs",
    "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs", "src/QS3D.Core/Persistence/AtomicFileCommit.cs",
    "src/QS3D.Core/Services/RegenerationEngine.cs", "src/QS3D.Core/Services/BulkEditService.cs",
    "src/QS3D.Core/Services/WallQuantityCalculator.cs", "src/QS3D.Core/Services/AutomaticRoomLifecycleService.cs",
    "src/QS3D.Core/Takeoff/QuantityEngine.cs", "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs", "src/QS3D.Core/Revisions/QuantityRevisionReport.cs",
    "src/QS3D.Core/Templates/ProjectTemplateStore.cs", "src/QS3D.Core/Templates/ProjectTemplateService.cs",
    "src/QS3D.Core/Rules/ProjectQuantityRuleService.cs", "src/QS3D.Core/Recognition/RecognitionEngine.cs",
    "src/QS3D.Core/Geometry/RoomBoundaryEngine.cs", "src/QS3D.Core/Export/RebarCsvExporter.cs",
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj", "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs", "src/QS3D.BricsCAD.V25/TemplateCommands.cs",
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs", "src/QS3D.BricsCAD.V25/ViewportCommands.cs",
    "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs", "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs",
    "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs", "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs",
    "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs", "src/QS3D.BricsCAD.V25/Cad/CadGeometryGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs", "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/XrefService.cs",
    "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs", "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml", "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml", "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml", "src/QS3D.BricsCAD.V25/UI/TemplateWindow.xaml",
    "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs", "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs", "tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/AutomaticRoomLifecycleSmoke.cs", "scripts/install-bricscad-v25.ps1",
    ".github/workflows/ci.yml", ".github/workflows/bricscad-v25.yml"
]
for rel in required:
    if not (ROOT / rel).exists():
        errors.append("missing required file: " + rel)

for path in list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("*.xaml")):
    try:
        ET.parse(path)
    except Exception as exc:
        errors.append(f"invalid XML {path.relative_to(ROOT)}: {exc}")

for bad in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    if list(ROOT.rglob(bad)):
        errors.append("proprietary BricsCAD assembly must not be committed: " + bad)
for ext in ("*.dwg", "*.dxf", "*.docx"):
    found = [str(p.relative_to(ROOT)) for p in ROOT.rglob(ext)]
    if found:
        errors.append(f"private/reference artifact must not be committed ({ext}): {found}")
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt", "blt3d"}:
        errors.append("vendor folder must not be committed: " + str(path.relative_to(ROOT)))

require_tokens(
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    ("<TargetFramework>net48</TargetFramework>", "$(BRICSCAD_V25_DIR)\\BrxMgd.dll", "<Private>false</Private>"),
    "plugin reference guard missing"
)

for workflow in (ROOT / ".github/workflows").glob("*.yml"):
    text = workflow.read_text(encoding="utf-8")
    if "workflow_dispatch:" not in text:
        errors.append(f"{workflow.name}: must remain manual-only")
    if re.search(r"(?m)^\s*(push|pull_request)\s*:", text):
        errors.append(f"{workflow.name}: automatic trigger forbidden before real V25 runtime gate")

for xaml in ROOT.rglob("*.xaml"):
    if xaml.name == "Theme.xaml":
        continue
    code = xaml.with_suffix(xaml.suffix + ".cs")
    if not code.exists():
        continue
    xt = xaml.read_text(encoding="utf-8")
    ct = code.read_text(encoding="utf-8")
    for handler in set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged|SelectedItemChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"', xt)):
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", ct):
            errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")

command_owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("([^\"]+)"', text):
        command_owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, owners in sorted(command_owners.items()):
    if len(owners) > 1:
        errors.append("duplicate CommandMethod " + command + ": " + ", ".join(owners))

require_tokens(
    "src/QS3D.Core/Persistence/QsdbProjectStore.cs",
    (
        "DtdProcessing = DtdProcessing.Prohibit", "XmlResolver = null", "MaxCharactersInDocument", "MaxProjectFileBytes",
        "RestorePersistenceState", "ValidateProject(project);", "AtomicFileCommit.ReplaceWithBackup",
        "double.IsNaN(quantity.Value)", "double.IsNaN(floor.ElevationM)", "QuantityRules", "AuditEntries"
    ),
    "QSDB persistence hardening missing"
)
require_tokens(
    "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs",
    ("ElementDirtyFlags.All", 'element.SetAttributeValue("updatedUtc", LegacyUpdatedUtc)', "CurrentSchemaVersion = 3", "v2 -> v3"),
    "QSDB migration guard missing"
)
require_tokens(
    "src/QS3D.Core/Revisions/QuantityRevisionReport.cs",
    ("ValidateFinite", "Revision delta overflow", "double.IsNaN", "double.IsInfinity"),
    "revision arithmetic guard missing"
)
require_tokens(
    "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs",
    ("ValidateSnapshot", "ValidateElement", "AtomicFileCommit.ReplaceWithBackup", "AtomicFileCommit.TryLoadWithBackup", "MaxSnapshotBytes"),
    "revision snapshot persistence guard missing"
)
require_tokens(
    "src/QS3D.Core/Templates/ProjectTemplateStore.cs",
    ("ValidateTemplate", "AtomicFileCommit.ReplaceWithBackup", "AtomicFileCommit.TryLoadWithBackup", "DtdProcessing = DtdProcessing.Prohibit", "XmlResolver = null"),
    "template persistence guard missing"
)
require_tokens(
    "src/QS3D.Core/Templates/ProjectTemplateService.cs",
    ("QuantityRules", "Recognition.Layer.", "VisibleBqColumnsKey", "MarkAllElementsDirty", "AuditTrail.ForProject"),
    "template apply workflow missing"
)
require_tokens(
    "src/QS3D.Core/Rules/ProjectQuantityRuleService.cs",
    ("ExpressionEvaluator.Evaluate", "AuditTrail.ForProject", "element.SetQuantity"),
    "project quantity rule workflow missing"
)
require_tokens(
    "src/QS3D.Core/Recognition/RecognitionEngine.cs",
    ("Confidence = 0.99d", "LayerMappingPrefix"),
    "project layer recognition override missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    ("new ProjectRecognitionService().SuggestBatch(project, snapshots)", "AuditTrail.ForProject(project)"),
    "recognition/revision adapter wiring missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/TemplateCommands.cs",
    ("QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT", "Chưa tự lưu .qsdb"),
    "template command/review guard missing"
)
require_tokens(
    "src/QS3D.Core/Services/BulkEditService.cs",
    ("inheritedKeys", "previousFamily.Properties"),
    "family reassignment inheritance guard missing"
)
require_tokens(
    "src/QS3D.Core/Services/WallQuantityCalculator.cs",
    ("RequireFiniteNonNegative", "FiniteProduct"),
    "legacy wall quantity finite guard missing"
)
require_tokens(
    "src/QS3D.Core/Takeoff/QuantityEngine.cs",
    ("ConvertMetric",),
    "snapshot takeoff finite guard missing"
)

require_tokens(
    "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs",
    ("QsdbRejectsDtd();", "ModelHealthDimensionIntegrity();", "ModelHealthGeneratedGeometryIntegrity();"),
    "hardening regression coverage missing"
)
require_tokens(
    "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs",
    ("LegacyMigrationMarksElementsDirty();", "FamilyAssignmentRefreshesInheritedDefaults();", "QsdbRejectsNonFiniteStateBeforeReplace();", "LegacyWallCalculatorRejectsNonFiniteValues();", "QuantityEngineRejectsInvalidSnapshotMetrics();"),
    "continuation regression coverage missing"
)
require_tokens(
    "tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs",
    ("SchemaV2MigratesToV3", "RuleAuditRoundTrip", "RuleDrivenRegeneration", "TemplateRoundTripApply", "ProjectLayerMappingWins"),
    "workflow persistence regression missing"
)
require_tokens(
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
    ("LogicRegressionSmoke.Run();", "WorkflowPersistenceSmoke.Run();", "RoomBoundaryRegressionSmoke.Run();", "AutomaticRoomLifecycleSmoke.Run();"),
    "registered smoke suite missing"
)

require_tokens(
    "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs",
    ("LengthUnit.Inch", "LengthUnit.Foot", "LengthUnit.Millimeter", "LengthUnit.Centimeter", "LengthUnit.Meter", "LengthUnit.Yard", "GetDrawingUnit"),
    "CAD unit mapping incomplete"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs",
    ("as Entity", "!entity.IsErased", "GetLiveSolidHandles", "as Solid3d"),
    "CAD handle liveness guard missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/Cad/CadGeometryGuard.cs",
    ("double.IsNaN", "double.IsInfinity", "Positive", "Finite", "ParseFinite"),
    "central CAD geometry finite guard missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    ("as Solid3d", "CommitReplacement", "PrepareReplacement"),
    "generated geometry replacement guard missing"
)
geometry_text = read("src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs")
if geometry_text:
    prepare = geometry_text.split("public static void CommitReplacement", 1)[0]
    if "element.Properties.Remove" in prepare:
        errors.append("generated metadata must not mutate before CAD transaction commit")
for rel in ("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"):
    text = read(rel)
    if not text:
        continue
    if "transaction.Commit();" not in text or "CommitReplacement" not in text:
        errors.append(rel + ": two-phase generated geometry commit missing")
    if "CadGeometryGuard." not in text:
        errors.append(rel + ": must use centralized non-finite/dimension guard")

require_tokens(
    "src/QS3D.Core/Diagnostics/ModelHealthService.cs",
    ("ValidateDimensions", "liveGeneratedSolidHandles", "GENERATED_SOLID_MISSING", "DUPLICATE_GENERATED_HANDLE", "GENERATED_HANDLE_IN_SOURCE"),
    "Model Health integrity guard missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs",
    ("Dictionary<Document, ProjectState>", "SyncDrawingIdentity", "SafeFileStem"),
    "project document lifecycle guard missing"
)
if "GetKey(Document" in read("src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"):
    errors.append("project cache must not key live documents by mutable document.Name")
require_tokens(
    "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs",
    ("ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",),
    "selection sync active-document guard missing"
)
palette_text = read("src/QS3D.BricsCAD.V25/PaletteCoordinator.cs")
if palette_text:
    if "MinimumSize = new Size(460, 420)" not in palette_text:
        errors.append("workspace PaletteSet minimum width must match compact BLT workspace target")
    if "MinimumSize = new Size(520, 420)" in palette_text:
        errors.append("workspace PaletteSet still forces old oversized width")

require_tokens(
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs",
    ("XrefService.SelectInstances", "XrefService.Reload", "XrefService.Detach", "SetImpliedSelection(Array.Empty<ObjectId>())"),
    "RightPanel Xref selection/action guard missing"
)
right_xaml = read("src/QS3D.BricsCAD.V25/UI/RightPanel.xaml")
if right_xaml:
    if 'Header="Tỉ lệ"' in right_xaml or 'Content="Xóa"' in right_xaml:
        errors.append("RightPanel still exposes misleading Xref labels")
    if 'SelectionChanged="OnDrawingSelectionChanged"' not in right_xaml:
        errors.append("Xref list must synchronize row selection to CAD selection")
require_tokens(
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs",
    ("TryFiniteNumber",),
    "Family dimensional property validation missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs",
    ("QS3DUNTRACKFINISH",),
    "HT_Phòng remove action safety missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/ViewportCommands.cs",
    ("QS3DUNTRACKFINISH",),
    "finish-only untrack command missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    ('TargetType="{x:Type DataGrid}"', 'TargetType="{x:Type DataGridColumnHeader}"'),
    "dark CAD DataGrid theme coverage missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs",
    ("_recalculate", "VisibleBqColumnsKey", "PersistColumnPreferences"),
    "BQ recalculation/column preference guard missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/Commands.cs",
    ("new QuantitySummaryWindow(rows, locate, recalculate)", "CadUnitService.GetDrawingUnit(doc)", "GetLiveSolidHandles", "liveGeneratedSolids"),
    "BQ/health command wiring missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    ("QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT", "QS3DRECOGNIZE", "QS3DRECOGNIZEAUTO", "QS3DBBSVIEW", "QS3DREVBASE", "QS3DREVDIFF"),
    "Ribbon workflow entry missing"
)
require_tokens(
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs",
    ("ProjectStateSnapshot.Capture", "AutomaticRoomLifecycleService", "SourceHandles.Clear", "ReconcileStale"),
    "automatic room apply/lifecycle guard missing"
)
require_tokens(
    "src/QS3D.Core/Services/AutomaticRoomLifecycleService.cs",
    ("BuildStableElementId", "NormalizeSourceSignature", "GetSourceSignature", "ReconcileStale", "AutoBoundaryStale"),
    "automatic room lifecycle service incomplete"
)
require_tokens(
    "scripts/install-bricscad-v25.ps1",
    ("Get-AuthenticodeSignature", "Get-FileHash", "ExpectedSha256"),
    "BricsCAD installer verification missing"
)

print("QS3D preflight")
print("root:", ROOT)
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: structure, XML/XAML handlers, unique commands, manual CI, proprietary-file guard, QSDB v3/rules/audit, template/recognition/revision workflows, migration/persistence/quantity/health guards, centralized CAD geometry validation, two-phase 3D geometry, document lifecycle, Xref/UI safety and automatic-room lifecycle invariants are present.")
