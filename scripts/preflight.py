#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "Directory.Build.props", "README.md", "AGENTS.md", "CI_POLICY.md",
    "src/QS3D.Core/QS3D.Core.csproj", "src/QS3D.Core/Persistence/QsdbProjectStore.cs",
    "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs", "src/QS3D.Core/Services/RegenerationEngine.cs",
    "src/QS3D.Core/Services/BulkEditService.cs", "src/QS3D.Core/Services/WallQuantityCalculator.cs",
    "src/QS3D.Core/Takeoff/QuantityEngine.cs", "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.Core/Export/RebarCsvExporter.cs", "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
    "src/QS3D.BricsCAD.V25/Commands.cs", "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs", "src/QS3D.BricsCAD.V25/DomainHubCommands.cs",
    "src/QS3D.BricsCAD.V25/ViewportCommands.cs", "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs",
    "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs", "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs",
    "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs", "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/XrefService.cs", "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml", "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml", "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml", "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs", "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs", "scripts/install-bricscad-v25.ps1",
    "scripts/package-v25.ps1", ".github/workflows/ci.yml", ".github/workflows/bricscad-v25.yml"
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing required file: " + rel)

for path in list(ROOT.rglob("*.csproj")) + list(ROOT.rglob("*.xaml")):
    try: ET.parse(path)
    except Exception as exc: errors.append(f"invalid XML {path.relative_to(ROOT)}: {exc}")

for bad in ("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"):
    if list(ROOT.rglob(bad)): errors.append("proprietary BricsCAD assembly must not be committed: " + bad)
for ext in ("*.dwg", "*.dxf", "*.docx"):
    found = [str(p.relative_to(ROOT)) for p in ROOT.rglob(ext)]
    if found: errors.append(f"private/reference artifact must not be committed ({ext}): {found}")
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt", "blt3d"}: errors.append("vendor folder must not be committed: " + str(path.relative_to(ROOT)))

plugin = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if plugin.exists():
    text = plugin.read_text(encoding="utf-8")
    for needle, message in {
        "<TargetFramework>net48</TargetFramework>": "plugin must target net48",
        "$(BRICSCAD_V25_DIR)\\BrxMgd.dll": "plugin must use external BrxMgd reference",
        "<Private>false</Private>": "BricsCAD references must not be copied locally",
    }.items():
        if needle not in text: errors.append(message)

for workflow in (ROOT / ".github/workflows").glob("*.yml"):
    text = workflow.read_text(encoding="utf-8")
    if "workflow_dispatch:" not in text: errors.append(f"{workflow.name}: must remain manual-only")
    if re.search(r"(?m)^\s*(push|pull_request)\s*:", text): errors.append(f"{workflow.name}: automatic trigger forbidden before real V25 runtime gate")

for xaml in ROOT.rglob("*.xaml"):
    if xaml.name == "Theme.xaml": continue
    code = xaml.with_suffix(xaml.suffix + ".cs")
    if not code.exists(): continue
    xt = xaml.read_text(encoding="utf-8")
    ct = code.read_text(encoding="utf-8")
    for handler in set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged|SelectedItemChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"', xt)):
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", ct): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")

