#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Progress/ProgressSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProgressSnapshotCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var knownCount = SnapshotKnownCount(source, parameterName, label);",
    "using (var enumerator = source.GetEnumerator())",
    "while (true)",
    "RequireKnownCountStable(source, knownCount, parameterName, label);",
    "if (!enumerator.MoveNext())",
    "knownCount.HasValue && result.Count >= knownCount.Value",
    "var item = enumerator.Current;",
    "private static void RequireKnownCountStable<T>",
    "known count changed during traversal",
    "private static int? SnapshotKnownCount<T>",
    "source is ICollection<T>",
    "source is IReadOnlyCollection<T>",
    "source is ICollection nonGenericCollection",
]
required_smoke = [
    "[ModuleInitializer]",
    "KnownCountOverrunStopsBeforeCurrentAndLaterTail",
    "TransientCountGrowthFailsBeforeCurrent",
    "TransientCountShrinkFailsBeforeCurrent",
    "TransientNegativeCountFailsBeforeCurrent",
    "PostTraversalUniformCountDriftFailsClosed",
    "PostTraversalSingleSurfaceConflictFailsClosed",
    "CurrentReads",
    "later tail must never win",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Progress snapshot Count-stability preflight missing contract tokens: " + ", ".join(missing))

loop = source.index("while (true)")
pre_move = source.index("RequireKnownCountStable(source, knownCount, parameterName, label);", loop)
move = source.index("if (!enumerator.MoveNext())", pre_move)
post_move = source.index("RequireKnownCountStable(source, knownCount, parameterName, label);", move)
overrun = source.index("knownCount.HasValue && result.Count >= knownCount.Value", post_move)
limit = source.index("result.Count == MaximumEntries", overrun)
current = source.index("var item = enumerator.Current;", limit)
null_check = source.index("item == null", current)
retain = source.index("result.Add(item)", null_check)
if not (loop < pre_move < move < post_move < overrun < limit < current < null_check < retain):
    raise SystemExit("Progress snapshot Count stability must bind before MoveNext and after successful MoveNext before Current/retention")

under_yield = source.index("knownCount.HasValue && knownCount.Value != result.Count", retain)
final_rebind = source.index("RequireKnownCountStable(source, knownCount, parameterName, label);", under_yield)
return_result = source.index("return result;", final_rebind)
if not under_yield < final_rebind < return_result:
    raise SystemExit("Progress snapshot Count evidence must be rebound after traversal before publication")

print("PASS progress snapshot known Count early-overrun and transient traversal-stability source contract")
