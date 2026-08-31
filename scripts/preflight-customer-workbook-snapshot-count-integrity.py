#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/QsCustomerWorkbookSnapshotCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
test = TEST.read_text(encoding="utf-8")

required_source = (
    "var detailCount = BindSourceCount(detailRows, DetailSheet);",
    "var summaryCount = BindSourceCount(summaryRows, DgklSheet);",
    "var details = Snapshot(detailRows, detailCount, true, DetailSheet);",
    "var summaries = Snapshot(summaryRows, summaryCount, false, DgklSheet);",
    "for (var index = 0; index < admittedCount; index++)",
    "if (source.Count != admittedCount)",
    "row Count changed during snapshot traversal.",
)
for token in required_source:
    if token not in source:
        raise SystemExit(f"customer workbook snapshot Count integrity guard missing source contract: {token}")

for forbidden in (
    "for (var index = 0; index < source.Count; index++)",
    "new List<QuantityReportRow>(source.Count)",
):
    if forbidden in source:
        raise SystemExit(f"customer workbook snapshot Count integrity guard found live Count traversal: {forbidden}")

for token in (
    "RejectsShrinkAfterAdmissionBeforeDestinationReplacement",
    "RejectsGrowthAfterAdmissionBeforeDestinationReplacement",
    "existing-destination",
):
    if token not in test:
        raise SystemExit(f"customer workbook snapshot Count integrity guard missing regression evidence: {token}")

print("PASS customer workbook snapshot Count integrity")
