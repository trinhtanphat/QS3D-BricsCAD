#!/usr/bin/env python3
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

def committable_files():
    result = subprocess.run(
        ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
        cwd=ROOT,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if result.returncode != 0:
        errors.append("could not enumerate Git-tracked/non-ignored files: " + result.stderr.decode("utf-8", errors="replace").strip())
        return []
    return [Path(item.decode("utf-8")) for item in result.stdout.split(b"\0") if item]

eligible_files = committable_files()

required = [
    "Directory.Build.props", "README.md", "AGENTS.md", "CI_POLICY.md",
    "src/QS3D.Core/QS3D.Core.csproj", "src/QS3D.Core/Domain/ProjectState.cs",
    "src/QS3D.Core/Persistence/QsdbProjectStore.cs", "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs",
    "src/QS3D.Core/Diagnostics/ModelHealthService.cs", "src/QS3D.Core/Audit/AuditTrail.cs",
    "src/QS3D.Core/Rules/QuantityRuleEngine.cs", "src/QS3D.Core/Services/RegenerationEngine.cs",
    "src/QS3D.Core/Services/HostLinkService.cs", "src/QS3D.Core/Services/BulkEditService.cs",
    "src/QS3D.Core/Services/WallQuantityCalculator.cs", "src/QS3D.Core/Recognition/ProjectRecognitionService.cs",
    "src/QS3D.Core/Templates/TemplateProfileStore.cs", "src/QS3D.Core/Takeoff/QuantityEngine.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs", "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V25/Commands.cs", "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "src/QS3D.BricsCAD.V25/TemplateCommands.cs", "src/QS3D.BricsCAD.V25/ViewportCommands.cs",
    "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs", "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs",
    "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs", "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs",
    "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs", "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/XrefService.cs", "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml", "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml", "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml", "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml",
    "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs", "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs", "tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs",
    "scripts/install-bricscad-v25.ps1", ".github/workflows/ci.yml", ".github/workflows/bricscad-v25.yml"
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing required file: " + rel)

for path in list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("*.xaml")):
    try: ET.parse(path)
    except Exception as exc: errors.append(f"invalid XML {path.relative_to(ROOT)}: {exc}")

for bad in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    if list(ROOT.rglob(bad)): errors.append("proprietary BricsCAD assembly must not be committed: " + bad)
allowed_synthetic_cad = {
    Path("samples/generated/QS3D-Sample.dwg"),
    Path("samples/generated/QS3D-Sample.dxf"),
}
private_extensions = {".dwg", ".dxf", ".docx"}
for relative in eligible_files:
    suffix = relative.suffix.casefold()
    if suffix not in private_extensions:
        continue
    if suffix in {".dwg", ".dxf"} and relative in allowed_synthetic_cad:
        continue
    errors.append(f"private/reference artifact must not be committed ({suffix}): {relative}")
sample_readme = ROOT / "samples/generated/README.md"
if any((ROOT / relative).is_file() for relative in allowed_synthetic_cad):
    if not sample_readme.is_file():
        errors.append("synthetic CAD fixtures require samples/generated/README.md provenance")
    else:
        sample_text = sample_readme.read_text(encoding="utf-8")
        for token in ("generated specifically for QS3D", "no BLT source", "private project data"):
            if token not in sample_text: errors.append("synthetic sample provenance README missing token: " + token)
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt", "blt3d"}: errors.append("vendor folder must not be committed: " + str(path.relative_to(ROOT)))

plugin = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if plugin.exists():
    text = plugin.read_text(encoding="utf-8")
    for needle, message in {"<TargetFramework>net48</TargetFramework>": "plugin must target net48", "$(BRICSCAD_V25_DIR)\\BrxMgd.dll": "plugin must use external BrxMgd reference", "<Private>false</Private>": "BricsCAD references must not be copied locally"}.items():
        if needle not in text: errors.append(message)

workflow_dir = ROOT / ".github/workflows"
automatic_workflows = {
    "ci.yml",
    "dispatch-v25-cloud-after-main-integration.yml",
    "hybrid-pr-coordinator.yml",
}
if workflow_dir.is_dir():
    workflow_files = sorted(
        path for path in workflow_dir.iterdir()
        if path.is_file() and path.suffix.casefold() in {".yml", ".yaml"}
    )
    for workflow in workflow_files:
        if workflow.name in automatic_workflows:
            continue
        text = workflow.read_text(encoding="utf-8")
        if "workflow_dispatch:" not in text: errors.append(f"{workflow.name}: must remain manual-only")
        if re.search(r"(?m)^\s*(push|pull_request)\s*:", text): errors.append(f"{workflow.name}: automatic trigger forbidden before real V25 runtime gate")
else:
    errors.append("missing workflows directory: .github/workflows")

for xaml in ROOT.rglob("*.xaml"):
    if xaml.name == "Theme.xaml": continue
    code = xaml.with_suffix(xaml.suffix + ".cs")
    if not code.exists(): continue
    xt = xaml.read_text(encoding="utf-8")
    companions = sorted(xaml.parent.glob(xaml.stem + "*.cs"))
    ct = "\n".join(path.read_text(encoding="utf-8") for path in companions)
    for handler in set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged|SelectedItemChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"', xt)):
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", ct): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")

