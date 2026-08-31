#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Revisions" / "QuantityRevisionReport.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityRevisionSummaryCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireStableKnownSummaryCount(rows, knownCount);",
    "var moved = enumerator.MoveNext();",
    "var row = enumerator.Current;",
    "summarizable.Add(row);",
    "rows is ICollection<QuantityRevisionRow>",
    "rows is IReadOnlyCollection<QuantityRevisionRow>",
    "rows is ICollection nonGenericCollection",
    "Quantity revision summary input known Count changed during traversal",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing Quantity revision transient Count source contract: {marker}")

loop = source.index("while (true)")
pre_move = source.index("RequireStableKnownSummaryCount(rows, knownCount);", loop)
move = source.index("var moved = enumerator.MoveNext();", pre_move)
post_move = source.index("RequireStableKnownSummaryCount(rows, knownCount);", move)
overrun = source.index("if (knownCount.HasValue && index >= knownCount.Value)", post_move)
current = source.index("var row = enumerator.Current;", overrun)
post_current = source.index("RequireStableKnownSummaryCount(rows, knownCount);", current)
null_validation = source.index("if (row == null)", post_current)
retain = source.index("summarizable.Add(row);", null_validation)

if not pre_move < move < post_move < overrun < current < post_current < null_validation < retain:
    raise SystemExit("Quantity revision summary must rebind Count around MoveNext and after Current before semantic retention")

if "foreach (var row in rows)" in source:
    raise SystemExit("legacy implicit foreach traversal remains in QuantityRevisionReport.Summarize")

required_smoke = [
    "TransientMoveNextCountDriftFailsBeforeCurrent();",
    "TransientCurrentCountDriftFailsBeforeRetention();",
    "TransientCountRows",
    "DriftBoundary.MoveNext",
    "DriftBoundary.Current",
    "Equal(0, source.CurrentReads);",
    "StableCountedAndStreamingSourcesRemainAccepted();",
    "PostTraversalCountConflictFailsClosed();",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing Quantity revision transient Count smoke contract: {marker}")

print("quantity revision summary transient Count integrity preflight: PASS")
