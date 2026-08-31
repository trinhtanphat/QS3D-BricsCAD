#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityReportTotals.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityReportTotalsTransientCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "private static void RequireKnownRowCountStable(",
    "var currentKnownCount = SnapshotKnownRowCount(rows, out var currentKnownCountSources);",
    "expectedKnownCount != currentKnownCount || expectedKnownCountSources != currentKnownCountSources",
    "while (true)",
    "RequireKnownRowCountStable(rows, knownCount, knownCountSources);\n                    if (!enumerator.MoveNext())",
    "if (!enumerator.MoveNext())\n                    {\n                        RequireKnownRowCountStable(rows, knownCount, knownCountSources);",
    "RequireKnownRowCountStable(rows, knownCount, knownCountSources);\n                    if (knownCount.HasValue && rowIndex >= knownCount.Value)",
    "var row = enumerator.Current;",
)
for token in required_source:
    if token not in source:
        raise SystemExit(f"QuantityReportTotals transient Count guard missing source contract: {token}")

pre_move = source.index("RequireKnownRowCountStable(rows, knownCount, knownCountSources);\n                    if (!enumerator.MoveNext())")
move = source.index("if (!enumerator.MoveNext())", pre_move)
post_move = source.index("RequireKnownRowCountStable(rows, knownCount, knownCountSources);", move + 1)
overrun = source.index("if (knownCount.HasValue && rowIndex >= knownCount.Value)", post_move)
current = source.index("var row = enumerator.Current;", overrun)
if not (pre_move < move < post_move < overrun < current):
    raise SystemExit("QuantityReportTotals transient Count guard requires rebound before MoveNext and after successful MoveNext before Current.")

required_smoke = (
    "RejectTransientCountGrowthBeforeNextMoveNext();",
    "RejectTransientCountShrinkBeforeNextMoveNext();",
    "RejectTransientNegativeCountBeforeNextMoveNext();",
    "RejectTransientConflictingCountsBeforeNextMoveNext();",
    "AcceptStableCountedRows();",
    "MoveNextCalls != 1 || source.CurrentReads != 1",
    "TransientCountMode.Grow",
    "TransientCountMode.Shrink",
    "TransientCountMode.Negative",
    "TransientCountMode.Conflict",
    "[ModuleInitializer]",
)
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"QuantityReportTotals transient Count guard missing deterministic smoke contract: {token}")

print("PASS QuantityReportTotals transient known-Count stability source guard")
