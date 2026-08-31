from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_PATH = ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookTraceReader.cs"
SMOKE_PATH = ROOT / "tests/QS3D.Core.SmokeTests/CustomerWorkbookWorksheetRowBoundSmoke.cs"

source = SOURCE_PATH.read_text(encoding="utf-8")
smoke = SMOKE_PATH.read_text(encoding="utf-8")

bounded_call = 'MaterializeWorksheetRowsBounded(document.Descendants(ns + "row"), MaxRows);'
if source.count(bounded_call) != 2:
    raise SystemExit("customer workbook row bound: business and TRACE_MODEL readers must share the bounded row materializer")

if 'document.Descendants(ns + "row").ToList();' in source:
    raise SystemExit("customer workbook row bound: raw worksheet-row ToList materialization must not return")

helper = source.find("private static IReadOnlyList<XElement> MaterializeWorksheetRowsBounded")
if helper < 0:
    raise SystemExit("customer workbook row bound: bounded worksheet-row helper is missing")
helper_end = source.find("private static IReadOnlyList<string>? ReadSharedStrings", helper)
if helper_end < 0:
    raise SystemExit("customer workbook row bound: unable to isolate bounded worksheet-row helper")
body = source[helper:helper_end]

ordered = [
    "while (enumerator.MoveNext())",
    "if (result.Count == maximum)",
    "var row = enumerator.Current;",
    "result.Add(row);",
]
positions = [body.find(token) for token in ordered]
if any(position < 0 for position in positions) or positions != sorted(positions):
    raise SystemExit("customer workbook row bound: require MoveNext -> ceiling check -> Current -> retention ordering")

if "maximum < 0 || maximum > MaxRows" not in body:
    raise SystemExit("customer workbook row bound: helper test ceiling must remain constrained by the XLSX MaxRows contract")

if "Where(row => ParseRow(row) == rowNumber).Take(2).ToList()" not in source:
    raise SystemExit("customer workbook row bound: duplicate row lookup must retain at most two matches")

required_smoke = [
    "ExactLimitIsRetainedWithoutOverread",
    "FirstOverLimitFailsBeforeUnexpectedCurrent",
    '"MaterializeWorksheetRowsBounded"',
    'Equal(2, source.CurrentReads, "surplus row must be rejected before unexpected Current")',
    'Equal(3, source.MoveNextCalls, "surplus row must be discovered by MoveNext before rejection")',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("customer workbook row bound: deterministic smoke is missing token: " + token)

print("PASS customer workbook worksheet row materialization bound")
