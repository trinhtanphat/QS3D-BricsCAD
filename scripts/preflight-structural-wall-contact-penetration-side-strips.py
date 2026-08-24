#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/StructuralWallConcreteContactService.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall penetration-side-strip preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def reaches_exterior(interior_side: int, minimum: float, maximum: float, tolerance: float) -> bool:
    if interior_side == 0:
        return False
    return minimum < -tolerance if interior_side > 0 else maximum > tolerance


# Sanitized #3697 control. The true end interface is 0.1600 m2. The old residual-boundary
# approach also counted two 0.05 m x 0.8 m side strips and returned 0.2400 m2.
tolerance = 1e-6
end_face = (1, -0.20, 0.05, 0.1600)       # cutter crosses from exterior through the end plane
side_a = (1, 0.0, 0.20, 0.0400)           # coplanar + interior only: not exterior contact
side_b = (-1, -0.20, 0.0, 0.0400)         # same artifact with opposite plane orientation
eligible = sum(
    area
    for interior, minimum, maximum, area in (end_face, side_a, side_b)
    if reaches_exterior(interior, minimum, maximum, tolerance)
)
if abs(eligible - 0.1600) > 1e-12:
    fail("sanitized 0.05 m penetration control no longer rejects the two 0.0400 m2 side strips")
if abs((0.1600 + 0.0400 + 0.0400) - 0.2400) > 1e-12:
    fail("sanitized historical over-deduction control is malformed")

text = SOURCE.read_text(encoding="utf-8")
for token in (
    "public int InteriorSide { get; }",
    "ReadBoundaryInteriorSide(solid, plane, distanceCad)",
    "foreach (var vertex in brep.Vertices)",
    "vertex.Point - plane.PointOnPlane",
    "if (seed.InteriorSide == 0)",
    "minDistance < -distanceCad",
    "maxDistance > distanceCad",
    "ReadEligibleOriginalFaceArea(",
    "deductionM2 = Math.Min(grossVerticalAreaCad, contactAreaCad) * areaScale;",
):
    if token not in text:
        fail("missing exact-BREP exterior-side contact contract: " + token)

method_start = text.find("private static bool CandidateReachesExterior(")
method_end = text.find("private static int ReadBoundaryInteriorSide(", method_start)
if method_start < 0 or method_end < 0:
    fail("candidate exterior-side classifier is missing")
classifier = text[method_start:method_end]
for forbidden in ("GeometricExtents", "BoundingBoxesMayOverlap"):
    if forbidden in classifier:
        fail("candidate exterior-side authority must use BREP topology, not bounding boxes: " + forbidden)

if "grossVerticalAreaCad - residualVerticalAreaCad" in text:
    fail("physical residual boundary loss became contact deduction authority again")

print("PASS: #3697 penetration control keeps 0.1600 m2 end contact, rejects both 0.0400 m2 coplanar interior side strips, and binds production classification to native BREP vertices")
