#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs",
    "src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs",
    "src/QS3D.BricsCAD.V25/PluginEntry.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing Project Tools file: " + relative)

checks = {
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.ProjectToolsWindow"', 'x:Name="ProjectNameText"', 'x:Name="FloorText"',
        'Tag="QS3DLEVELS"', 'Tag="QS3DZONES"', 'Tag="QS3DMATERIALS"', 'Tag="QS3DMATERIALXLSX"',
        'Tag="QS3DTEMPLATEEXPORT"', 'Tag="QS3DTEMPLATEIMPORT"', 'Tag="QS3DCURTAIN"', 'Tag="QS3DGEOMETRYEXT"',
        'Tag="QS3DREBARHUB"', 'Tag="QS3DBQ"', 'Tag="QS3DHEALTHALL"', 'Tag="QS3DAUDIT"', 'Click="OnCommandClick"',
        "Cửa sổ khóa theo bản vẽ đã mở",
    ],
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs": [
        "private readonly Document _document", "ProjectToolsWindow(Document document)", "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)", "EnsureBoundDrawingIsActive",
        "project.ActiveFloorId", "project.Families.Count", "project.Elements.Count", "_document.SendStringToExecute", "Activated +=", "DrawingLabel(_document)",
    ],
    "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs": [
        'CommandMethod("QS3DPROJECTTOOLS"', "new ProjectToolsWindow(document)", "ShowModelessWindow", "khóa theo bản vẽ",
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs": [
        'TabId = "QS3D_PROJECT"',
        'new ButtonSpec("QS3D_PROJECT_PROJECTTOOLS", "Project Tools", "QS3DPROJECTTOOLS")',
        'new ButtonSpec("QS3D_PROJECT_LEVELS", "Tầng / Cao độ", "QS3DLEVELS")',
        'new ButtonSpec("QS3D_PROJECT_ZONES", "Khu vực / Zone", "QS3DZONES")',
        'new ButtonSpec("QS3D_PROJECT_MATERIALS", "Vật liệu", "QS3DMATERIALS")',
        "CollectionContainsId", "CommandParameter", "CommandHandler", "SendStringToExecute",
    ],
    "src/QS3D.BricsCAD.V25/PluginEntry.cs": [
        "RibbonBootstrapper.TryInitialize();", "ProjectRibbonAugmenter.TryInitialize();", "ProjectRibbonAugmenter.Reset();", "RibbonBootstrapper.Reset();",
    ],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing Project Tools guard/token: " + needle)

code = ROOT / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs"
if code.is_file() and "var document = Application.DocumentManager.MdiActiveDocument" in code.read_text(encoding="utf-8"):
    errors.append("ProjectToolsWindow must not switch project ownership through MdiActiveDocument inside modeless event handlers")
if code.is_file() and "ProjectContextCoordinator.GetOrCreate(_document)" in code.read_text(encoding="utf-8"):
    errors.append("ProjectToolsWindow refresh must not create or replace project state")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in (
    "QS3DPROJECTTOOLS", "QS3DLEVELS", "QS3DZONES", "QS3DMATERIALS", "QS3DMATERIALXLSX", "QS3DTEMPLATEEXPORT", "QS3DTEMPLATEIMPORT",
    "QS3DSAVE", "QS3DRELOAD", "QS3DCURTAIN", "QS3DGEOMETRYEXT", "QS3DREBARHUB", "QS3DBQ",
    "QS3DREGEN", "QS3DHEALTHALL", "QS3DAUDIT", "QS3D", "QS3DREFRESH"):
    if commands.count(command) != 1: errors.append(command + " must be declared exactly once for Project Tools wiring")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: document-bound Project Tools hub, Floor/Zone/Material setup, material XLSX, command wiring and additive QS3D_PROJECT ribbon shortcuts are present without rewriting the base ribbon spec.")
