#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "Directory.Build.props", "README.md", "AGENTS.md", "docs/CI_POLICY.md",
    "src/QS3D.Core/QS3D.Core.csproj", "src/QS3D.Core/Persistence/QsdbProjectStore.cs",
    "src/QS3D.Core/Services/RegenerationEngine.cs", "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj", "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs", "src/QS3D.BricsCAD.V25/ViewportCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/CadUnitService.cs", "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/XrefService.cs", "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml", "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml", "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml", "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs",
    "scripts/install-bricscad-v25.ps1", ".github/workflows/ci.yml", ".github/workflows/bricscad-v25.yml"
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
    for handler in set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"', xt)):
        if not re.search(r"\b" + re.escape(handler) + r"\s*\(", ct): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")

store = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
if store.exists():
    text = store.read_text(encoding="utf-8")
    for needle, message in {
        "DtdProcessing = DtdProcessing.Prohibit": "QSDB DTD hardening missing",
        "XmlResolver = null": "QSDB external XML resolver must stay disabled",
        "MaxCharactersInDocument": "QSDB XML character limit missing",
        "MaxProjectFileBytes": "QSDB file-size guard missing",
        "RestorePersistenceState": "QSDB dirty-state restore missing",
    }.items():
        if needle not in text: errors.append(message)

hardening = ROOT / "tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs"
if hardening.exists() and "QsdbRejectsDtd();" not in hardening.read_text(encoding="utf-8"): errors.append("DTD rejection regression coverage missing")

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
print("PASS: structure, XML/XAML handlers, manual CI, proprietary-file guard, QSDB hardening, units, two-phase 3D geometry, Xref selection, family validation, finish safety, dark UI, BQ recalculation and installer verification are present.")
