#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rebar/RebarShapePath.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RebarShapePathCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/rebar-shape-path-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Rebar shape path Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "var admittedPointCount = points.Count;",
    "if (admittedPointCount < 2)",
    "var snapshot = new List<RebarShapePoint>(admittedPointCount);",
    "for (var index = 0; index < admittedPointCount; index++)",
    "RequireStablePointCount(points, admittedPointCount);",
    "snapshot.Add(points[index]);",
    "Rebar shape path point Count changed during snapshot.",
    "Points = snapshot.AsReadOnly();",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Rebar shape path Count-stability source contract missing: " + repr(missing))

ctor_start = source.index("public RebarShapePath(string shapeCode, IReadOnlyList<RebarShapePoint> points)")
builder_start = source.index("public static class RebarShapePathBuilder", ctor_start)
contract = source[ctor_start:builder_start]
if "new List<RebarShapePoint>(points)" in contract:
    raise SystemExit("Rebar shape path must not delegate snapshot traversal to caller enumeration.")

admission = contract.index("var admittedPointCount = points.Count;")
minimum = contract.index("if (admittedPointCount < 2)", admission)
allocation = contract.index("var snapshot = new List<RebarShapePoint>(admittedPointCount);", minimum)
loop = contract.index("for (var index = 0; index < admittedPointCount; index++)", allocation)
pre_index = contract.index("RequireStablePointCount(points, admittedPointCount);", loop)
index_read = contract.index("snapshot.Add(points[index]);", pre_index)
final_rebound = contract.index("RequireStablePointCount(points, admittedPointCount);", index_read)
publication = contract.index("Points = snapshot.AsReadOnly();", final_rebound)
if not (admission < minimum < allocation < loop < pre_index < index_read < final_rebound < publication):
    raise SystemExit("Rebar shape path Count-stability ordering changed.")

required_smoke = (
    "GrowthRejectsBeforeUnexpectedIndexerRead",
    "ShrinkRejectsBeforeMissingIndexerRead",
    "PostTraversalCountDriftRejects",
    "TooFewRejectsBeforeIndexerRead",
    "StablePointsSnapshotWithoutEnumeration",
    "Equal(1, source.IndexerReads",
    "Equal(2, source.IndexerReads",
    "Equal(0, source.EnumeratorRequests",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Rebar shape path Count-stability smoke contract missing: " + repr(missing_smoke))

print("PASS rebar shape path Count-stability source guard")
