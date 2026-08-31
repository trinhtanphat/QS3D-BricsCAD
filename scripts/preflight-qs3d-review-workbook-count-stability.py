#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "Qs3dReviewWorkbook.Exporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "Qs3dReviewWorkbookCountNoOverreadSmoke.cs"


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        raise SystemExit(f"ERROR: QS3D Review Count guard missing {label}: {marker}")
    return index


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    helper = require(source, "void RequireStableCount()", "stable Count helper")
    pre_move = require(source, "RequireStableCount();\n                    var moved = enumerator.MoveNext();", "pre-MoveNext rebound")
    post_move = require(source, "var moved = enumerator.MoveNext();\n                    RequireStableCount();", "post-MoveNext rebound")
    overrun = require(source, "if (result.Count >= expectedCount)", "known-count overrun rejection")
    current = require(source, "var value = enumerator.Current;", "detached Current read")
    post_current = require(source, "var value = enumerator.Current;\n                    RequireStableCount();", "post-Current rebound")
    retain = require(source, "result.Add(value);", "retention after rebound")
    exact = require(source, "if (result.Count != expectedCount)", "final exact-cardinality rejection")

    if not (helper < pre_move < post_move < overrun < current <= post_current < retain < exact):
        raise SystemExit("ERROR: QS3D Review Count ordering must be helper -> pre/post MoveNext -> overrun -> Current -> rebound -> retention -> exact cardinality")

    if source.count("RequireStableCount();") != 4:
        raise SystemExit("ERROR: QS3D Review SnapshotCounted must contain exactly four traversal/final Count rebound calls")

    require(smoke, "MoveNextInducedCountDriftFailsBeforeCurrent();", "MoveNext drift regression")
    require(smoke, "CurrentInducedCountDriftFailsBeforeRetention();", "Current drift regression")
    require(smoke, "source.CountReads == 10", "stable two-item Count observation budget")
    require(smoke, "source.CurrentReads == 1", "single Current read assertion")

    print("PASS QS3D Review workbook transient Count stability source guard")


if __name__ == "__main__":
    main()
