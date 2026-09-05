#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"


def fail(message: str) -> None:
    raise SystemExit("FAIL: " + message)


def require(source: str, needle: str, message: str) -> int:
    pos = source.find(needle)
    if pos < 0:
        fail(message)
    return pos


def main() -> int:
    path = ROOT / REL
    if not path.exists():
        fail(f"missing required source: {REL}")

    source = path.read_text(encoding="utf-8")
    inspector_start = require(source, "internal static class BltLegacyCadInspector", "missing BLT legacy CAD inspector")
    inspector_end = require(source, "internal static class BltLegacyProbeReport", "missing BLT legacy probe report boundary")
    inspector = source[inspector_start:inspector_end]

    require(inspector, "MaxRetainedSnapshotBudgetBytes", "aggregate retained-snapshot byte budget is missing")
    require(inspector, "EstimatedSnapshotOverheadBytes", "snapshot structural budget cost is missing")
    require(inspector, "EstimatedMetadataEntryOverheadBytes", "metadata-entry structural budget cost is missing")
    require(inspector, "EstimateRetainedSnapshotBytes", "deterministic retained-snapshot estimator is missing")

    current_start = require(inspector, "public static IReadOnlyList<EntitySnapshot> ReadCurrentSpace", "missing Current Space scanner")
    selection_start = require(inspector, "public static IReadOnlyList<EntitySnapshot> ReadSelection", "missing selection scanner")
    try_add_start = require(inspector, "private static void TryAdd", "missing shared snapshot admission path")
    metrics_start = require(inspector, "private static void PopulateDirectMetrics", "missing TryAdd boundary")

    current = inspector[current_start:selection_start]
    selection = inspector[selection_start:try_add_start]
    try_add = inspector[try_add_start:metrics_start]

    for label, block in (("Current Space", current), ("selection", selection)):
        require(block, "long retainedSnapshotBytes = 0;", f"{label} must start one aggregate retained-snapshot budget counter")
        require(block, "ref retainedSnapshotBytes", f"{label} must route the shared aggregate budget through TryAdd")

    estimate_pos = require(try_add, "var snapshotBytes = EstimateRetainedSnapshotBytes(snapshot);", "TryAdd must estimate a completed snapshot before retention")
    reject_pos = require(try_add, "retainedSnapshotBytes > MaxRetainedSnapshotBudgetBytes - snapshotBytes", "TryAdd must fail closed before aggregate budget overflow")
    add_pos = require(try_add, "result.Add(snapshot);", "TryAdd must retain an admitted snapshot")
    update_pos = require(try_add, "retainedSnapshotBytes += snapshotBytes;", "TryAdd must charge the admitted snapshot to the aggregate budget")
    if not (estimate_pos < reject_pos < update_pos < add_pos):
        fail("budget estimate/rejection/charge must happen before result retention")

    catch_pos = try_add.find("catch")
    if catch_pos < 0 or catch_pos > estimate_pos:
        fail("per-object inspection failures must remain isolated before aggregate budget admission")

    estimator_start = require(inspector, "private static long EstimateRetainedSnapshotBytes", "missing retained-snapshot estimator")
    estimator_end = require(inspector, "private static void PopulateDirectMetrics", "missing estimator boundary")
    estimator = inspector[estimator_start:estimator_end]
    for needle, message in (
        ("snapshot.Handle", "estimator must account source Handle text"),
        ("snapshot.EntityType", "estimator must account entity-type text"),
        ("snapshot.Layer", "estimator must account layer text"),
        ("snapshot.Metadata", "estimator must account retained metadata"),
        ("EstimatedMetadataEntryOverheadBytes", "estimator must reserve structural cost per metadata entry"),
        ("Encoding.UTF8.GetByteCount", "estimator must measure retained text deterministically as UTF-8 bytes"),
    ):
        require(estimator, needle, message)

    print("PASS: V25 BLT legacy scan applies one fail-closed aggregate retained-snapshot budget to Current Space and selection paths.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
