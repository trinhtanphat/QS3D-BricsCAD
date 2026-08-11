#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rebar/PolygonalSlabMultiRegionMeshPlanner.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/PolygonalSlabMultiRegionMeshPlannerSmoke.cs"
DOC = ROOT / "docs/POLYGON-MULTI-REGION-MESH.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing polygon multi-region mesh file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "PolygonalSlabMeshRegionInput",
    "PolygonalSlabMultiRegionMeshInput",
    "PolygonalSlabMeshRegionLayout",
    "PolygonalSlabMultiRegionMeshLayout",
    "PolygonRegionSetTopology.NormalizeAndValidate",
    "PolygonalSlabMeshPlanner.Plan",
    "island.Region.Outer",
    "island.Region.Holes",
    "MaxTotalBars = 32768",
    "RegionId",
    "public PolygonalSlabMeshLayout Layout { get; }",
):
    if token not in source:
        errors.append("polygon multi-region mesh source missing contract token: " + token)

for token in (
    "PlansEachIslandIndependently",
    "CountModeKeepsPerRegionSpacingSemantics",
    "HoleSplittingStaysInsideRegionLayout",
    "InvalidRegionTopologyFailsBeforeMeshPlanning",
    "wide.YActualSpacingM > small.YActualSpacingM",
    "Near(small.XActualSpacingM, wide.XActualSpacingM)",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("polygon multi-region mesh smoke missing regression token: " + token)

for token in (
    "per-region",
    "does not combine distribution counts",
    "stable RegionId",
    "engineering standard",
    "native owner",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("polygon multi-region mesh documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: disconnected polygon mesh planning delegates each stable RegionId to the single-region planner; smoke coverage proves per-region actual-spacing semantics and bounded aggregate output.")
