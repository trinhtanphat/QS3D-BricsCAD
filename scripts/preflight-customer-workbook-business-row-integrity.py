from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_PATH = ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookTraceReader.cs"
SMOKE_PATH = ROOT / "tests/QS3D.Core.SmokeTests/CustomerWorkbookBusinessRowIntegritySmoke.cs"

source = SOURCE_PATH.read_text(encoding="utf-8")
smoke = SMOKE_PATH.read_text(encoding="utf-8")

required_source = [
    'SelectBusinessRowsBounded(document.Descendants(ns + "row"), rowNumber, MaxRows)',
    "private static Tuple<XElement, XElement> SelectBusinessRowsBounded",
    "while (enumerator.MoveNext())",
    "if (retainedCount == maximum)",
    "var row = enumerator.Current;",
    "var parsedRow = ParseRow(row);",
    "if (parsedRow == int.MaxValue)",
    "if (parsedRow == 1)",
    "if (parsedRow == targetRowNumber)",
]
for token in required_source:
    if token not in source:
        raise SystemExit("customer workbook business row integrity: missing source token: " + token)

if source.count('SelectBusinessRowsBounded(document.Descendants(ns + "row"), rowNumber, MaxRows)') != 1:
    raise SystemExit("customer workbook business row integrity: business worksheet must use exactly one bounded selector")

helper = source.find("private static Tuple<XElement, XElement> SelectBusinessRowsBounded")
helper_end = source.find("private static IReadOnlyList<XElement> MaterializeWorksheetRowsBounded", helper)
if helper < 0 or helper_end < 0:
    raise SystemExit("customer workbook business row integrity: unable to isolate bounded selector")
body = source[helper:helper_end]
ordered = [
    "while (enumerator.MoveNext())",
    "if (retainedCount == maximum)",
    "var row = enumerator.Current;",
    "retainedCount++;",
    "var parsedRow = ParseRow(row);",
    "if (parsedRow == int.MaxValue)",
]
positions = [body.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions):
    raise SystemExit("customer workbook business row integrity: require MoveNext -> ceiling -> Current -> count -> row validation ordering")

if "new List<XElement>()" in body or "result.Add(row)" in body:
    raise SystemExit("customer workbook business row integrity: selector must retain only header and target rows")

required_smoke = [
    "StableBusinessRowsRetainOnlyHeaderAndTarget",
    "DuplicateHeaderAndTargetFailClosed",
    "MalformedUnrelatedRowFailsClosed",
    "OutOfRangeUnrelatedRowFailsClosed",
    "SurplusRowFailsBeforeUnexpectedCurrent",
    '"SelectBusinessRowsBounded"',
    'Equal(2, source.CurrentReads, "surplus business row must fail before unexpected Current")',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("customer workbook business row integrity: deterministic smoke is missing token: " + token)

print("PASS customer workbook business row integrity")
