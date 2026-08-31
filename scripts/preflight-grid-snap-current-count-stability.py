#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridSnapInputMaterializer.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridSnapCurrentCountStabilitySmoke.cs"


def require(text: str, marker: str, label: str) -> int:
    index = text.find(marker)
    if index < 0:
        raise SystemExit(f"ERROR: Grid snap Current Count guard missing {label}: {marker}")
    return index


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    pre_move = require(source, "ValidateKnownCount(curves, admittedCount, label);\n                    var moved = enumerator.MoveNext();", "pre-MoveNext Count rebound")
    post_move = require(source, "var moved = enumerator.MoveNext();\n                    ValidateKnownCount(curves, admittedCount, label);", "post-MoveNext Count rebound")
    overrun = require(source, "if (admittedCount.HasValue && result.Count >= admittedCount.Value)", "known-count overrun rejection")
    current = require(source, "var curve = enumerator.Current;", "detached Current read")
    post_current = require(source, "var curve = enumerator.Current;\n                    ValidateKnownCount(curves, admittedCount, label);", "post-Current Count rebound")
    retain = require(source, "result.Add(curve);", "retention after rebound")

    if not (pre_move < post_move < overrun < current <= post_current < retain):
        raise SystemExit("ERROR: Grid snap Count ordering must be pre-MoveNext -> post-MoveNext -> overrun -> Current -> post-Current rebound -> retention")

    if source.count("ValidateKnownCount(curves, admittedCount, label);") != 4:
        raise SystemExit("ERROR: Grid snap materializer must contain exactly four traversal/final Count rebound calls")

    require(smoke, "Equal(7, source.CountReads);", "stable one-item seven-observation contract")
    require(smoke, "GridLineSnapPlanner.TryFindNearest", "LINE regression")
    require(smoke, "GridArcSnapPlanner.TryFindNearest", "ARC regression")

    print("PASS Grid snap Current-induced Count stability source guard")


if __name__ == "__main__":
    main()
