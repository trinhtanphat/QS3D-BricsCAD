#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CommercialQsWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialQsWorkbookSmoke.cs"

errors = []
if not SOURCE.is_file():
    errors.append("missing CommercialQsWorkbook.cs")
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

if not SMOKE.is_file():
    errors.append("missing CommercialQsWorkbookSmoke.cs")
    smoke = ""
else:
    smoke = SMOKE.read_text(encoding="utf-8")

# Writer-side budgets are required in addition to the post-write XML parser.
for token in (
    "MaxArchiveBytes",
    "MaxEntryBytes",
    "MaxTotalUncompressedBytes",
    "MaxWorksheetRows",
    "BoundedArchiveWriteStream",
    "BoundedEntryWriteStream",
    "ReserveUncompressed",
):
    if token not in source:
        errors.append(f"commercial XLSX bounded-write contract missing {token!r}")

validator = source.find("XlsxPackageValidator.Validate(")
commit = source.find("AtomicFileCommit.ReplaceWithoutBackup(")
if validator < 0 or commit < 0 or validator > commit:
    errors.append("commercial XLSX must structurally validate the completed temp package before atomic destination commit")

if "new ZipArchive(boundedStream, ZipArchiveMode.Create" not in source:
    errors.append("commercial XLSX ZipArchive is not bound to a bounded archive stream")
if "if (rows.Count > MaxWorksheetRows)" not in source:
    errors.append("commercial XLSX worksheet cardinality is not rejected before row emission")
if "buffer.CopyTo(target)" not in source:
    errors.append("commercial XLSX worksheets must be bounded before copying into the ZIP entry")
if "archiveLength <= 0L || archiveLength > MaxArchiveBytes" not in source:
    errors.append("commercial XLSX completed temporary archive size is not rechecked before validation/commit")

for token in (
    "OversizedWorksheetFailsWithoutPublishingDestination",
    "SequenceEqual(sentinel)",
    "Directory.GetFiles(root).Length == 1",
):
    if token not in smoke:
        errors.append(f"commercial XLSX hostile-output regression missing {token!r}")

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS commercial QS workbook bounded-write source guard")
