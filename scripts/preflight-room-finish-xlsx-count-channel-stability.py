#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomFinishXlsxCountChannelStabilitySmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "private sealed class KnownCountContract<T>",
    "BindKnownCount(rows, MaxDataRows, \"export rows\")",
    "rowCount.Revalidate(rows, \"before row indexer\")",
    "var sourceRow = rows[rowIndex];",
    "rowCount.Revalidate(rows, \"after row indexer\")",
    "rowCount.Revalidate(rows, \"after row snapshot\")",
    "rowCount.Revalidate(rows, \"after snapshot traversal\")",
    "rowCount.Revalidate(rows, \"after row stability validation\")",
]
for token in required_source:
    if token not in source:
        raise SystemExit("missing Room-finish XLSX count-channel stability source token: " + token)

before = source.index('rowCount.Revalidate(rows, "before row indexer")')
read = source.index("var sourceRow = rows[rowIndex];")
after = source.index('rowCount.Revalidate(rows, "after row indexer")')
snapshot = source.index("var row = SnapshotRow(sourceRow, rowIndex);")
post_snapshot = source.index('rowCount.Revalidate(rows, "after row snapshot")')
post_traversal = source.index('rowCount.Revalidate(rows, "after snapshot traversal")')
post_stability = source.index('rowCount.Revalidate(rows, "after row stability validation")')
if not (before < read < after < snapshot < post_snapshot < post_traversal < post_stability):
    raise SystemExit("Room-finish row traversal must revalidate all admitted Count channels around caller indexer/snapshot boundaries")

required_smoke = [
    "RejectsIndexerInducedGenericCountDriftBeforeFilesystem();",
    "AcceptsStableMultiInterfaceCounts();",
    "ICollection<RoomFinishScheduleRow>.Count => _drifted ? 2 : 1",
    "if (_driftAfterIndexer) _drifted = true;",
    "rows.IndexerReads != 1",
    "Directory.Exists(root)",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("missing Room-finish XLSX count-channel stability smoke token: " + token)

print("PASS room finish XLSX count-channel stability guard")
