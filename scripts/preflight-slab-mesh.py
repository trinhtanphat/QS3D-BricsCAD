#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
planner = ROOT / "src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs"
polygon_planner = ROOT / "src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/SlabMeshRegressionSmoke.cs"
polygon_smoke = ROOT / "tests/QS3D.Core.SmokeTests/PolygonalSlabMeshSmoke.cs"
linear = ROOT / "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs"

for path in (planner, polygon_planner, smoke, polygon_smoke, linear):
    if not path.is_file(): errors.append("missing slab-mesh file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "SlabMeshFace", "SlabMeshDirection", "RectangularSlabMeshInput", "SlabMeshBarPlacement",
        "LinearRebarLayoutPlanner.Plan", "IncludeBottom", "IncludeTop", "XClosestToFace",
        "slab X end center cover", "slab Y end center cover", "top + bottom two-direction mesh",
        "MaxBars = 8192", "projectedBars", "new List<SlabMeshBarPlacement>((int)projectedBars)", "ActualSpacingM",
    ):
        if needle not in text: errors.append("slab mesh planner guard missing: " + needle)

if polygon_planner.is_file():
    text = polygon_planner.read_text(encoding="utf-8")
    for needle in (
        "PolygonalSlabMeshInput", "PolygonRegionScanlineClipper.NormalizeAndValidate", "HoleFootprintsM",
        "TranslateLoopToLocal", "RestoreGlobalCoordinates", "CheckedSubtract", "CheckedAdd(origin.X",
        "MaxForbiddenIntervalsPerScanline", "SubtractBoundaryClearance", "AppendCapsuleIntersection",
    ):
        if needle not in text: errors.append("polygon slab mesh planner guard missing: " + needle)
    localize = text.find("var localOuter = TranslateLoopToLocal")
    validate = text.find("PolygonRegionScanlineClipper.NormalizeAndValidate(localOuter, localHoles)")
    restore = text.find("RestoreGlobalCoordinates(bars, origin);")
    if localize < 0 or validate < 0 or restore < 0 or not (localize < validate < restore):
        errors.append("polygon slab mesh far-origin localization ordering changed")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "BottomMeshUsesTwoDirectionsAndCover();", "BothFacesRemainSeparated();", "CountModeIsDeterministic();",
        "ThinSlabIsRejected();", "AmbiguousDistributionIsRejected();", "OversizedAggregateMeshIsRejected();", "ModuleInitializer",
    ):
        if needle not in text: errors.append("slab mesh regression missing: " + needle)

if polygon_smoke.is_file():
    text = polygon_smoke.read_text(encoding="utf-8")
    for needle in (
        "RectangleMatchesLegacyLengthsAndCount();", "ConcaveFootprintSplitsBarsDeterministically();",
        "SlopedBoundaryRespectsEuclideanCover();", "FarOriginMatchesLocalLayout();",
        "SelfIntersectionFailsClosed();", "ImpossibleCoverFailsClosed();", "AggregateBarLimitFailsClosed();",
        "1_000_000_000d",
    ):
        if needle not in text: errors.append("polygon slab mesh regression missing: " + needle)

print("QS3D slab-mesh planner preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: rectangular + polygonal slab mesh planning, cover/stacking/limits, far-origin local-coordinate hardening and smoke coverage are present; native CAD adapter remains separately runtime-gated.")