project_state = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
if project_state.exists():
    text = project_state.read_text(encoding="utf-8")
    if "CurrentSchemaVersion = 4" not in text: errors.append("QSDB schema v4 is required for persisted rules/audit and project mappings")
    if "IList<QuantityRule> QuantityRules" not in text: errors.append("project quantity-rule catalog missing")
    if "IList<AuditEvent> AuditEvents" not in text: errors.append("project audit catalog missing")

store = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
if store.exists():
    text = store.read_text(encoding="utf-8")
    for needle, message in {
        "DtdProcessing = DtdProcessing.Prohibit": "QSDB DTD hardening missing", "XmlResolver = null": "QSDB external XML resolver must stay disabled",
        "MaxCharactersInDocument": "QSDB XML character limit missing", "MaxProjectFileBytes": "QSDB file-size guard missing",
        "RestorePersistenceState": "QSDB dirty-state restore missing", "ValidateProject(project);": "QSDB must validate in-memory state before replacing the persisted project",
        "AtomicFileCommit.ReplaceWithBackup": "QSDB atomic replacement/recovery helper missing", "double.IsNaN(quantity.Value)": "QSDB non-finite quantity validation missing",
        "double.IsNaN(floor.ElevationM)": "QSDB non-finite floor validation missing", 'new XElement("rules"': "QSDB quantity-rule persistence missing",
        'new XElement("audit"': "QSDB audit persistence missing", "project.QuantityRules.Add": "QSDB quantity-rule deserialization missing", "project.AuditEvents.Add": "QSDB audit deserialization missing"
    }.items():
        if needle not in text: errors.append(message)

migrator = ROOT / "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs"
if migrator.exists():
    text = migrator.read_text(encoding="utf-8")
    if "ElementDirtyFlags.All" not in text or 'element.SetAttributeValue("updatedUtc", LegacyUpdatedUtc)' not in text: errors.append("legacy QSDB elements must migrate dirty and require deterministic regeneration")
    if "MigrateV2ToV3" not in text: errors.append("QSDB v2 to v3 migration missing")
    if "MigrateV3ToV4" not in text: errors.append("QSDB v3 to v4 migration missing")

regen = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
if regen.exists() and "ApplyMatching(project, element)" not in regen.read_text(encoding="utf-8"): errors.append("project quantity rules are not integrated into regeneration")
rules = ROOT / "src/QS3D.Core/Rules/QuantityRuleEngine.cs"
if rules.exists() and "ApplyMatching(ProjectState project" not in rules.read_text(encoding="utf-8"): errors.append("quantity rule auto-application missing")
host = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
if host.exists():
    text = host.read_text(encoding="utf-8")
    if "EnsureOpening" not in text: errors.append("host link must reject non-opening semantic elements")
    if "AuditTrail.ForProject(project)" not in text: errors.append("host link/unlink audit provenance missing")

template = ROOT / "src/QS3D.Core/Templates/TemplateProfileStore.cs"
if template.exists():
    text = template.read_text(encoding="utf-8")
    for needle in ("MaxTemplateFileBytes", "DtdProcessing.Prohibit", "AtomicFileCommit.ReplaceWithBackup", "ExportProject", "Apply(ProjectState project", "LayerMappingPrefix", "VisibleBqColumnsKey"):
        if needle not in text: errors.append("template persistence/apply hardening missing: " + needle)
recognition = ROOT / "src/QS3D.Core/Recognition/ProjectRecognitionService.cs"
if recognition.exists():
    text = recognition.read_text(encoding="utf-8")
    if "Confidence = 0.99d" not in text or "LayerMappingPrefix" not in text: errors.append("project layer recognition override missing")
