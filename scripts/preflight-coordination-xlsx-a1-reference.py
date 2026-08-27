#!/usr/bin/env python3
"""Fail closed if Coordination issue XLSX import stops validating canonical A1 identity."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "CoordinationIssueExcelWorkbook.cs"


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(message)


def forbid(text: str, token: str, message: str) -> None:
    if token in text:
        raise SystemExit(message)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")

    require(
        text,
        "TryParseA1CellReference(cellReference, out columnIndex, out parsedRowIndex)",
        "Coordination XLSX import must parse the complete A1 cell reference, not only its column prefix.",
    )
    require(
        text,
        "int.TryParse(rowReference, NumberStyles.None, CultureInfo.InvariantCulture, out expectedRowIndex)",
        "Coordination XLSX import must validate the containing worksheet row identity using invariant decimal parsing.",
    )
    require(
        text,
        "parsedRowIndex != expectedRowIndex",
        "Coordination XLSX import must reject a cell whose encoded row disagrees with its containing worksheet row.",
    )
    require(
        text,
        "if (character < 'A' || character > 'Z') break;",
        "Coordination XLSX A1 parsing must constrain column letters to ASCII A-Z.",
    )
    require(
        text,
        "if (reference[i] < '0' || reference[i] > '9') return false;",
        "Coordination XLSX A1 parsing must validate the complete decimal row suffix.",
    )
    require(
        text,
        "columnNumber > MaxColumns",
        "Coordination XLSX A1 parsing must reject columns beyond the XLSX XFD boundary.",
    )
    require(
        text,
        "parsedRowIndex > MaxRows",
        "Coordination XLSX A1 parsing must reject rows beyond the XLSX 1,048,576 boundary.",
    )
    forbid(
        text,
        "ColumnIndex(reference)",
        "Legacy column-prefix-only XLSX parsing is unsafe because malformed row suffixes are silently ignored.",
    )
    forbid(
        text,
        "char.IsLetter(reference",
        "Culture-sensitive letter classification is not a canonical XLSX A1 identity parser.",
    )

    print("PASS coordination XLSX canonical A1 cell-reference source guard")


if __name__ == "__main__":
    main()
