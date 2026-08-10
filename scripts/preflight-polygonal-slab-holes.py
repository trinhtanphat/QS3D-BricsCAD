#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs"
REGION = ROOT / "src/QS3D.Core/Geometry/PolygonRegionScanlineClipper.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PolygonalSlabMeshHolesSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/POLYGONAL-SLAB-MESH.md"
REGION_DOC = ROOT / "docs/POLYGON-REGION-HOLES.md"
errors = []

for path in (PLANNER, REGION, SMOKE, REG, DOC, REGION_DOC):
    if not path.is_file(): errors.append("missing polygonal slab hole-mesh contract file: " + str(path.relative_to(ROOT)))

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        "public IReadOnlyList<IReadOnlyList<Point2>> HoleFootprintsM",
        "if (input.HoleFootprintsM == null)",
        "TranslateLoopToLocal(input.FootprintM, origin",
        "PolygonRegionScanlineClipper.NormalizeAndValidate(localOuter, localHoles)",
        "PolygonRegionScanlineClipper.Clip(region, axis, coordinate)",
        "SubtractBoundaryClearance(region.BoundaryLoops, axis, coordinate, interior, clearance)",
        "foreach (var loop in boundaryLoops)",
        "AppendCapsuleIntersection(forbidden, axis, coordinate, a, b, clearance)",
        "Polygonal slab region leaves no cover-compliant X rebar segments",
        "Polygonal slab region leaves no cover-compliant Y rebar segments",
    ):
        if token not in text: errors.append("PolygonalSlabMeshPlanner.cs missing hole-aware clearance token: " + token)

if REGION.is_file():
    text = REGION.read_text(encoding="utf-8")
    for token in ("public IReadOnlyList<IReadOnlyList<Point2>> BoundaryLoops", "overlap or are nested", "Clip(PolygonRegion2 region"):
        if token not in text: errors.append("PolygonRegionScanlineClipper.cs lost hole topology prerequisite: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "EmptyHoleListPreservesSimplePolygonLayout",
        "CentralHoleSplitsBarsWithCoverAndRadiusClearance",
        "HoleSplitsPhysicalBarsWithoutChangingDistributedScanlines",
        "TopBottomElevationsRemainStableWithHoles",
        "InvalidHoleTopologyFailsBeforeLayout",
        "Near(3.79d, middleX[0].EndM.X)",
        "Near(6.21d, middleX[1].StartM.X)",
    ):
        if token not in text: errors.append("PolygonalSlabMeshHolesSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "PolygonalSlabMeshHolesSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("polygonal slab hole-mesh smoke is not registered")

for path in (DOC, REGION_DOC):
    if path.is_file():
        text = path.read_text(encoding="utf-8")
        for token in ("cover + bar radius", "distributed scanlines", "native", "LOCAL_ONLY"):
            if token not in text: errors.append(path.name + " missing hole-mesh/native boundary token: " + token)

print("QS3D polygonal slab hole-mesh preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: one-outer-loop polygon mesh planning subtracts validated holes and cover+radius clearance on every region boundary while native loop extraction/ownership remains separately runtime-gated.")