review_commands = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review_commands.exists():
    text = review_commands.read_text(encoding="utf-8")
    if "new ProjectRecognitionService().SuggestBatch(project, snapshots)" not in text: errors.append("Recognition UI does not consume project layer mappings")
    if "AuditTrail.ForProject(project)" not in text: errors.append("recognition/revision audit wiring missing")
template_commands = ROOT / "src/QS3D.BricsCAD.V25/TemplateCommands.cs"
if template_commands.exists():
    text = template_commands.read_text(encoding="utf-8")
    for command in ("QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT"):
        if command not in text: errors.append("template command missing: " + command)
    if "Chưa tự lưu .qsdb" not in text: errors.append("template import must remain reviewable before explicit project save")

bulk = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
if bulk.exists():
    text = bulk.read_text(encoding="utf-8")
    family_reassignment_tokens = (
        "inheritedKeys",
        'ProjectFamilyService.SnapshotProperties(family, "Target", "bulk assignment")',
        'ProjectFamilyService.SnapshotProperties(previousFamily, "Previous", "bulk assignment")',
        "targetPropertyKeys",
        "previousProperties",
    )
    if any(token not in text for token in family_reassignment_tokens):
        errors.append("family reassignment must refresh inherited defaults without overwriting instance overrides")
wall_quantity = ROOT / "src/QS3D.Core/Services/WallQuantityCalculator.cs"
if wall_quantity.exists():
    text = wall_quantity.read_text(encoding="utf-8")
    if "RequireFiniteNonNegative" not in text or "FiniteProduct" not in text: errors.append("legacy wall quantity path must reject non-finite dimensions and overflow")
takeoff = ROOT / "src/QS3D.Core/Takeoff/QuantityEngine.cs"
if takeoff.exists() and "ConvertMetric" not in takeoff.read_text(encoding="utf-8"): errors.append("raw snapshot takeoff must reject negative/non-finite metrics")

hardening = ROOT / "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs"
if hardening.exists():
    text = hardening.read_text(encoding="utf-8")
    for needle in ("QsdbRejectsDtd();", "ModelHealthDimensionIntegrity();", "ModelHealthGeneratedGeometryIntegrity();"):
        if needle not in text: errors.append("hardening regression coverage missing: " + needle)
continuation = ROOT / "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs"
if continuation.exists():
    text = continuation.read_text(encoding="utf-8")
    for needle in ("LegacyMigrationMarksElementsDirty();", "FamilyAssignmentRefreshesInheritedDefaults();", "QsdbRejectsNonFiniteStateBeforeReplace();", "LegacyWallCalculatorRejectsNonFiniteValues();", "QuantityEngineRejectsInvalidSnapshotMetrics();"):
        if needle not in text: errors.append("continuation regression coverage missing: " + needle)
workflow_smoke = ROOT / "tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs"
if workflow_smoke.exists():
    text = workflow_smoke.read_text(encoding="utf-8")
    for needle in ("SchemaV2MigratesToV4", "RuleAuditRoundTrip", "RuleDrivenRegeneration", "TemplateRoundTripApply", "ProjectLayerMappingWins"):
        if needle not in text: errors.append("workflow persistence regression missing: " + needle)
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists():
    text = registration.read_text(encoding="utf-8")
    for needle in ("LogicRegressionSmoke.Run();", "WorkflowPersistenceSmoke.Run();"):
        if needle not in text: errors.append("registered smoke suite missing: " + needle)
    program_text = (ROOT / "tests/QS3D.Core.SmokeTests/Program.cs").read_text(encoding="utf-8")
    if "SmokeTestRegistration.RunAll();" not in program_text: errors.append("smoke suites must run from Main instead of a module initializer")

units = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs"
if units.exists():
    text = units.read_text(encoding="utf-8")
    for needle in ("LengthUnit.Inch", "LengthUnit.Foot", "LengthUnit.Millimeter", "LengthUnit.Centimeter", "LengthUnit.Meter", "LengthUnit.Yard", "GetDrawingUnit"):
        if needle not in text: errors.append("CAD unit mapping incomplete: " + needle)
handle_service = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs"
if handle_service.exists():
    text = handle_service.read_text(encoding="utf-8")
    if "as Entity" not in text or "!entity.IsErased" not in text: errors.append("CAD handle resolver must open entities and reject erased objects")
    if "GetLiveSolidHandles" not in text or "as Solid3d" not in text: errors.append("generated geometry liveness must verify Solid3d type")

