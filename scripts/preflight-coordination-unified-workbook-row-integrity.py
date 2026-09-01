#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationUnifiedWorkbook.cs"


def fail(message: str) -> None:
    raise SystemExit("ERROR: " + message)


def body(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        fail("missing " + signature)
    brace = source.find("{", start)
    if brace < 0:
        fail("missing body for " + signature)
    depth = 0
    for index in range(brace, len(source)):
        ch = source[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]
    fail("unterminated body for " + signature)
    return ""


source = SOURCE.read_text(encoding="utf-8")
source_row = body(source, "private static SourceProjection ReadSourceRow(")
trace_row = body(source, "private static TraceProjection ReadTraceRow(")

for name, block in (("ReadSourceRow", source_row), ("ReadTraceRow", trace_row)):
    if 'Descendants(ns + "row").ToList()' in block:
        fail(name + " must not eagerly materialize all worksheet rows")
    if "ParseRow(" not in block:
        fail(name + " must validate row coordinates during traversal")

if "SelectUnifiedRowsBounded(" not in source_row:
    fail("source lookup must use the bounded selective row scanner")

scanner = body(source, "private static SelectedUnifiedRows SelectUnifiedRowsBounded(")
for token in (
    'document.Descendants(ns + "row")',
    "ParseRow(row)",
    "rowNumber",
    "header",
    "target",
    "duplicated",
):
    if token.lower() not in scanner.lower():
        fail("bounded scanner missing contract token: " + token)
if ".ToList()" in scanner:
    fail("bounded scanner must not materialize worksheet rows")

for token in (
    'foreach (var row in document.Descendants(ns + "row"))',
    "var declaredRow = ParseRow(row);",
    "matchedCells",
    "matchedFormulas",
    "TRACE_MODEL lookup is missing or ambiguous",
):
    if token not in trace_row:
        fail("TRACE_MODEL scan missing bounded unique-match contract token: " + token)

if "private sealed class SelectedUnifiedRows" not in source:
    fail("missing bounded selected-row carrier")

print("PASS coordination unified workbook bounded row integrity")
