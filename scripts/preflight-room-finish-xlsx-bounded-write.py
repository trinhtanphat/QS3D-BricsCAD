from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishXlsxBoundedWriteSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.exists() else ""

required_source = [
    "MaxWorksheetEntryBytes",
    "MaxAggregateUncompressedBytes",
    "MaxArchiveBytes",
    "StrictUtf8",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
    '"Tầng", "Phòng", "Loại hoàn thiện", "Family / Loại", "Vật liệu", "Đơn vị", "SL", "KL chính", "Dài (m)", "Diện tích (m²)"',
    '"Element IDs", "Room IDs", "Project ID", "Drawing fingerprint", "Source Handles"',
    'name=\\"HT Phòng\\"',
]
forbidden_source = [
    "private static string BuildSheet(IReadOnlyList<RoomFinishScheduleRow> rows)",
    "Write(archive, \"xl/worksheets/sheet1.xml\", BuildSheet(snapshot))",
]
required_smoke = [
    "VietnameseHeaderRoundTripIsPreserved",
    "xl/worksheets/sheet1.xml",
    "xl/workbook.xml",
    'name=\\\"HT Phòng\\\"',
    "new UTF8Encoding(false, true)",
    "CumulativeWorksheetBudgetFailsBeforeDestinationReplacement",
    "ExistingRoomFinishWorkbookSurvivesBudgetFailure",
    "Room-finish XLSX worksheet exceeds",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]
if missing or forbidden:
    detail = []
    if missing:
        detail.append("missing: " + ", ".join(missing))
    if forbidden:
        detail.append("forbidden: " + ", ".join(forbidden))
    raise SystemExit("Room Finish XLSX bounded-write preflight failed; " + "; ".join(detail))

print("PASS Room Finish XLSX bounded worksheet/archive write and UTF-8 workbook/header-fidelity guard")