geometry = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs"
if geometry.exists():
    text = geometry.read_text(encoding="utf-8")
    if "as Solid3d" not in text: errors.append("generated geometry cleanup must only erase tracked Solid3d objects")
    if "CommitReplacement" not in text or "PrepareReplacement" not in text: errors.append("generated geometry must use two-phase CAD/metadata replacement")
    for needle in ("ExtendedDataRegAppName", "GetXDataForApplication", "RequireMatchingOwnership", "GeneratedSolidOwnerProjectId", "GeneratedSolidOwnerElementId"):
        if needle not in text: errors.append("generated geometry XData ownership guard missing: " + needle)
    prepare = text.split("public static void CommitReplacement", 1)[0]
    if "element.Properties.Remove" in prepare: errors.append("generated metadata must not mutate before CAD transaction commit")
for rel in ("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"):
    path = ROOT / rel
    if path.exists():
        text = path.read_text(encoding="utf-8")
        if "transaction.Commit();" not in text or "CommitReplacement" not in text: errors.append(rel + ": two-phase generated geometry commit missing")
        has_inline_finite_guard = "double.IsNaN" in text and "double.IsInfinity" in text
        if not has_inline_finite_guard and "CadGeometryGuard." not in text: errors.append(rel + ": non-finite dimension guard missing")

health = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthService.cs"
if health.exists():
    text = health.read_text(encoding="utf-8")
    for needle in ("ValidateDimensions", "liveGeneratedSolidHandles", "GENERATED_SOLID_MISSING", "DUPLICATE_GENERATED_HANDLE", "GENERATED_HANDLE_IN_SOURCE", "GENERATED_OWNERSHIP_MISSING", "GENERATED_PROJECT_MISMATCH", "GENERATED_ELEMENT_MISMATCH"):
        if needle not in text: errors.append("Model Health integrity guard missing: " + needle)

context = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
if context.exists():
    text = context.read_text(encoding="utf-8")
    if "Dictionary<Document, ProjectState>" not in text: errors.append("project cache must use Document identity so Save As cannot orphan in-memory project state")
    if "SyncDrawingIdentity" not in text: errors.append("project cache must synchronize drawing identity after Save As")
    if "Database.FingerprintGuid" not in text or "drawing identity mismatch" not in text: errors.append("project cache must bind persisted Handles to the live DWG fingerprint and fail closed on mismatch")
    if "SafeFileStem" not in text: errors.append("unsaved drawing project path must sanitize the local filename")
    if "GetKey(Document" in text: errors.append("project cache must not key live documents by mutable document.Name")
selection_sync = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
if selection_sync.exists() and "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in selection_sync.read_text(encoding="utf-8"): errors.append("selection sync must ignore inactive documents")
palette = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
if palette.exists():
    text = palette.read_text(encoding="utf-8")
    if "MinimumSize = new DrawingSize(UserUiLayoutStore.WorkspacePaletteMinWidth, UserUiLayoutStore.WorkspacePaletteMinHeight)" not in text: errors.append("workspace PaletteSet minimum must use the centralized compact layout policy")
    if "MinimumSize = new Size(520, 420)" in text: errors.append("workspace PaletteSet still forces the old oversized minimum width")
layout_store = ROOT / "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
if layout_store.exists():
    text = layout_store.read_text(encoding="utf-8")
    if "internal const int WorkspacePaletteMinWidth = 460;" not in text or "internal const int WorkspacePaletteMinHeight = 420;" not in text:
        errors.append("centralized workspace PaletteSet minimum must preserve the compact 460x420 target")

right = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
if right.exists():
    text = right.read_text(encoding="utf-8")
    for needle in ("XrefService.SelectInstances", "XrefService.Reload", "XrefService.Detach", "SetImpliedSelection(Array.Empty<ObjectId>())"):
        if needle not in text: errors.append("RightPanel Xref selection/action guard missing: " + needle)
right_xaml = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
if right_xaml.exists():
    text = right_xaml.read_text(encoding="utf-8")
    if 'Header="Tỉ lệ"' in text or 'Content="Xóa"' in text: errors.append("RightPanel still exposes misleading Xref labels")
    if 'SelectionChanged="OnDrawingSelectionChanged"' not in text: errors.append("Xref list must synchronize row selection to CAD selection")
