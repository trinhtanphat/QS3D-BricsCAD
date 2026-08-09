#!/usr/bin/env python3
from pathlib import Path
import re, sys, xml.etree.ElementTree as ET
ROOT = Path(__file__).resolve().parents[1]; errors=[]
required=["Directory.Build.props","README.md","scripts/package-v25.ps1","docs/COMMANDS.md","docs/RUNTIME-TEST-CHECKLIST.md","src/QS3D.Core/QS3D.Core.csproj","src/QS3D.Core/Domain/ProjectState.cs","src/QS3D.Core/Domain/ProjectElement.cs","src/QS3D.Core/Persistence/QsdbProjectStore.cs","src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs","src/QS3D.Core/Persistence/ProjectLoadResult.cs","src/QS3D.Core/Diagnostics/ModelHealthService.cs","src/QS3D.Core/Rules/QuantityRuleEngine.cs","src/QS3D.Core/Services/DependencyGraph.cs","src/QS3D.Core/Services/HostLinkService.cs","src/QS3D.Core/Services/StructuralQuantityCalculator.cs","src/QS3D.Core/Services/StructuralRegenerators.cs","src/QS3D.Core/Services/GenericQuantityRegenerator.cs","src/QS3D.Core/Rebar/RebarSchedule.cs","src/QS3D.Core/Rebar/RebarRegenerator.cs","src/QS3D.Core/Recognition/RecognitionEngine.cs","src/QS3D.Core/Revisions/QuantityRevisionReport.cs","src/QS3D.Core/Revisions/RevisionSnapshotStore.cs","src/QS3D.Core/Export/RebarCsvExporter.cs","src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs","src/QS3D.Core/Export/XlsxQuantityExporter.cs","src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj","src/QS3D.BricsCAD.V25/DomainCommands.cs","src/QS3D.BricsCAD.V25/Services/RevisionCoordinator.cs","src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs","src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs","src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs","src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs","src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs","src/QS3D.BricsCAD.V25/Cad/LayerVisibilityService.cs","src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml","src/QS3D.BricsCAD.V25/UI/RightPanel.xaml","src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml","src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml","src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml","src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml","src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml","tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj","tests/QS3D.Core.SmokeTests/PersistenceHardeningSmoke.cs","tests/QS3D.Core.SmokeTests/FullDomainSmoke.cs","tests/QS3D.Core.SmokeTests/DomainHealthSmoke.cs",".github/workflows/ci.yml",".github/workflows/bricscad-v25.yml"]
for rel in required:
    if not (ROOT/rel).exists(): errors.append(f"missing required file: {rel}")
for path in list(ROOT.rglob("*.csproj"))+list(ROOT.rglob("*.xaml")):
    try: ET.parse(path)
    except Exception as exc: errors.append(f"invalid XML {path.relative_to(ROOT)}: {exc}")
for bad in ("BrxMgd.dll","TD_Mgd.dll","TD_MgdBrep.dll"):
    if list(ROOT.rglob(bad)): errors.append(f"proprietary BricsCAD assembly must not be committed: {bad}")
for ext in ("*.dwg","*.dxf","*.docx"):
    found=[p.relative_to(ROOT) for p in ROOT.rglob(ext)]
    if found: errors.append(f"private/reference artifact must not be committed in public repo ({ext}): {found}")
plugin=ROOT/"src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
if plugin.exists():
    text=plugin.read_text(encoding="utf-8")
    for needle,message in {"<TargetFramework>net48</TargetFramework>":"plugin must target net48","$(BRICSCAD_V25_DIR)\\BrxMgd.dll":"plugin must use external BrxMgd reference","<Private>false</Private>":"BricsCAD references must not be copied locally"}.items():
        if needle not in text: errors.append(message)
for workflow in (ROOT/".github/workflows").glob("*.yml"):
    text=workflow.read_text(encoding="utf-8")
    if "workflow_dispatch:" not in text: errors.append(f"{workflow.name}: must be manual-only")
    if re.search(r"(?m)^\s*(push|pull_request)\s*:",text): errors.append(f"{workflow.name}: automatic trigger forbidden before V25 runtime gate")
for path in ROOT.rglob("*"):
    if path.is_dir() and path.name.lower() in {"blt","blt3d"}: errors.append(f"vendor folder must not be committed: {path.relative_to(ROOT)}")
for xaml in ROOT.rglob("*.xaml"):
    if xaml.name=="Theme.xaml": continue
    code=xaml.with_suffix(xaml.suffix+".cs")
    if not code.exists(): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind file"); continue
    xt=xaml.read_text(encoding="utf-8"); ct=code.read_text(encoding="utf-8")
    for handler in set(re.findall(r'\b(?:Click|TextChanged|SelectionChanged|Checked|Unchecked|MouseDoubleClick)="([A-Za-z_][A-Za-z0-9_]*)"',xt)):
        if not re.search(r"\b"+re.escape(handler)+r"\s*\(",ct): errors.append(f"{xaml.relative_to(ROOT)}: missing code-behind handler {handler}")
for path in ROOT.rglob("*.cs"):
    raw=path.read_text(encoding="utf-8"); text=re.sub(r"//.*?$|/\*.*?\*/|(?:\$|@|\$@|@\$)?\"(?:\"\"|\\.|[^\"\\])*\"|'(?:\\.|[^'\\])'",'',raw,flags=re.M|re.S); pairs={'{':'}','(':')','[':']'}; stack=[]
    for ch in text:
        if ch in pairs: stack.append(pairs[ch])
        elif ch in pairs.values():
            if not stack or stack.pop()!=ch: errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter near '{ch}'"); break
    else:
        if stack: errors.append(f"{path.relative_to(ROOT)}: unbalanced delimiter(s)")
for path in (ROOT/"src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text=path.read_text(encoding="utf-8")
    if ".ToHashSet(" in text: errors.append(f"{path.relative_to(ROOT)}: avoid ToHashSet in net48 adapter")
    if "FormulaEngine" in text: errors.append(f"{path.relative_to(ROOT)}: FormulaEngine does not exist")
for path in ROOT.rglob("*.cs"):
    text=path.read_text(encoding="utf-8")
    if "foundation is ready" in text or "after the first runtime gate" in text: errors.append(f"{path.relative_to(ROOT)}: placeholder UX text must not ship")
package=(ROOT/"scripts/package-v25.ps1").read_text(encoding="utf-8") if (ROOT/"scripts/package-v25.ps1").exists() else ""
for forbidden in ("BrxMgd.dll","TD_Mgd.dll","TD_MgdBrep.dll"):
    if forbidden not in package: errors.append(f"package-v25.ps1: missing proprietary DLL guard for {forbidden}")
print("QS3D preflight"); print("root:",ROOT)
if errors:
    [print("ERROR:",e) for e in errors]; print(f"FAILED with {len(errors)} error(s)."); sys.exit(1)
print("PASS: full-domain tree, XML/XAML handlers, delimiters, proprietary-file, net48, packaging and manual-CI guards are clean.")
