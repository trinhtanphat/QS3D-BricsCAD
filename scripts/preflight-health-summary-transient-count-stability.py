#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "HealthSummary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "HealthSummaryTransientCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "RequireKnownCountsWithinLimit(issues, out var expectedKnownCountSources)",
    "private static void RequireKnownCountStable(",
    "expectedKnownCountSources != currentKnownCountSources || expectedKnownCount != currentKnownCount",
    "while (true)",
    "if (!enumerator.MoveNext())",
    "result.Add(enumerator.Current);",
    "knownCountSources |= 1;",
    "knownCountSources |= 2;",
    "knownCountSources |= 4;",
)
for token in required_source:
    if token not in source:
        raise SystemExit("HealthSummary transient Count source guard missing token: " + token)

pre_move = source.index("RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);")
move = source.index("if (!enumerator.MoveNext())", pre_move)
post_move = source.index("RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);", move + 1)
overrun = source.index("if (expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value)", post_move)
current = source.index("result.Add(enumerator.Current);", overrun)
if not (pre_move < move < post_move < overrun < current):
    raise SystemExit("HealthSummary must rebind Count before/after MoveNext and admit cardinality before Current")
if "while (enumerator.MoveNext())" in source:
    raise SystemExit("HealthSummary must not regress to while(enumerator.MoveNext()) for caller-controlled issues")

for token in (
    "KnownCountOverrunRejectsBeforeSecondCurrent",
    "TransientGrowthRejectsBeforeNextMove",
    "TransientShrinkRejectsBeforeNextMove",
    "TransientNegativeRejectsBeforeNextMove",
    "TransientConflictRejectsBeforeNextMove",
    "StableMultiInterfaceCountRemainsAccepted",
    "Equal(2, source.MoveNextCalls);",
    "Equal(1, source.CurrentReads);",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("HealthSummary transient Count smoke missing scenario/token: " + token)

print("PASS health summary transient known-Count stability source guard")
