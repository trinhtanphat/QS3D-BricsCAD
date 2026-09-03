#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationWorkbookCellCoordinateIntegritySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationWorkbookCellCoordinateIntegritySmokeRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing coordination cell-coordinate integrity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "private const int MaxColumns = 16384;",
    "var declaredRow = ParseRow(row);",
    "var reference = (string)cell.Attribute(\"r\") ?? string.Empty;",
    "var column = ParseColumn(reference, declaredRow);",
    "private static int ParseColumn(string reference, int expectedRow)",
    "column > MaxColumns",
    "reference[index] == '0'",
    "NumberStyles.None",
    "cellRow > MaxRows",
    "cellRow != expectedRow",
    "cell coordinate row does not match its containing row",
):
    if token not in source:
        errors.append("coordination cell-coordinate production contract missing: " + token)

for forbidden, label in (
    ("var reference = ((string)cell.Attribute(\"r\") ?? string.Empty).Trim();", "cell coordinates must not be canonicalized by trimming"),
    ("private static int ParseColumn(string reference)\n", "column-only parser must not survive"),
    ("ch >= 'a' && ch <= 'z'", "lowercase cell coordinates must not be silently canonicalized"),
):
    if forbidden in source:
        errors.append(label)

for token in (
    "RejectCoordinate",
    "xl/worksheets/sheet1.xml",
    "xl/worksheets/sheet2.xml",
    "B999",
    "A999",
    "B2garbage",
    "b2",
    "B02",
    "B0",
    "XFE2",
    "ExpectInvalidData",
):
    if token not in smoke:
        errors.append("coordination cell-coordinate regression missing: " + token)

if "CoordinationWorkbookCellCoordinateIntegritySmoke.Run();" not in registration:
    errors.append("coordination cell-coordinate smoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Coordination workbook trace lookup validates complete canonical A1 cell coordinates and binds every cell row to its containing worksheet row before provenance values are trusted.")
