from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CustomerWorkbookBoundedPublicationSmoke.cs").read_text(encoding="utf-8")

required = [
    "MaxWorksheetEntryBytes",
    "MaxAggregateUncompressedBytes",
    "MaxArchiveBytes",
    "BoundedArchiveWriteStream",
    "ReserveUncompressed",
    "FixedZipTimestamp",
]
for token in required:
    if token not in source:
        raise SystemExit("customer workbook bounded publication: missing source contract: " + token)

for legacy in ["var dgklXml =", "var formworkXml =", "var detailXml =", "var traceXml ="]:
    if legacy in source:
        raise SystemExit("customer workbook bounded publication: whole-workbook worksheet materialization returned: " + legacy)
smoke_required = [
    "new string('X', 30000)",
    "600",
    "ExpectThrows<InvalidDataException>",
    "existing destination",
    "owned temp residue",
]
for token in smoke_required:
    if token not in smoke:
        raise SystemExit("customer workbook bounded publication: missing hostile regression: " + token)

print("PASS customer workbook bounded publication")
