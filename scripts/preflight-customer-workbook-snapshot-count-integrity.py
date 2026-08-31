#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/QsCustomerWorkbookSnapshotCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
test = TEST.read_text(encoding="utf-8")

required_source = (
    "using System.Collections;",
    "KnownCountContract",
    "BindSourceCount(detailRows, DetailSheet)",
    "BindSourceCount(summaryRows, DgklSheet)",
    "Revalidate(source, \"before row indexer\")",
    "var row = source[index]",
    "Revalidate(source, \"after row indexer\")",
    "Revalidate(source, \"after snapshot traversal\")",
    "Revalidate(detailRows, \"before filesystem publication\")",
    "Revalidate(summaryRows, \"before filesystem publication\")",
)
for token in required_source:
    if token not in source:
        raise SystemExit(f"customer workbook snapshot Count integrity guard missing source contract: {token}")

for forbidden in (
    "for (var index = 0; index < source.Count; index++)",
    "new List<QuantityReportRow>(source.Count)",
    "if (source.Count != admittedCount)",
):
    if forbidden in source:
        raise SystemExit(f"customer workbook snapshot Count integrity guard found legacy single-channel Count logic: {forbidden}")

for token in (
    "RejectsShrinkAfterAdmissionBeforeDestinationReplacement",
    "RejectsGrowthAfterAdmissionBeforeDestinationReplacement",
    "RejectsConflictingAdmittedCountChannelsBeforeIndexer",
    "RejectsTransientGenericCountDriftAroundIndexer",
    "AcceptsStableMultiInterfaceCountChannels",
    "ICollection<QuantityReportRow>",
    "ICollection.Count => 1",
    "details.IndexerReads != 0",
    "details.IndexerReads != 1",
    "existing-destination",
):
    if token not in test:
        raise SystemExit(f"customer workbook snapshot Count integrity guard missing regression evidence: {token}")

print("PASS customer workbook snapshot Count integrity")
