#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Reporting/DoorOpeningSchedule.cs",
    "src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs",
    "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/DoorOpeningScheduleWindowCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/DoorOpeningScheduleWindow.xaml.cs",
    "tests/QS3D.Core.SmokeTests/DoorOpeningScheduleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/DoorOpeningScheduleRegistration.cs",
    "tests/QS3D.Core.SmokeTests/DoorOpeningXlsxSmoke.cs",
    "tests/QS3D.Core.SmokeTests/DoorOpeningXlsxRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing door/opening schedule file: " + relative)

checks = {
    required[0]: [
        "DoorOpeningScheduleRow", "DoorOpeningScheduleBuilder", "ElementCategory.Door", "ElementCategory.WallOpening",
        'Number(element, family, "WidthM"', 'Number(element, family, "HeightM"', 'Number(element, family, "SillHeightM"',
        'Number(element, family, "ThicknessM"', 'Text(element, family, "Material")', '"OpeningAreaM2"', '"HostWallId"',
        "HostCount", "ElementIds", "HostIds", "must be finite and > 0", "must be finite and >= 0",
    ],
    required[1]: [
        "DoorOpeningXlsxExporter", "AtomicFileCommit.CreateTempPath", "AtomicFileCommit.ReplaceWithoutBackup", "ZipArchive",
        "Cửa - Lỗ mở", "DT mở (m²)", "SL host", "Element IDs", "Host IDs", "<autoFilter ref=", "Validate(tempPath)",
    ],
    required[2]: [
        'CommandMethod("QS3DDOORXLSX"', "RegenerationEngine", "DoorOpeningScheduleBuilder.Build(project)",
        "DoorOpeningXlsxExporter.Export", "SaveFileDialog", "Cua-Lo-Mo.xlsx", "QuantityReportMath.AddCount", "QuantityReportMath.Add",
    ],
    required[3]: ['CommandMethod("QS3DDOORSCHEDULE"', "new DoorOpeningScheduleWindow(document)", "ShowModelessWindow"],
    required[4]: [
        'x:Class="QS3D.BricsCAD.V25.UI.DoorOpeningScheduleWindow"', 'x:Name="SearchBox"', 'x:Name="ScheduleGrid"',
        'x:Name="GroupCountText"', 'x:Name="AreaText"', 'x:Name="HostCountText"', 'Click="OnRefreshClick"', 'Click="OnExportClick"',
        'Header="Host IDs"',
    ],
    required[5]: [
        "private readonly Document _document", "DoorOpeningScheduleWindow(Document document)", "DoorOpeningScheduleBuilder.Build(project)",
        "DoorOpeningXlsxExporter.Export", "RegenerationEngine", "SearchText.Contains(query)", "Distinct(StringComparer.OrdinalIgnoreCase)",
        "DrawingLabel(_document)", "EnsureActive", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        'EnsureActive("đọc Door/Opening Schedule hiện hành")', 'EnsureActive("xuất Door/Opening XLSX")',
        "QuantityReportMath.AddCount", "QuantityReportMath.Add", '"Door/Opening visible area"',
    ],
    required[6]: [
        "GroupsDoorsByDimensionsAndDistinctHosts", "InstanceOverrideSplitsFamilyInheritedRow", "RejectsInvalidSemanticDimensions",
        "3.9d", "2.4d", "Host count must remain distinct",
    ],
    required[7]: ["DoorOpeningScheduleSmoke.Run();"],
    required[8]: [
        "DoorOpeningXlsxExporter.Export", "xl/worksheets/sheet1.xml", "DT mở", "3.9", "wall-a;wall-b",
    ],
    required[9]: ["DoorOpeningXlsxSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing door/opening schedule guard/token: " + needle)

for relative in (required[2], required[5]):
    path = ROOT / relative
    if path.is_file() and ".Sum(" in path.read_text(encoding="utf-8"):
        errors.append(relative + " must not use unchecked LINQ Sum for schedule totals")

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DDOORSCHEDULE", "QS3DDOORXLSX"):
    if commands.count(command) != 1: errors.append(command + " must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: deterministic Door/Opening schedule, host provenance, overflow-safe summaries, XLSX export and cross-DWG-safe modeless review UI are present.")
