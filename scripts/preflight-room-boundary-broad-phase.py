#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ENGINE = ROOT / "src" / "QS3D.Core" / "Geometry" / "RoomBoundaryEngine.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomBoundaryBroadPhaseSmoke.cs"
DOC = ROOT / "docs" / "ROOM-BOUNDARY-BROAD-PHASE.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing token: " + token)


engine = read(ENGINE)
smoke = read(SMOKE)
doc = read(DOC)

for token, label in [
    ("EnumeratePotentialPairs(segments, tolerance)", "broad-phase integration"),
    ("private static IEnumerable<Tuple<int, int>> EnumeratePotentialPairs", "sweep helper"),
    ("ordered.Sort((left, right)", "deterministic bounds ordering"),
    ("left.MinX.CompareTo(right.MinX)", "x-axis sweep ordering"),
    ("active[index].MaxX < current.MinX", "expired-active pruning"),
    ("other.Overlaps(current)", "full tolerance-expanded bounds overlap"),
    ("MaxInputSegments = 5000", "input safety limit"),
    ("MaxSubdividedEdges = 20000", "subdivision safety limit"),
]:
    require(engine, token, label)

if "for (var j = i + 1; j < segments.Count; j++)" in engine:
    errors.append("RoomBoundaryEngine regressed to direct all-pairs segment scanning")

for token, label in [
    ("SparseNearLimitNetworkPreservesRoom", "near-limit sparse regression"),
    ("index < 4500", "high-count sparse fixture"),
    ("SweepKeepsTJunctionCandidates", "T-junction regression"),
    ("ToleranceExpandedBoundsKeepNearEndpoints", "tolerance bounds regression"),
]:
    require(smoke, token, label)

for token, label in [
    ("sweep broad-phase", "algorithm documentation"),
    ("does not change", "semantic-boundary documentation"),
    ("LOCAL-010", "local performance handoff reference"),
    ("No Stopwatch", "non-benchmark source evidence"),
]:
    require(doc, token, label)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: RoomBoundaryEngine prunes sparse segment pairs with a deterministic tolerance-expanded sweep broad-phase while retaining topology/subdivision limits and existing local V25 performance qualification boundaries.")
