#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


planner = read("src/QS3D.Core/Geometry/PolygonScanlineClipper.cs")
smoke = read("tests/QS3D.Core.SmokeTests/PolygonScanlineClipperSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
mesh = read("src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs")
mesh_smoke = read("tests/QS3D.Core.SmokeTests/PolygonalSlabMeshSmoke.cs")

for token in [
    "MaxVertices = 4096",
    "MaxSegments = 2048",
    "NormalizeAndValidate",
    "ValidateSimple",
    "Polygon self-intersects",
    "Half-open edge rule",
    "DeduplicateIntersections",
    "odd intersection count",
    "PolygonScanAxis.Horizontal",
    "PolygonScanAxis.Vertical",
    "axis != PolygonScanAxis.Horizontal && axis != PolygonScanAxis.Vertical",
]:
    require(planner, token, "polygon scanline planner")

for token in [
    "RectangleClipsBothAxes",
    "ConcavePolygonCreatesDeterministicSegments",
    "ClosingVertexMayBeRepeated",
    "SelfIntersectionFailsClosed",
    "BoundaryVertexRuleIsDeterministic",
    "InvalidAxisFailsClosed",
]:
    require(smoke, token, "polygon scanline smoke")

for token in [
    "PolygonalSlabMeshPlanner",
    "PolygonScanlineClipper.NormalizeAndValidate",
    "SubtractBoundaryClearance",
    "AppendCapsuleIntersection",
    "RebarMath.Add(cover, xRadius",
    "RebarMath.Add(cover, yRadius",
    "MaxBars = 8192",
    "MaxForbiddenIntervalsPerScanline = 16384",
    "Polygonal slab footprint leaves no cover-compliant X rebar segments",
    "Polygonal slab footprint leaves no cover-compliant Y rebar segments",
]:
    require(mesh, token, "polygon mesh planner")

for token in [
    "RectangleMatchesLegacyLengthsAndCount",
    "ConcaveFootprintSplitsBarsDeterministically",
    "SlopedBoundaryRespectsEuclideanCover",
    "SelfIntersectionFailsClosed",
    "ImpossibleCoverFailsClosed",
    "AggregateBarLimitFailsClosed",
    "AssertBoundaryDistance",
    "[ModuleInitializer]",
]:
    require(mesh_smoke, token, "polygon mesh smoke")

require(registration, "PolygonScanlineClipperSmoke.Run();", "smoke registration")

print("[PASS] bounded simple-polygon scanline clipping plus cover-safe polygonal slab/foundation mesh planning is statically guarded for rectangle compatibility, concavity, sloped edges, invalid topology, impossible cover and aggregate limits")
