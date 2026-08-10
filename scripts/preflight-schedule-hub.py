#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/ScheduleHubCommands.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing Schedule Hub file: " + relative)

checks = {
    required[0]: [
        'x:Class="QS3D.BricsCAD.V25.UI.ScheduleHubWindow"', 'x:Name="ElementCountText"', 'x:Name="DoorCountText"',
        'x:Name="CurtainCountText"', 'x:Name="MaterialCountText"', 'Tag="QS3DBQ"', 'Tag="QS3DMATERIALS"',
        'Tag="QS3DMATERIALXLSX"', 'Tag="QS3DCURTAIN"', 'Tag="QS3DCURTAINXLSX"', 'Tag="QS3DDOORSCHEDULE"',
        'Tag="QS3DDOORXLSX"', 'Tag="QS3DREBARHUB"', 'Tag="QS3DBBS"', 'Tag="QS3DBBSCSV"', 'Click="OnCommandClick"',
        "Schedule Hub khóa theo bản vẽ đã mở",
    ],
    required[1]: [
        "private readonly Document _document", "ScheduleHubWindow(Document document)", "ProjectContextCoordinator.GetOrCreate(_document)",
        "ElementCategory.Door", "ElementCategory.WallOpening", "ElementCategory.GlassWall", "ProjectMaterialCatalog.ReferencedMaterialNames(project)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)", "EnsureActive", "_document.SendStringToExecute", "DrawingLabel(_document)",
    ],
    required[2]: ['CommandMethod("QS3DSCHEDULES"', "new ScheduleHubWindow(document)", "ShowModelessWindow"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing Schedule Hub guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in (
    "QS3DSCHEDULES", "QS3DBQ", "QS3DMATERIALS", "QS3DMATERIALXLSX", "QS3DCURTAIN", "QS3DCURTAINXLSX",
    "QS3DDOORSCHEDULE", "QS3DDOORXLSX", "QS3DREBARHUB", "QS3DBBS", "QS3DBBSCSV"):
    if commands.count(command) != 1: errors.append(command + " must be declared exactly once for Schedule Hub wiring")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: document-bound Schedule Hub exposes BQ, material, curtain, door/opening and rebar schedule/export commands with live project snapshot.")
