from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/XlsxRebarScheduleBoundedWriteSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.exists() else ""

required_source = [
    "MaxWorksheetEntryBytes",
    "MaxAggregateUncompressedBytes",
    "MaxArchiveBytes",
    "StrictUtf8",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
    '"Element", "Bar Mark", "Shape", "Notation"',
    '"Fabrication Status", "Standard Code", "Detailing Revision"',
]
forbidden_source = [
    "private static string BuildSheet(IReadOnlyList<RebarScheduleRow> rows, int rowCount)",
    "WriteEntry(archive, \"xl/worksheets/sheet1.xml\", BuildSheet(snapshot, rowCount))",
]
required_smoke = [
    "CumulativeWorksheetBudgetFailsBeforeDestinationReplacement",
    "ExistingRebarWorkbookSurvivesBudgetFailure",
    "new UTF8Encoding(false, true)",
    "xl/worksheets/sheet1.xml",
    "Rebar XLSX worksheet exceeds",
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
    raise SystemExit("Rebar Schedule XLSX bounded-write preflight failed; " + "; ".join(detail))

print("PASS Rebar Schedule XLSX bounded worksheet/archive write guard")
