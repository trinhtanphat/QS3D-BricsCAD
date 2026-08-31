#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "RoomBoundaryDiagnostics.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomBoundaryDiagnosticTransientCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

start = source.index("        public RoomBoundaryDiagnosticAnalysis Analyze(")
end = source.index("        private static int? GetKnownInputCount", start)
region = source[start:end]

for token in (
    "MaterializeBoundedSegments(source, knownCount)",
    "GetKnownInputCount(source)",
):
    if token not in region:
        raise SystemExit("Room diagnostic transient-Count guard missing token: " + token)

if "source.Take(MaxInputSegments + 1).ToList()" in region:
    raise SystemExit("Room diagnostic source must not use LINQ materialization before Count rebound.")

helper_start = source.index("        private static List<BoundarySegment> MaterializeBoundedSegments(")
helper_end = source.index("        private static int? GetKnownInputCount", helper_start)
helper = source[helper_start:helper_end]
rebound = "RequireStableKnownInputCount(source, knownCount)"

for token in (
    "using (var enumerator = source.GetEnumerator())",
    "if (!enumerator.MoveNext())",
    rebound,
    "observedCount >= knownCount.Value",
    "observedCount >= MaxInputSegments",
    "var segment = enumerator.Current;",
    "segments.Add(segment);",
):
    if token not in helper:
        raise SystemExit("Room diagnostic bounded materializer missing token: " + token)

if helper.count(rebound) < 5:
    raise SystemExit("Room diagnostic materializer must keep pre-move, terminal, post-success, post-Current, and final Count rebounds.")

pre = helper.index(rebound)
move = helper.index("if (!enumerator.MoveNext())", pre)
terminal = helper.index(rebound, move)
post_move = helper.index(rebound, terminal + len(rebound))
known = helper.index("observedCount >= knownCount.Value", post_move)
cap = helper.index("observedCount >= MaxInputSegments", known)
current = helper.index("var segment = enumerator.Current;", cap)
post_current = helper.index(rebound, current)
retain = helper.index("segments.Add(segment);", post_current)
final = helper.index(rebound, retain)
if not (pre < move < terminal < post_move < known < cap < current < post_current < retain < final):
    raise SystemExit("Room diagnostic traversal must preserve MoveNext -> Count rebound -> cardinality gates -> Current -> Count rebound -> retention -> final rebound.")

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
    "StableCountedInputRemainsAccepted",
    "StreamingInputRemainsAccepted",
):
    if token not in smoke:
        raise SystemExit("Room diagnostic transient-Count smoke missing assertion: " + token)

print("PASS room boundary diagnostic MoveNext/Current known Count stability guard")
