#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "DoorOpeningXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DoorOpeningXlsxTransientCountStabilitySmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "KnownCountContract<DoorOpeningScheduleRow>",
    "BindKnownCount(rows, MaxDataRows, \"export rows\")",
    "rowCount.Revalidate(rows, \"before row indexer\")",
    "var sourceRow = rows[rowIndex];",
    "rowCount.Revalidate(rows, \"after row indexer\")",
    "rowCount.Revalidate(rows, \"after row snapshot\")",
]
for token in required_source:
    if token not in source:
        raise SystemExit("missing Door/opening transient Count stability source token: " + token)

before = source.index('rowCount.Revalidate(rows, "before row indexer")')
read = source.index("var sourceRow = rows[rowIndex];")
after = source.index('rowCount.Revalidate(rows, "after row indexer")')
snapshot = source.index("snapshot.Add(SnapshotRow(sourceRow, rowIndex));")
post_snapshot = source.index('rowCount.Revalidate(rows, "after row snapshot")')
if not (before < read < after < snapshot < post_snapshot):
    raise SystemExit("Door/opening row traversal must revalidate Count before and after caller indexer and after semantic snapshot")

for token in (
    "RejectsTransientGenericCountGrowthBeforeIndexer();",
    "RejectsTransientGenericCountDriftAfterIndexer();",
    "StableMultiInterfaceSourceReadsEachRowOnce();",
    "expectedIndexerReads: 0",
    "expectedIndexerReads: 1",
    "sentinel",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("missing Door/opening transient Count smoke token: " + token)

print("PASS door opening XLSX transient row Count stability guard")
