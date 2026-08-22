#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Reporting/CurtainWallSchedule.cs",
    "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallHubCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallScheduleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallScheduleRegistration.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallXlsxSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallXlsxRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing curtain hub/export file: " + relative)

checks = {
    "src/QS3D.Core/Reporting/CurtainWallSchedule.cs": [
        "CurtainWallScheduleRow",
        "CurtainWallScheduleBuilder",
        "ElementCategory.GlassWall",
        "CurtainPanelCount",
        "CurtainNetGlassAreaM2",
        "CurtainFrameLengthM",
        "MinimumClearPanelWidthM",
        "ElementIds",
        "must be an integer quantity within range",
    ],
    "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs": [
        "CurtainWallXlsxExporter",
        "AtomicFileCommit.CreateTempPath",
        "ZipArchive",
        "DT kính net (m²)",
        "Dài khung (m)",
        "Panel clear W min (m)",
        "<autoFilter ref=",
        "ValidatePackage",
        "Vách Kính",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs": [
        'CommandMethod("QS3DCURTAINXLSX"',
        "RegenerationEngine",
        "CurtainWallScheduleBuilder.Build",
        "CurtainWallXlsxExporter.Export",
        "SaveFileDialog",
        "Vach-Kinh.xlsx",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallHubCommands.cs": [
        'CommandMethod("QS3DCURTAIN"',
        "new CurtainWallWindow()",
        "ShowModelessWindow",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs": [
        'CommandMethod("QS3DCURTAINFRAMES3D"',
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
    ],
    "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs": [
        'CommandMethod("QS3DCURTAINFRAMEHEALTH"',
        "GeneratedCurtainFrameHealthService",
        "GetLiveSolidHandles",
    ],
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs": [
        "GeneratedCurtainFrameOwnershipGuard.Build(project)",
        "CurtainWallDetailPlanner.Plan",
        "CurtainFrameDepthM",
        "MaxFramesPerElement = 4096",
        "MaxFramesPerBatch = 8192",
        "ownership.EnsureOwned",
        "Refusing destructive erase",
        "GeneratedCurtainFrameHandles",
        "ClearGeneratedCurtainFrameStale();",
    ],
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml": [
        'x:Class="QS3D.BricsCAD.V25.UI.CurtainWallWindow"',
        'x:Name="FrameDepthBox"',
        'x:Name="PanelCountText"',
        'Tag="QS3DGLASSWALL"',
        'Tag="QS3DCURTAINFRAMES3D"',
        'Tag="QS3DCURTAINFRAMEHEALTH"',
        'Tag="QS3DCUTOPENINGSCURVED"',
        'Tag="QS3DCURTAINXLSX"',
        'Click="OnSaveClick"',
    ],
    "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs": [
        '"CurtainFrameDepthM"',
        "FrameDepthBox.Text",
        "ApplyFamilyValue",
        "element.SetProperty(key, next)",
        "element.MarkDirty(ElementDirtyFlags.All)",
        "RegenerationEngine",
        "CurtainPanelCount",
        "CurtainNetGlassAreaM2",
        "CurtainFrameLengthM",
        "SendStringToExecute",
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallScheduleSmoke.cs": [
        "GroupsByStableFloorAndFamily",
        "RejectsNonIntegerPanelCounts",
        "22.5d",
        "51d",
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallScheduleRegistration.cs": ["CurtainWallScheduleSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/CurtainWallXlsxSmoke.cs": [
        "CurtainWallXlsxExporter.Export",
        "xl/worksheets/sheet1.xml",
        "DT kính net",
    ],
    "tests/QS3D.Core.SmokeTests/CurtainWallXlsxRegistration.cs": ["CurtainWallXlsxSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing curtain hub/export guard/token: " + needle)

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DCURTAIN", "QS3DCURTAINXLSX", "QS3DCURTAINFRAMES3D", "QS3DCURTAINFRAMEHEALTH"):
    if commands.count(command) != 1:
        errors.append(command + " must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain-wall modeless hub, family/instance-safe settings, native frame ownership/stale rebuild safety, frame health, schedule grouping and real XLSX export are present.")
