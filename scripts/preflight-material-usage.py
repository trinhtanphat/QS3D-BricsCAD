#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs",
    "src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs",
    "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml",
    "tests/QS3D.Core.SmokeTests/MaterialUsageScheduleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/MaterialUsageScheduleRegistration.cs",
    "tests/QS3D.Core.SmokeTests/MaterialUsageXlsxSmoke.cs",
    "tests/QS3D.Core.SmokeTests/MaterialUsageXlsxRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing material usage file: " + relative)

checks = {
    "src/QS3D.Core/Reporting/MaterialUsageSchedule.cs": [
        "MaterialUsageRow", "MaterialUsageScheduleBuilder", "PrimaryQuantity",
        'if (unit == "m") return LengthM', 'if (unit == "m2") return AreaM2',
        'if (unit == "m3") return VolumeM3', 'if (unit == "kg") return MassKg',
        "ProjectMaterialCatalog.GetAll(project)", "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)",
        'Effective(element, family, "Material")', 'Effective(element, family, "CurtainFrameMaterial")',
        '"CurtainFrame"', '"CurtainNetGlassAreaM2"', '"CurtainFrameLengthM"',
        "element.Properties.TryGetValue(key", "family.Properties.TryGetValue(key", "ElementIds.Add(element.Id)",
        "must be finite and non-negative",
    ],
    "src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs": [
        "MaterialUsageXlsxExporter", "AtomicFileCommit.CreateTempPath", "AtomicFileCommit.ReplaceWithoutBackup",
        "ZipArchive", "KL chính", "Diện tích (m²)", "Thể tích (m³)", "Khối lượng (kg)",
        "PrimaryQuantity", "<autoFilter ref=", "Validate(tempPath)", "Vật liệu",
    ],
    "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs": [
        'CommandMethod("QS3DMATERIALXLSX"', "RegenerationEngine", "MaterialUsageScheduleBuilder.Build(project)",
        "MaterialUsageXlsxExporter.Export", "SaveFileDialog", "Vat-Lieu.xlsx",
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml": [
        'Content="Xuất bảng vật liệu"', 'Click="OnExportClick"', "material usage XLSX",
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": [
        "OnExportClick", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        '_document.SendStringToExecute("QS3DMATERIALXLSX "',
    ],
    "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml": [
        'Tag="QS3DMATERIALXLSX"', 'Content="Xuất bảng vật liệu"',
    ],
    "tests/QS3D.Core.SmokeTests/MaterialUsageScheduleSmoke.cs": [
        "FamilyInheritanceAndCurtainComponents", "InstanceOverrideUsesCatalogUnit", "RejectsInvalidQuantities",
        "14.4d", "33d", "22d",
    ],
    "tests/QS3D.Core.SmokeTests/MaterialUsageScheduleRegistration.cs": ["MaterialUsageScheduleSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/MaterialUsageXlsxSmoke.cs": [
        "MaterialUsageXlsxExporter.Export", "xl/worksheets/sheet1.xml", "KL chính", "22.5", "Kính",
    ],
    "tests/QS3D.Core.SmokeTests/MaterialUsageXlsxRegistration.cs": ["MaterialUsageXlsxSmoke.Run();"],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing material usage guard/token: " + needle)

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DMATERIALXLSX") != 1:
    errors.append("QS3DMATERIALXLSX must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: material-aware Family/Instance usage schedule, curtain-frame split, catalog-unit primary quantities, provenance, real XLSX export and document-bound UI entry points are present.")
