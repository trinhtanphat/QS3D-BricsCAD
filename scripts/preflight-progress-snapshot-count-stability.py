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
    "while (enumerator.MoveNext())",
    "knownCount.HasValue && result.Count >= knownCount.Value",
    "var item = enumerator.Current;",
    "var postTraversalKnownCount = SnapshotKnownCount(source, parameterName, label);",
    "known count changed during traversal",
    "private static int? SnapshotKnownCount<T>",
    "source is ICollection<T>",
    "source is IReadOnlyCollection<T>",
    "source is ICollection nonGenericCollection",
]
required_smoke = [
    "[ModuleInitializer]",
    "KnownCountOverrunStopsBeforeCurrentAndLaterTail",
    "PostTraversalUniformCountDriftFailsClosed",
    "PostTraversalSingleSurfaceConflictFailsClosed",
    "CurrentReads",
    "later tail must never win",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Progress snapshot Count-stability preflight missing contract tokens: " + ", ".join(missing))

# Ordering matters: MoveNext may expose the existence of an unexpected item, but
# admitted known Count must reject it before Current, null semantics or retention.
loop = source.index("while (enumerator.MoveNext())")
overrun = source.index("knownCount.HasValue && result.Count >= knownCount.Value", loop)
limit = source.index("result.Count == MaximumEntries", loop)
current = source.index("var item = enumerator.Current;", loop)
null_check = source.index("item == null", loop)
retain = source.index("result.Add(item)", loop)
if not (loop < overrun < limit < current < null_check < retain):
    raise SystemExit("Progress snapshot Count overrun guard must precede Current/streaming-null-retention semantics")

post = source.index("var postTraversalKnownCount = SnapshotKnownCount(source, parameterName, label);")
return_result = source.index("return result;", post)
if not post < return_result:
    raise SystemExit("Progress snapshot Count evidence must be rebound before publication")

print("PASS progress snapshot known Count early-overrun and traversal-stability source contract")
