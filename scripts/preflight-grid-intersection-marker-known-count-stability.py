#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Geometry" / "GridIntersectionMarkerPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridIntersectionMarkerKnownCountStabilitySmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

plan_start = source.index("        public static IReadOnlyList<GridIntersectionMarkerPlan> Plan(")
plan_end = source.index("    }\n}", plan_start)
region = source[plan_start:plan_end]
for token in ("GetKnownInputCount(intersections)", "MaterializeBounded(intersections, knownCount)"):
    if token not in region:
        raise SystemExit("Grid marker Count stability guard missing: " + token)
if "intersections.Take(MaxMarkers + 1).ToList()" in region:
    raise SystemExit("Grid marker planner must not use LINQ materialization before Count rebound.")

helper_start = source.index("        private static List<GridIntersection> MaterializeBounded(")
helper_end = source.index("        private static int? GetKnownInputCount", helper_start)
helper = source[helper_start:helper_end]
rebound = "RequireStableKnownInputCount(intersections, knownCount)"
for token in ("if (!enumerator.MoveNext())", rebound, "observedCount >= knownCount.Value", "observedCount >= MaxMarkers", "var intersection = enumerator.Current;"):
    if token not in helper:
        raise SystemExit("Grid marker bounded materializer missing: " + token)
if helper.count(rebound) < 4:
    raise SystemExit("Grid marker planner must preserve pre-move, terminal, post-success, and final Count rebounds.")
pre = helper.index(rebound)
move = helper.index("if (!enumerator.MoveNext())", pre)
terminal = helper.index(rebound, move)
post = helper.index(rebound, terminal + len(rebound))
known = helper.index("observedCount >= knownCount.Value", post)
cap = helper.index("observedCount >= MaxMarkers", known)
current = helper.index("var intersection = enumerator.Current;", cap)
final = helper.index(rebound, current)
if not (pre < move < terminal < post < known < cap < current < final):
    raise SystemExit("Grid marker Count/MoveNext/Current ordering regressed.")

for token in ("AdvertisedOverrunRejectsBeforeSecondCurrent", "TransientMode.Growth", "TransientMode.Shrink", "TransientMode.Negative", "TransientMode.Conflict", "Equal(0, source.CurrentReads)", "StableCountedInputStillPlans"):
    if token not in smoke:
        raise SystemExit("Grid marker Count stability smoke missing: " + token)
print("PASS grid intersection marker known Count stability guard")
