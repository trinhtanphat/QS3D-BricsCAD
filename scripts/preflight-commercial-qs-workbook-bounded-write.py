#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CommercialQsWorkbook.cs"

errors = []
if not SOURCE.is_file():
    errors.append("missing CommercialQsWorkbook.cs")
else:
    source = SOURCE.read_text(encoding="utf-8")

    # The commercial workbook must carry explicit output budgets at the writer,
    # not rely only on a validator that runs after the temporary XLSX exists.
    required = (
        "MaxArchiveBytes",
        "MaxEntryBytes",
        "MaxTotalUncompressedBytes",
        "MaxWorksheetRows",
        "BoundedArchiveWriteStream",
    )
    for token in required:
        if token not in source:
            errors.append(f"commercial XLSX bounded-write contract missing {token!r}")

    validator = source.find("XlsxPackageValidator.Validate(")
    commit = source.find("AtomicFileCommit.ReplaceWithoutBackup(")
    if validator < 0 or commit < 0 or validator > commit:
        errors.append("commercial XLSX must structurally validate the completed temp package before atomic destination commit")

    # The archive budget must wrap the stream consumed by ZipArchive. A plain
    # ZipArchive over the raw temp FileStream can exhaust temporary storage
    # before post-write validation observes the oversized package.
    if "new ZipArchive(boundedStream, ZipArchiveMode.Create" not in source:
        errors.append("commercial XLSX ZipArchive is not bound to a bounded archive stream")

    if "if (rows.Count > MaxWorksheetRows)" not in source:
        errors.append("commercial XLSX worksheet cardinality is not rejected before row emission")

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS commercial QS workbook bounded-write source guard")
