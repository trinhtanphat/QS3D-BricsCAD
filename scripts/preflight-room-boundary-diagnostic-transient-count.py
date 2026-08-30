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
):
    if token not in helper:
        raise SystemExit("Room diagnostic bounded materializer missing token: " + token)

if helper.count(rebound) < 4:
    raise SystemExit("Room diagnostic materializer must keep pre-move, terminal, post-success, and final Count rebounds.")

pre = helper.index(rebound)
move = helper.index("if (!enumerator.MoveNext())", pre)
terminal = helper.index(rebound, move)
post = helper.index(rebound, terminal + len(rebound))
known = helper.index("observedCount >= knownCount.Value", post)
cap = helper.index("observedCount >= MaxInputSegments", known)
current = helper.index("var segment = enumerator.Current;", cap)
final = helper.index(rebound, current)
if not (pre < move < terminal < post < known < cap < current < final):
    raise SystemExit("Room diagnostic traversal ordering regressed around Count/MoveNext/Current.")

for token in (
    "AdvertisedOverrunRejectsBeforeSecondCurrent",
    "TransientMode.Growth",
    "TransientMode.Shrink",
    "TransientMode.Negative",
    "TransientMode.Conflict",
    "Equal(0, source.CurrentReads)",
    "StableCountedInputRemainsAccepted",
):
    if token not in smoke:
        raise SystemExit("Room diagnostic transient-Count smoke missing assertion: " + token)

print("PASS room boundary diagnostic transient Count-before-Current guard")
