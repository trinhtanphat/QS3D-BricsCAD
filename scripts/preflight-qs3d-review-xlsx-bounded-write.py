from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Xlsx.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Exporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs3dReviewWorkbookXlsxBoundedWriteSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
exporter = EXPORTER.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.exists() else ""

required_source = [
    "MaxWorksheetEntryBytes",
    "MaxArchiveBytes",
    "StrictUtf8",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
]
forbidden_source = [
    "internal static void WritePackage(string path, params string[] sheets)",
    "using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false))) writer.Write(content);",
]
required_exporter = ["Qs3dReviewXlsx.WritePackage(path"]
required_smoke = [
    "CumulativeWorksheetBudgetFailsBeforeDestinationReplacement",
    "ExistingReviewWorkbookSurvivesBudgetFailure",
    "QS3D Review XLSX worksheet exceeds",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_exporter if token not in exporter]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]
if missing or forbidden:
    detail = []
    if missing:
        detail.append("missing: " + ", ".join(missing))
    if forbidden:
        detail.append("forbidden: " + ", ".join(forbidden))
    raise SystemExit("QS3D Review XLSX bounded-write preflight failed; " + "; ".join(detail))

print("PASS QS3D Review XLSX bounded worksheet/archive write guard")
