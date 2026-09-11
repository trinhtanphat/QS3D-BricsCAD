from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxQuantityEvidenceExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityEvidenceXlsxHardeningSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "MaxWorksheetEntryBytes",
    "MaxArchiveBytes",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
    "WriteWorksheet(",
    "StrictUtf8",
]
forbidden_source = [
    'WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(rows));',
    "private static string WorksheetXml(",
    "new StringBuilder(4096 + rows.Count * 1024)",
]
required_smoke = [
    "CumulativeWorksheetBudgetFailsBeforeDestinationReplacement",
    "ExistingQuantityEvidenceWorkbookSurvivesBudgetFailure",
    "Quantity evidence XLSX worksheet exceeds",
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
    raise SystemExit(
        "Quantity evidence XLSX bounded-write preflight failed; " + "; ".join(detail)
    )

print("PASS quantity evidence XLSX bounded worksheet/archive write guard")
