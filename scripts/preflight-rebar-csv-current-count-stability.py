#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/RebarCsvExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RebarCsvCurrentCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

current = "var sourceRow = enumerator.Current;"
rebound = "ValidateKnownCount(rows, admittedCount);"
null_semantics = 'if (sourceRow == null) throw new ArgumentException("BBS row cannot be null.", nameof(rows));'
snapshot = "var snapshot = SnapshotRow(sourceRow);"

if source.count(current) != 1:
    raise SystemExit("ERROR: BBS CSV must read enumerator.Current exactly once in row traversal")
current_index = source.index(current)
rebound_index = source.index(rebound, current_index + len(current))
null_index = source.index(null_semantics, current_index)
snapshot_index = source.index(snapshot, current_index)
if not current_index < rebound_index < null_index < snapshot_index:
    raise SystemExit("ERROR: BBS CSV must rebound admitted Count immediately after Current and before row semantics/snapshot")

between = source[current_index + len(current):snapshot_index]
if between.count(rebound) != 1:
    raise SystemExit("ERROR: BBS CSV requires exactly one post-Current Count rebound before snapshot")

required_smoke = [
    "[ModuleInitializer]",
    "CurrentInducedCountDriftWinsBeforeNullRowSemantics();",
    "StableCurrentIsReadExactlyOnce();",
    '"Count changed during serialization"',
    "_owner._count = 2;",
    "return null!;",
    "rows.CurrentReads != 1",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"ERROR: BBS Current Count smoke missing token: {token}")

print("PASS BBS CSV Current-induced Count stability")