command_owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("([^\"]+)"', text):
        command_owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, owners in sorted(command_owners.items()):
    if len(owners) > 1: errors.append("duplicate CommandMethod " + command + ": " + ", ".join(owners))

store = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
if store.exists():
    text = store.read_text(encoding="utf-8")
    for needle, message in {
        "DtdProcessing = DtdProcessing.Prohibit": "QSDB DTD hardening missing",
        "XmlResolver = null": "QSDB external XML resolver must stay disabled",
        "MaxCharactersInDocument": "QSDB XML character limit missing",
        "MaxProjectFileBytes": "QSDB file-size guard missing",
        "RestorePersistenceState": "QSDB dirty-state restore missing",
        "ValidateProject(project);": "QSDB must validate in-memory state before replacing the persisted project",
        "AtomicFileCommit.ReplaceWithBackup": "QSDB atomic replacement/recovery helper missing",
        "double.IsNaN(quantity.Value)": "QSDB non-finite quantity validation missing",
        "double.IsNaN(floor.ElevationM)": "QSDB non-finite floor validation missing",
    }.items():
        if needle not in text: errors.append(message)

migrator = ROOT / "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs"
if migrator.exists():
    text = migrator.read_text(encoding="utf-8")
    if "ElementDirtyFlags.All" not in text or 'element.SetAttributeValue("updatedUtc", LegacyUpdatedUtc)' not in text:
        errors.append("legacy QSDB elements must migrate dirty and require deterministic regeneration")

bulk = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
if bulk.exists():
    text = bulk.read_text(encoding="utf-8")
    if "inheritedKeys" not in text or "previousFamily.Properties" not in text:
        errors.append("family reassignment must refresh inherited defaults without overwriting instance overrides")

wall_quantity = ROOT / "src/QS3D.Core/Services/WallQuantityCalculator.cs"
if wall_quantity.exists():
    text = wall_quantity.read_text(encoding="utf-8")
    if "RequireFiniteNonNegative" not in text or "FiniteProduct" not in text:
        errors.append("legacy wall quantity path must reject non-finite dimensions and overflow")

takeoff = ROOT / "src/QS3D.Core/Takeoff/QuantityEngine.cs"
if takeoff.exists() and "ConvertMetric" not in takeoff.read_text(encoding="utf-8"):
    errors.append("raw snapshot takeoff must reject negative/non-finite metrics")

hardening = ROOT / "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs"
if hardening.exists() and "QsdbRejectsDtd();" not in hardening.read_text(encoding="utf-8"): errors.append("DTD rejection regression coverage missing")
continuation = ROOT / "tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs"
if continuation.exists():
    text = continuation.read_text(encoding="utf-8")
    for needle in ("LegacyMigrationMarksElementsDirty();", "FamilyAssignmentRefreshesInheritedDefaults();", "QsdbRejectsNonFiniteStateBeforeReplace();", "LegacyWallCalculatorRejectsNonFiniteValues();", "QuantityEngineRejectsInvalidSnapshotMetrics();"):
        if needle not in text: errors.append("continuation regression coverage missing: " + needle)
completion = ROOT / "tests/QS3D.Core.SmokeTests/CompletionRegressionSmoke.cs"
if completion.exists():
    text = completion.read_text(encoding="utf-8")
    for needle in ("StairQuantities();", "RailingQuantities();", "EarthworkQuantities();", "CsvIsExcelSafeAndFinite();", "VietnameseRecognition();"):
        if needle not in text: errors.append("completion regression coverage missing: " + needle)

units = ROOT / "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs"
if units.exists():
    text = units.read_text(encoding="utf-8")
    for needle in ("LengthUnit.Inch", "LengthUnit.Foot", "LengthUnit.Millimeter", "LengthUnit.Centimeter", "LengthUnit.Meter", "LengthUnit.Yard", "GetDrawingUnit"):
        if needle not in text: errors.append("CAD unit mapping incomplete: " + needle)

geometry = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs"
if geometry.exists():
    text = geometry.read_text(encoding="utf-8")
    if "as Solid3d" not in text: errors.append("generated geometry cleanup must only erase tracked Solid3d objects")
    if "CommitReplacement" not in text or "PrepareReplacement" not in text: errors.append("generated geometry must use two-phase CAD/metadata replacement")
    prepare = text.split("public static void CommitReplacement", 1)[0]
    if "element.Properties.Remove" in prepare: errors.append("generated metadata must not mutate before CAD transaction commit")

for rel in ("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"):
    path = ROOT / rel
    if path.exists():
        text = path.read_text(encoding="utf-8")
        if "transaction.Commit();" not in text or "CommitReplacement" not in text: errors.append(rel + ": two-phase generated geometry commit missing")
        if "double.IsNaN" not in text or "double.IsInfinity" not in text: errors.append(rel + ": non-finite dimension guard missing")

structural_solid = ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs"
if structural_solid.exists():
    text = structural_solid.read_text(encoding="utf-8")
    for needle in ("ElementCategory.Stair", "ElementCategory.Railing", "ElementCategory.Earthwork", "DownwardFootprintMass"):
        if needle not in text: errors.append("full-domain native mass adapter missing: " + needle)

context = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
if context.exists():
    text = context.read_text(encoding="utf-8")
    if "Dictionary<Document, ProjectState>" not in text: errors.append("project cache must use Document identity so Save As cannot orphan in-memory project state")
    if "SyncDrawingIdentity" not in text: errors.append("project cache must synchronize drawing identity after Save As")
    if "SafeFileStem" not in text: errors.append("unsaved drawing project path must sanitize the local filename")
    if "GetKey(Document" in text: errors.append("project cache must not key live documents by mutable document.Name")

selection_sync = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
if selection_sync.exists():
    text = selection_sync.read_text(encoding="utf-8")
    if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in text: errors.append("selection sync must ignore inactive documents")

palette = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
if palette.exists():
    text = palette.read_text(encoding="utf-8")
    if "MinimumSize = new Size(460, 420)" not in text: errors.append("workspace PaletteSet minimum width must match compact BLT workspace target")
    if "MinimumSize = new Size(520, 420)" in text: errors.append("workspace PaletteSet still forces the old oversized minimum width")

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
if workspace_vm.exists():
    text = workspace_vm.read_text(encoding="utf-8")
    if "TryFiniteNumber" not in text: errors.append("Family dimensional property validation missing")
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
if quantity.exists() and "_recalculate" not in quantity.read_text(encoding="utf-8"): errors.append("BQ Tính lại must have a real recalculation callback")
if commands.exists():
    text = commands.read_text(encoding="utf-8")
    if "new QuantitySummaryWindow(rows, locate, recalculate)" not in text: errors.append("BQ command does not wire recalculation callback")
    if "CadUnitService.GetDrawingUnit(doc)" not in text: errors.append("BQ snapshot fallback still assumes millimeters")

csv_exporter = ROOT / "src/QS3D.Core/Export/RebarCsvExporter.cs"
if csv_exporter.exists():
    text = csv_exporter.read_text(encoding="utf-8")
    if "TrimStart" not in text or "double.IsNaN" not in text: errors.append("BBS CSV injection/non-finite guards missing")

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
print("PASS: structure, XML/XAML handlers, unique commands, manual CI, proprietary-file guard, QSDB migration/persistence hardening, units, two-phase/full-domain 3D geometry, document lifecycle, active-document selection sync, compact palettes, Xref selection, family inheritance, finish safety, dark UI, BQ recalculation, BBS CSV and installer verification are present.")
