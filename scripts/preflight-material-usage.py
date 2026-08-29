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
    if not (ROOT / relative).is_file(): errors.append("missing material usage file: " + relative)

checks = {
    required[0]: [
        "MaterialUsageRow", "MaterialUsageScheduleBuilder", "PrimaryQuantity",
        'if (unit == "m") return LengthM', 'if (unit == "m2") return AreaM2',
        'if (unit == "m3") return VolumeM3', 'if (unit == "kg") return MassKg',
        "ProjectMaterialCatalog.GetAll(project)", "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)",
        'Effective(element, family, "Material")', 'Effective(element, family, "CurtainFrameMaterial")',
        '"CurtainFrame"', '"CurtainNetGlassAreaM2"', '"CurtainFrameLengthM"',
        "element.Properties.TryGetValue(key", "family.Properties.TryGetValue(key", "ElementIds.Add(element.Id)",
        "must be finite and non-negative", "private static double QFirst(ProjectElement element, params string[] keys)",
        "element.Quantities.ContainsKey(key)", "QuantityReportMath.AddCount", "QuantityReportMath.Add",
        'QFirst(element, "NetVolumeM3", "VolumeM3")',
        'QFirst(element, "CurtainNetGlassAreaM2", "NetWallAreaM2")',
        'QFirst(element, "NetWallAreaM2", "SideAreaM2")',
        'QFirst(element, "NetFinishAreaM2", "SideAreaM2", "AreaM2")',
        'QFirst(element, "BottomAreaM2", "AreaM2")',
        'QFirst(element, "TopAreaM2", "AreaM2")',
        'QFirst(element, "SkirtingLengthM", "InnerPerimeterM", "PerimeterM", "LengthM")',
        'QFirst(element, "OpeningAreaM2", "AreaM2")',
    ],
    required[1]: [
        "MaterialUsageXlsxExporter", "AtomicFileCommit.CreateTempPath", "AtomicFileCommit.ReplaceWithoutBackup",
        "ZipArchive", "KL chính", "Diện tích (m²)", "Thể tích (m³)", "Khối lượng (kg)",
        "PrimaryQuantity", "source.PrimaryQuantity != snapshot.PrimaryQuantity", "<autoFilter ref=", "Validate(tempPath)", "Vật liệu", "inlineStr",
    ],
    required[2]: [
        'CommandMethod("QS3DMATERIALXLSX"', "RegenerationEngine", "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)", "RegenerateDirty(snapshot)", "MaterialUsageScheduleBuilder.Build(snapshot)",
        "MaterialUsageXlsxExporter.Export", "SaveFileDialog", "Vat-Lieu.xlsx", "QuantityReportMath.AddCount",
    ],
    required[3]: ['Content="Xuất bảng vật liệu"', 'Click="OnExportClick"', "material usage XLSX"],
    required[4]: ["OnExportClick", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)", '_document.SendStringToExecute("QS3DMATERIALXLSX "'],
    required[5]: ['Tag="QS3DMATERIALXLSX"', 'Content="Xuất bảng vật liệu"'],
    required[6]: [
        "FamilyInheritanceAndCurtainComponents", "InstanceOverrideUsesCatalogUnit", "RejectsInvalidQuantities",
        "PrimaryQuantitiesIgnoreInvalidFallbacks", "InvalidUsedFallbackIsRejected", "RoomFinishQuantityPriorityMatchesFinishSchedule",
        "RoomFinishScheduleBuilder.Build(project)", "BottomAreaM2", "TopAreaM2", "SkirtingLengthM", "NetFinishAreaM2",
        "VolumeM3\"] = -99d", "SideAreaM2\"] = double.NaN", "14.4d", "33d", "22d",
    ],
    required[7]: ["MaterialUsageScheduleSmoke.Run();"],
    required[8]: [
        "MaterialUsageXlsxExporter.Export", "xl/worksheets/sheet1.xml", "KL chính", "22.5", "Kính",
        "PrimaryQuantityMutatingRows", "PrimaryQuantity snapshot mutation", "ORIGINAL"
    ],
    required[9]: ["MaterialUsageXlsxSmoke.Run();"],
}
checks[required[1]].append("XlsxPackageValidator.Validate")
checks[required[8]].extend(("Invalid\\u0001Family", "ORIGINAL"))

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing material usage guard/token: " + needle)

exporter = ROOT / required[1]
if exporter.is_file():
    text = exporter.read_text(encoding="utf-8")
    snapshot_call_pos = text.find("var row = SnapshotRow(sourceRow, rowIndex);")
    stability_call_pos = text.find("EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);")
    publication_pos = text.find("var fullPath = Path.GetFullPath(path);")
    if snapshot_call_pos < 0 or stability_call_pos < 0 or publication_pos < 0:
        errors.append("Material XLSX snapshot/stability/publication call markers must all exist")
    elif not (snapshot_call_pos < stability_call_pos < publication_pos):
        errors.append("Material XLSX must snapshot rows, verify row stability, then begin filesystem publication")

commands_source = ROOT / required[2]
if commands_source.is_file():
    text = commands_source.read_text(encoding="utf-8")
    for forbidden in ("ProjectContextCoordinator.GetOrCreate(document)", "RegenerateDirty(project)", "MaterialUsageScheduleBuilder.Build(project)"):
        if forbidden in text: errors.append(required[2] + " must not mutate or build from the live project: " + forbidden)

schedule = ROOT / required[0]
if schedule.is_file():
    text = schedule.read_text(encoding="utf-8")
    for eager in (
        'Q(element, "NetVolumeM3", Q(', 'Q(element, "CurtainNetGlassAreaM2", Q(',
        'Q(element, "NetWallAreaM2", Q(', 'Q(element, "NetFinishAreaM2", Q(',
        'Q(element, "OpeningAreaM2", Q(', 'Q(element, "AreaM2", Q(',
    ):
        if eager in text: errors.append("material usage schedule still evaluates a fallback eagerly: " + eager)

command_source = ROOT / required[2]
if command_source.is_file():
    text = command_source.read_text(encoding="utf-8")
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "MaterialUsageScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("Material Usage export must not create/bind/regenerate live project state: " + forbidden)

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DMATERIALXLSX") != 1: errors.append("QS3DMATERIALXLSX must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: material usage keeps lazy validation, checked aggregation, HT_Phòng quantity-priority parity, catalog units/provenance, detached read-only freshness, PrimaryQuantity snapshot stability, and atomic XLSX through bound UI/command entry points.")
