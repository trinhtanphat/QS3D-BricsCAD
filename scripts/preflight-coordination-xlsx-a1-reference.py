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
        "ParseCellReference(reference)",
        "Coordination XLSX import must parse the complete A1 cell reference, not only its column prefix.",
    )
    require(
        text,
        "ParseWorksheetRowReference",
        "Coordination XLSX import must validate the containing worksheet row identity.",
    )
    require(
        text,
        "cellReference.Row != rowNumber",
        "Coordination XLSX import must reject a cell whose encoded row disagrees with its containing worksheet row.",
    )
    require(
        text,
        "reference[index] >= 'A' && reference[index] <= 'Z'",
        "Coordination XLSX A1 parsing must use canonical ASCII uppercase column letters.",
    )
    require(
        text,
        "reference[index] >= '0' && reference[index] <= '9'",
        "Coordination XLSX A1 parsing must validate the complete decimal row suffix.",
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
