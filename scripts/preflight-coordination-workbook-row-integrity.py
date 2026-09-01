#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationWorkbook.cs"


def fail(message: str) -> None:
    raise SystemExit("ERROR: " + message)


def body(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        fail(f"missing {signature}")
    brace = source.find("{", start)
    if brace < 0:
        fail(f"missing body for {signature}")
    depth = 0
    for index in range(brace, len(source)):
        ch = source[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]
    fail(f"unterminated body for {signature}")
    return ""


source = SOURCE.read_text(encoding="utf-8")
clash = body(source, "private static ClashProjection ReadClashRow(")
trace = body(source, "private static ClashProjection ReadTraceProjection(")

for name, block in (("ReadClashRow", clash), ("ReadTraceProjection", trace)):
    if '.Descendants(ns + "row").ToList()' in block or 'Descendants(ns + "row").ToList()' in block:
        fail(f"{name} must not eagerly materialize all worksheet rows")
    if "ParseRow(" not in block:
        fail(f"{name} must validate worksheet row metadata while scanning")

if "SelectCoordinationRowsBounded(" not in source:
    fail("missing shared bounded selective worksheet row scanner")

scanner = body(source, "private static SelectedCoordinationRows SelectCoordinationRowsBounded(")
for token in (
    'document.Descendants(ns + "row")',
    "ParseRow(row)",
    "rowNumber",
    "header",
    "target",
    "duplicate",
):
    if token.lower() not in scanner.lower():
        fail("bounded selective scanner missing contract token: " + token)

if ".ToList()" in scanner:
    fail("bounded selective scanner must not materialize worksheet rows")

if "private sealed class SelectedCoordinationRows" not in source:
    fail("missing bounded selected-row carrier")

print("PASS coordination workbook bounded row integrity")
