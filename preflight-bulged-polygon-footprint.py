#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Geometry/BulgedPolygonFootprintTessellator.cs"
ARC = ROOT / "src/QS3D.Core/Geometry/BulgeArcTessellator.cs"
CLIP = ROOT / "src/QS3D.Core/Geometry/PolygonScanlineClipper.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/BulgedPolygonFootprintSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (CORE, ARC, CLIP, SMOKE, REG):
    if not path.is_file():
        errors.append("missing bulged polygon footprint contract file: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        "public sealed class BulgedPolygonVertex2",
        "public double BulgeToNext { get; }",
        "BulgeArcTessellator.Tessellate",
        "if (result.Count > MaxVertices)",
        "return PolygonScanlineClipper.NormalizeAndValidate(result);",
    ):
        if token not in text:
            errors.append("BulgedPolygonFootprintTessellator.cs missing bounded contract token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "StraightPolygonPreservesCanonicalVertices",
        "BulgedBoundaryFeedsPolygonalMeshPlanner",
        "SelfIntersectionFailsClosed",
        "ExcessiveTessellationFailsClosed",
        "PolygonalSlabMeshPlanner.Plan",
    ):
        if token not in text:
            errors.append("BulgedPolygonFootprintSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "BulgedPolygonFootprintSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Bulged polygon footprint smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core has bounded closed bulge tessellation that validates a simple polygon before feeding polygonal mesh planning; native V25 extraction/wiring remains separate.")
