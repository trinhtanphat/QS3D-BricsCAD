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

indexer_contract = '''rowCount.Revalidate(rows, "before row indexer");
                var sourceRow = rows[rowIndex];
                rowCount.Revalidate(rows, "after row indexer");'''
if indexer_contract not in source:
    raise SystemExit("Room-finish XLSX must revalidate admitted Count channels immediately around the caller row indexer")

snapshot_contract = '''var row = SnapshotRow(sourceRow, rowIndex);
                rowCount.Revalidate(rows, "after row snapshot");'''
if snapshot_contract not in source:
    raise SystemExit("Room-finish XLSX must revalidate admitted Count channels immediately after semantic row snapshot")

traversal_contract = '''rowCount.Revalidate(rows, "after snapshot traversal");
            for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)
                EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);
            rowCount.Revalidate(rows, "after row stability validation");'''
if traversal_contract not in source:
    raise SystemExit("Room-finish XLSX must revalidate admitted Count channels after traversal and source-row stability validation")

filesystem_boundary = '''rowCount.Revalidate(rows, "after row stability validation");
            var fullPath = Path.GetFullPath(path);'''
if filesystem_boundary not in source:
    raise SystemExit("Room-finish XLSX final Count revalidation must precede filesystem publication setup")

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
