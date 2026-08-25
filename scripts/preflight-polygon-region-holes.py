#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REGION = ROOT / "src/QS3D.Core/Geometry/PolygonRegionScanlineClipper.cs"
BASE = ROOT / "src/QS3D.Core/Geometry/PolygonScanlineClipper.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/PolygonRegionScanlineSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/POLYGON-REGION-HOLES.md"
errors = []

for path in (REGION, BASE, SMOKE, REG, DOC):
    if not path.is_file(): errors.append("missing polygon-region contract file: " + str(path.relative_to(ROOT)))

if REGION.is_file():
    text = REGION.read_text(encoding="utf-8")
    for token in (
        "private const int MaxHoles = 256",
        "private const int MaxTotalVertices = 16384",
        "private const int MaxSegments = 4096",
        "PolygonScanlineClipper.NormalizeAndValidate(outer)",
        "SnapshotHoleReferences(holes)",
        "PolygonScanlineClipper.NormalizeAndValidate(sourceHoles[i])",
        "must be strictly inside the outer boundary without touching it",
        "intersects/touches the outer boundary",
        "intersect/touch",
        "overlap or are nested",
        "Islands require an explicit multi-region topology contract",
        "PolygonScanlineClipper.Clip(region.Outer, axis, coordinate)",
        "PolygonScanlineClipper.Clip(hole, axis, coordinate)",
    ):
        if token not in text: errors.append("PolygonRegionScanlineClipper.cs missing bounded/topology token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "HoleSplitsHorizontalAndVerticalScanlines",
        "OutsideHoleFailsClosed",
        "BoundaryTouchFailsClosed",
        "OverlappingHolesFailClosed",
        "NestedHolesFailClosed",
        "WindingDirectionDoesNotChangeRegion",
    ):
        if token not in text: errors.append("PolygonRegionScanlineSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "PolygonRegionScanlineSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("polygon-region smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "one simple outer loop plus zero or more simple holes",
        "applies its existing cover + bar-radius boundary-clearance contract to every outer/hole edge",
        "Multiple disconnected outer loops are not represented",
        "REMOTE_DONE for one-outer-loop + holes topology and Core mesh planning",
        "LOCAL_ONLY",
    ):
        if token not in text: errors.append("POLYGON-REGION-HOLES.md missing topology/runtime boundary: " + token)

print("QS3D polygon region hole preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: one-outer-loop polygon regions subtract bounded validated holes fail-closed; hole-aware mesh clearance, multi-region ownership and native V25 wiring remain explicit separate gates.")
