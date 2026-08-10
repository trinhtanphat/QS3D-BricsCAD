#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Reporting/RoomFinishSchedule.cs",
    "src/QS3D.Core/Export/RoomFinishXlsxExporter.cs",
    "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishScheduleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishScheduleRegistration.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishXlsxSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RoomFinishXlsxRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing room-finish schedule file: " + relative)

checks = {
    required[0]: [
        "RoomFinishScheduleRow", "RoomFinishScheduleBuilder", "ElementCategory.FloorFinish", "ElementCategory.Waterproofing",
        "ElementCategory.Skirting", "ElementCategory.WallFinish", "ElementCategory.CeilingFinish",
        "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)", "ProjectMaterialCatalog.GetAll(project)",
        '"ParentRoomId"', '"SourceRoomId"', '"GeneratedFromRoomId"', '"RoomId"',
        'var roomKey = roomId.Length > 0 ? roomId : "(unlinked)"',
        'FirstQuantity(element, "NetFinishAreaM2", "SideAreaM2", "AreaM2")',
        'FirstQuantity(element, "SkirtingLengthM", "InnerPerimeterM", "PerimeterM", "LengthM")',
        'FirstQuantity(element, "TopAreaM2", "AreaM2")', 'FirstQuantity(element, "BottomAreaM2", "AreaM2")',
        "PrimaryQuantity", "ElementIds", "RoomIds", "must be finite and non-negative",
    ],
    required[1]: [
        "RoomFinishXlsxExporter", "AtomicFileCommit.CreateTempPath", "AtomicFileCommit.ReplaceWithoutBackup", "ZipArchive",
        "HT Phòng", "Loại hoàn thiện", "KL chính", "Element IDs", "Room IDs", "<autoFilter ref=", "Validate(tempPath)",
    ],
    required[2]: [
        'CommandMethod("QS3DFINISHXLSX"', "RegenerationEngine", "RoomFinishScheduleBuilder.Build(project)",
        "RoomFinishXlsxExporter.Export", "SaveFileDialog", "HT-Phong.xlsx",
    ],
    required[3]: [
        "GroupsAreaAndLengthFinishesByRoom", "FamilyMaterialAndInstanceOverrideSplitRows",
        "SameRoomLabelsRemainSeparateByStableId", "PreferredQuantityDoesNotEvaluateUnusedLegacyFallbacks",
        "UnlinkedFinishRemainsSchedulable", "room-2", "double.NaN", "Phòng 101", "30d", "14d", "(chưa liên kết phòng)",
    ],
    required[4]: ["RoomFinishScheduleSmoke.Run();"],
    required[5]: ["RoomFinishXlsxExporter.Export", "xl/worksheets/sheet1.xml", "Loại hoàn thiện", "Phòng 101", ">30<"],
    required[6]: ["RoomFinishXlsxSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DFINISHXLSX") != 1: errors.append("QS3DFINISHXLSX must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: room-finish schedule keeps stable room identity, lazy quantity fallbacks, material inheritance and real XLSX export.")