workspace_vm = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
if workspace_vm.exists() and "TryFiniteNumber" not in workspace_vm.read_text(encoding="utf-8"): errors.append("Family dimensional property validation missing")
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
if workspace.exists() and "QS3DUNTRACKFINISH" not in workspace.read_text(encoding="utf-8"): errors.append("HT_Phòng remove action must not untrack unrelated semantic elements")
viewport = ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs"
if viewport.exists() and "QS3DUNTRACKFINISH" not in viewport.read_text(encoding="utf-8"): errors.append("finish-only untrack command missing")
theme = ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml"
if theme.exists():
    text = theme.read_text(encoding="utf-8")
    if 'TargetType="{x:Type DataGrid}"' not in text or 'TargetType="{x:Type DataGridColumnHeader}"' not in text: errors.append("dark CAD DataGrid theme coverage missing")

quantity = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
commands = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
if quantity.exists():
    text = quantity.read_text(encoding="utf-8")
    if "_recalculate" not in text: errors.append("BQ Tính lại must have a real recalculation callback")
    if "VisibleBqColumnsKey" not in text or "PersistColumnPreferences" not in text: errors.append("BQ visible columns must round-trip through project metadata")
if commands.exists():
    text = commands.read_text(encoding="utf-8")
    if "new QuantitySummaryWindow(doc, rows, locate, recalculate)" not in text: errors.append("BQ command does not wire recalculation callback")
    if "CadUnitService.GetDrawingUnit(doc)" not in text: errors.append("BQ snapshot fallback still assumes millimeters")
    if "GetLiveSolidHandles" not in text or "liveGeneratedSolids" not in text: errors.append("QS3DHEALTH must verify generated Solid3d liveness")
    if "QS3DED2" not in text or "QS3DEXCELLOCATE" not in text or "XlsxHandleReader.ReadHandleLookup" not in text: errors.append("ED2 Excel/Handle round-trip workflow missing")
    locate_service = ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs"
    identity_text = text + (locate_service.read_text(encoding="utf-8") if locate_service.exists() else "")
    for needle in ("DrawingFingerprint", "Excel drawing fingerprint does not match", "Type YES"):
        if needle not in identity_text: errors.append("ED2 drawing-identity guard missing: " + needle)

review_commands = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
snapshot_reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs"
if review_commands.exists() and snapshot_reader.exists():
    review_text = review_commands.read_text(encoding="utf-8")
    snapshot_text = snapshot_reader.read_text(encoding="utf-8")
    for needle in ("QS3DB4D", "ReadCurrentSpace", "CollectGeneratedHandles(previewProject)", "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)"):
        if needle not in review_text: errors.append("B4D generated-source exclusion missing: " + needle)
    for needle in ("ReadCurrentSpace", "MaxCurrentSpaceEntities"):
        if needle not in snapshot_text: errors.append("B4D bounded whole-Current-Space scan missing: " + needle)

semantic_capture = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
if semantic_capture.exists():
    text = semantic_capture.read_text(encoding="utf-8")
    for needle in ("ReplaceSourceMetric", "element.Properties.Remove(key)"):
        if needle not in text: errors.append("CAD rescan must replace stale source-derived metrics/metadata: " + needle)
    cad_prefix = 'StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)'
    if cad_prefix not in text: errors.append("CAD rescan must remove stale CAD metadata with ordinal-ignore-case prefix matching: " + cad_prefix)

ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
if ribbon.exists():
    text = ribbon.read_text(encoding="utf-8")
    for command in ("QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT", "QS3DRECOGNIZE", "QS3DRECOGNIZEAUTO", "QS3DBBSVIEW", "QS3DREVBASE", "QS3DREVDIFF"):
        if command not in text: errors.append("Ribbon workflow entry missing: " + command)
installer = ROOT / "scripts/install-bricscad-v25.ps1"
if installer.exists():
    text = installer.read_text(encoding="utf-8")
    for needle in ("Get-AuthenticodeSignature", "Get-FileHash", "ExpectedSha256"):
        if needle not in text: errors.append("BricsCAD installer verification missing: " + needle)

print("QS3D preflight")
print("root:", ROOT)
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: structure, XML/XAML handlers, bounded workflow trigger policy, proprietary/private-file guard with explicit synthetic sample provenance, QSDB v4/rules/audit/project mappings, template/recognition/revision workflow wiring, migration/persistence hardening, quantity/health/generated-solid guards, units, two-phase 3D geometry, document lifecycle, selection sync, compact palettes, Xref selection, family inheritance, finish safety, dark UI, BQ recalculation/preferences, canonical B4D generated-source exclusion and installer verification are present.")