from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "Qs3dReviewWorkbook.TraceReader.cs"
text = SOURCE.read_text(encoding="utf-8")

start = text.find("private static void FindRequiredRows(")
if start < 0:
    raise SystemExit("FAIL: Qs3dReviewWorkbookTraceReader.FindRequiredRows was not found")
end = text.find("\n        private static int ParseRow", start)
if end < 0:
    raise SystemExit("FAIL: could not isolate FindRequiredRows")
method = text[start:end]

required = [
    'foreach (var row in document.Descendants(ns + "row"))',
    "var declaredRow = ParseRow(row);",
    "if (declaredRow == int.MaxValue)",
    'throw new InvalidDataException("QS3D Review workbook contains an invalid row number.")',
    "headerMatches++",
    "targetMatches++",
    "if (headerMatches != 1)",
    "if (targetMatches != 1)",
]
for snippet in required:
    if snippet not in method:
        raise SystemExit("FAIL: trace row scan lost required single-pass/all-row validation contract: " + snippet)

for forbidden in (
    ".ToList()",
    "FindUniqueRow(",
    "rows.Where(",
):
    if forbidden in method:
        raise SystemExit("FAIL: trace row scan regressed to all-row/list rescan materialization: " + forbidden)

if "private const int MaxRows = 1048576;" not in text:
    raise SystemExit("FAIL: trace reader lost canonical XLSX row bound")

print("PASS QS3D Review trace worksheet single-pass row integrity")
