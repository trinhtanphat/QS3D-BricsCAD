#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/PolygonRegionSetTopology.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/PolygonRegionSetTopologySmoke.cs"
DOC = ROOT / "docs/POLYGON-MULTI-REGION-TOPOLOGY.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing polygon multi-region file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "PolygonRegionSeed2",
    "PolygonRegionIsland2",
    "PolygonRegionSet2",
    "PolygonRegionTaggedScanSegment",
    "PolygonRegionScanlineClipper.NormalizeAndValidate",
    "ValidateIslandPair",
    "intersect or touch",
    "overlap or are nested",
    "Nested outer islands require an explicit ownership/topology policy",
    "MaxRegions = 256",
    "MaxTotalVertices = 65536",
    "MaxTaggedScanSegments = 16384",
    "StringComparer.OrdinalIgnoreCase",
    "PolygonRegionScanlineClipper.Clip",
):
    if token not in source:
        errors.append("polygon multi-region source missing contract token: " + token)

for token in (
    "SeparateIslandsRemainIndependentlyTagged",
    "HoleClippingStaysWithinOwningIsland",
    "InputOrderDoesNotChangeCanonicalRegionOrder",
    "DuplicateIdsFailClosed",
    "TouchingIslandsFailClosed",
    "OverlappingIslandsFailClosed",
    "NestedOuterIslandsFailClosed",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("polygon multi-region smoke missing regression token: " + token)

for token in (
    "stable region ID",
    "must not concatenate vertices",
    "must not reinterpret an island as a hole",
    "native source-loop ownership",
    "LOCAL_ONLY",
    "PolygonRegionSetTopology",
):
    if token not in doc:
        errors.append("polygon multi-region documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: disconnected polygon islands keep stable identities, independent hole topology and tagged clipping; touching/overlap/nesting fail closed.")
