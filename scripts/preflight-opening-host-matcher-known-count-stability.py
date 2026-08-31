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
):
    if token not in helper:
        raise SystemExit("Opening host bounded materializer missing token: " + token)

if helper.count(rebound) < 4:
    raise SystemExit("Opening host materializer must keep pre-move, terminal, post-success, and final Count rebounds.")

pre = helper.index(rebound)
move = helper.index("if (!enumerator.MoveNext())", pre)
terminal = helper.index(rebound, move)
post = helper.index(rebound, terminal + len(rebound))
known = helper.index("observedCount >= knownCount.Value", post)
cap = helper.index("observedCount >= MaxSegments", known)
current = helper.index("var segment = enumerator.Current;", cap)
final = helper.index(rebound, current)
if not (pre < move < terminal < post < known < cap < current < final):
    raise SystemExit("Opening host traversal ordering regressed around Count/MoveNext/Current.")

for token in (
    "AdvertisedOverrunRejectsBeforeSecondCurrent",
    "TransientMode.Growth",
    "TransientMode.Shrink",
    "TransientMode.Negative",
    "TransientMode.Conflict",
    "Equal(0, source.CurrentReads)",
    "StableCountedInputStillMatches",
):
    if token not in smoke:
        raise SystemExit("Opening host Count stability smoke missing assertion: " + token)

print("PASS opening host matcher known Count stability guard")
