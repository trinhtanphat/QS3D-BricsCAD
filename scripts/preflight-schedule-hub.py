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
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing Schedule Hub file: " + relative)

checks = {
    required[0]: [
        'x:Class="QS3D.BricsCAD.V25.UI.ScheduleHubWindow"', 'x:Name="ElementCountText"', 'x:Name="FinishCountText"',
        'x:Name="DoorCountText"', 'x:Name="CurtainCountText"', 'x:Name="MaterialCountText"',
        'Text="SCHEDULE-SAFE SNAPSHOT"', 'Text="CẤU KIỆN BQ"', 'Text="VẬT LIỆU"',
        'Tag="QS3DBQ"', 'Tag="QS3DREGEN"', 'Tag="QS3DFINISHSCHEDULE"', 'Tag="QS3DFINISHXLSX"',
        'Tag="QS3DROOMFINISHHEALTH"', 'Tag="QS3DMATERIALS"', 'Tag="QS3DMATERIALXLSX"',
        'Tag="QS3DCURTAIN"', 'Tag="QS3DCURTAINXLSX"', 'Tag="QS3DDOORSCHEDULE"', 'Tag="QS3DDOORXLSX"',
        'Tag="QS3DREBARHUB"', 'Tag="QS3DBBS"', 'Tag="QS3DBBSCSV"', 'Click="OnCommandClick"',
        'Text="SAME VALIDATION AS EXPORT"',
    ],
    required[1]: [
        "private readonly Document _document", "ScheduleHubWindow(Document document)", "ProjectContextCoordinator.GetOrCreate(_document)",
        "RegenerationEngine", "DependencyGraph", "RegeneratorCatalog.CreateDefault()",
        "ProjectQuantityReportBuilder.Group(project)", "RoomFinishScheduleBuilder.Build(project)",
        "DoorOpeningScheduleBuilder.Build(project)", "CurtainWallScheduleBuilder.Build(project)",
        "MaterialUsageScheduleBuilder.Build(project)", "QuantityReportMath.AddCount",
        "CountBqElements", "CountFinishElements", "CountDoorElements", "CountCurtainElements",
        "Distinct(StringComparer.OrdinalIgnoreCase)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        "số đang hiển thị được giữ nguyên", "EnsureActive", "_document.SendStringToExecute", "DrawingLabel(_document)",
    ],
    required[2]: ['CommandMethod("QS3DSCHEDULES"', "new ScheduleHubWindow(document)", "ShowModelessWindow"],
    required[3]: ['Tag="QS3DSCHEDULES"', "Schedule / Bóc khối lượng"],
    required[4]: ['Tag="QS3DSCHEDULES"', "Schedule / Bóc khối lượng Hub"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing Schedule Hub guard/token: " + needle)

code = (ROOT / required[1]).read_text(encoding="utf-8") if (ROOT / required[1]).is_file() else ""
for forbidden in ("ProjectMaterialCatalog.ReferencedMaterialNames(project)", "project.Elements.Count(x => IsRoomFinish", "project.Elements.Count(x => x.Category == ElementCategory.Door"):
    if forbidden in code: errors.append("Schedule Hub must not use raw schedule badge counting: " + forbidden)

xaml = (ROOT / required[0]).read_text(encoding="utf-8") if (ROOT / required[0]).is_file() else ""
if "QS3DBBSC SV" in xaml: errors.append("Schedule Hub must not retain the dead QS3DBBSC SV command typo")

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in (
    "QS3DSCHEDULES", "QS3DBQ", "QS3DREGEN", "QS3DFINISHSCHEDULE", "QS3DFINISHXLSX", "QS3DROOMFINISHHEALTH",
    "QS3DMATERIALS", "QS3DMATERIALXLSX", "QS3DCURTAIN", "QS3DCURTAINXLSX", "QS3DDOORSCHEDULE", "QS3DDOORXLSX",
    "QS3DREBARHUB", "QS3DBBS", "QS3DBBSCSV"):
    if commands.count(command) != 1: errors.append(command + " must be declared exactly once for Schedule Hub wiring")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: document-bound Schedule Hub uses validated builders for badges, exposes HT_Phòng repair health, and exposes all schedule/export workflows without recomputing a background DWG.")
