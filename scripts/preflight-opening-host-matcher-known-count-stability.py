#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "OpeningHostMatcher.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "OpeningHostMatcherKnownCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = source.index("        public OpeningHostMatchResult Match(")
end = source.index("        private static int Compare", start)
region = source[start:end]

for token in (
    "GetKnownInputCount(source)",
    "MaterializeBoundedSegments(source, knownCount)",
):
    if token not in region:
        raise SystemExit("Opening host Count stability guard missing token: " + token)

if "source.Take(MaxSegments + 1).ToList()" in region:
    raise SystemExit("Opening host matcher must not use LINQ materialization before Count rebound.")

helper_start = source.index("        private static List<OpeningHostSegment> MaterializeBoundedSegments(")
helper_end = source.index("        private static int? GetKnownInputCount", helper_start)
helper = source[helper_start:helper_end]
rebound = "RequireStableKnownInputCount(source, knownCount)"

for token in (
    "using (var enumerator = source.GetEnumerator())",
    "if (!enumerator.MoveNext())",
    rebound,
    "observedCount >= knownCount.Value",
    "observedCount >= MaxSegments",
    "var segment = enumerator.Current;",
    "segments.Add(segment);",
):
    if token not in helper:
        raise SystemExit("Opening host bounded materializer missing token: " + token)

if helper.count(rebound) < 5:
    raise SystemExit("Opening host materializer must keep pre-move, terminal, post-success, post-Current, and final Count rebounds.")

pre = helper.index(rebound)
move = helper.index("if (!enumerator.MoveNext())", pre)
terminal = helper.index(rebound, move)
post_move = helper.index(rebound, terminal + len(rebound))
known = helper.index("observedCount >= knownCount.Value", post_move)
cap = helper.index("observedCount >= MaxSegments", known)
current = helper.index("var segment = enumerator.Current;", cap)
post_current = helper.index(rebound, current)
retain = helper.index("segments.Add(segment);", post_current)
final = helper.index(rebound, retain)
if not (pre < move < terminal < post_move < known < cap < current < post_current < retain < final):
    raise SystemExit("Opening host traversal must preserve MoveNext -> Count rebound -> cardinality gates -> Current -> Count rebound -> retention -> final rebound.")

for token in (
    "AdvertisedOverrunRejectsBeforeSecondCurrent",
    "AssertMoveNextTransientRejected(TransientMode.Growth)",
    "AssertMoveNextTransientRejected(TransientMode.Shrink)",
    "AssertMoveNextTransientRejected(TransientMode.Negative)",
    "AssertMoveNextTransientRejected(TransientMode.Conflict)",
    "AssertCurrentTransientRejected(TransientMode.Growth)",
    "AssertCurrentTransientRejected(TransientMode.Shrink)",
    "AssertCurrentTransientRejected(TransientMode.Negative)",
    "AssertCurrentTransientRejected(TransientMode.Conflict)",
    "Equal(0, source.CurrentReads)",
    "Equal(1, source.PostCurrentCountRebounds)",
    "StableCountedInputStillMatches",
    "StreamingInputStillMatches",
):
    if token not in smoke:
        raise SystemExit("Opening host Count stability smoke missing assertion: " + token)

print("PASS opening host matcher known Count stability guard")
