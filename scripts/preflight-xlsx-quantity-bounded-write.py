from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/XlsxQuantityBoundedWriteSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.exists() else ""

required_source = [
    "MaxWorksheetEntryBytes",
    "MaxAggregateUncompressedBytes",
    "MaxArchiveBytes",
    "StrictUtf8",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
]
forbidden_source = [
    "private static string BuildSheet(IReadOnlyList<QuantityReportRow> rows)",
    "private static string BuildEd2Sheet(IReadOnlyList<QuantityReportRow> rows)",
    "WriteEntry(archive, \"xl/worksheets/sheet1.xml\", isEd2 ? BuildEd2Sheet(rows) : BuildSheet(rows))",
    "WriteEntry(archive, \"xl/worksheets/sheet2.xml\", BuildEd2Sheet(summaryRows))",
]
required_smoke = [
    "CumulativeWorksheetBudgetFailsBeforeDestinationReplacement",
    "ExistingQuantityWorkbookSurvivesBudgetFailure",
    "Quantity XLSX worksheet exceeds",
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
    raise SystemExit("Quantity XLSX bounded-write preflight failed; " + "; ".join(detail))

print("PASS Quantity XLSX bounded worksheet/archive write guard")
