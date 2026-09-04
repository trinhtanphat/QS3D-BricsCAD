#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REL = "src/QS3D.BricsCAD.V25/BltLegacyCommands.cs"


def fail(message):
    raise SystemExit("FAIL: " + message)


def require(source, needle):
    if needle not in source:
        fail(f"{REL} missing BLT selection-bound contract: {needle}")


def main():
    path = ROOT / REL
    if not path.exists():
        fail(f"missing required source: {REL}")
    source = path.read_text(encoding="utf-8")

    for needle in (
        "private const int MaxScannedEntities = 250000;",
        "if (scanned++ >= MaxScannedEntities)",
        "if (selection.Value.Count > MaxScannedEntities)",
        '"BLT legacy selection exceeds guarded limit of " + MaxScannedEntities + " entities."',
        "foreach (var id in selection.Value.GetObjectIds()) TryAdd(transaction, id, result);",
        "StartOpenCloseTransaction()",
    ):
        require(source, needle)

    if "MaxCurrentSpaceEntities" in source:
        fail(f"{REL} still has path-specific scan limit instead of one shared cardinality contract")

    selection_guard = source.index("if (selection.Value.Count > MaxScannedEntities)")
    selection_materialize = source.index("selection.Value.GetObjectIds()")
    if selection_guard >= selection_materialize:
        fail("selection cardinality must be admitted before GetObjectIds/per-entity inspection")

    print("PASS: V25 BLT legacy current-space and selection inspection share one 250000-entity fail-closed cardinality bound, and over-limit selection is rejected before ObjectId materialization/per-entity work.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
