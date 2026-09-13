from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Sheets.cs"
text = SOURCE.read_text(encoding="utf-8")
match = re.search(
    r"private static double\? Sum\(.*?\n        \}",
    text,
    flags=re.S,
)
if match is None:
    print("FAIL: QS3D Review summary Sum helper is missing")
    sys.exit(1)
body = match.group(0)
required = (
    "QuantityReportMath.FiniteAccumulator",
    "accumulator.Add(value(row)",
    "accumulator.Value(",
)
missing = [token for token in required if token not in body]
if missing:
    print("FAIL: summary numeric stability guard missing: " + ", ".join(missing))
    sys.exit(1)
forbidden = ("total += value(row)", "rows.Sum(")
found = [token for token in forbidden if token in body]
if found:
    print("FAIL: naive summary aggregation reintroduced: " + ", ".join(found))
    sys.exit(1)
print("PASS: QS3D Review summary uses compensated finite aggregation")
