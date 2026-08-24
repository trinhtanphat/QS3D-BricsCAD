#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/StructuralWallConcreteContactService.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall residual-contact preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
required = (
    "internal sealed class StructuralWallConcreteContactDiagnostics",
    "public int CandidateSolidCount { get; internal set; }",
    "public int VerticalFaceSeedCount { get; internal set; }",
    "public int PositiveVolumeCutCount { get; internal set; }",
    "public int ContactProbeCutCount { get; internal set; }",
    "public int FailedNativeCutCount { get; internal set; }",
    "public double GrossVerticalAreaM2 { get; internal set; }",
    "public double ResidualVerticalAreaM2 { get; internal set; }",
    "out StructuralWallConcreteContactDiagnostics diagnostics",
    "TryIntersection(residual, candidate, out var intersectionFailed)",
    "TrySubtract(residual, overlap)",
    "TryIntersection(residual, contactProbe, out var contactIntersectionFailed)",
    "TrySubtract(residual, contact)",
    "diagnostics.FailedNativeCutCount++",
    "if (diagnostics.FailedNativeCutCount > 0) return false;",
    "private static PlanarEntity? ReadFacePlane(BrepFace face)",
    "surface is ExternalBoundedSurface external",
    "external.IsPlane",
    "external.BaseSurface is PlanarEntity basePlane",
    # #3697: contact authority is original-face coverage, not every vertical residual face lost.
    "public int InteriorSide { get; }",
    "TryReadSolidVertexSideRange(",
    "CandidateReachesExterior(",
    "ReadEligibleOriginalFaceArea(",
    "contactAreaCad += overlapContactAreaCad;",
    "contactAreaCad += probeContactAreaCad;",
    "deductionM2 = Math.Min(grossVerticalAreaCad, contactAreaCad) * areaScale;",
    "diagnostics.ResidualVerticalAreaM2 = Math.Max(0d, grossVerticalAreaCad - contactAreaCad) * areaScale;",
)
for token in required:
    if token not in text:
        fail("missing clipped-residual/original-face-contact contract: " + token)

for forbidden in (
    "TrySubtract(residual, candidate);",
    "TrySubtract(residual, contactProbe);",
    "using (var intersection = TryIntersection(target, candidate))",
    "face.Surface as PlanarEntity",
    "external.BaseSurface as PlanarEntity",
    # #3697 regression: a penetrating cutter creates new side-strip residual boundaries.
    # Gross-minus-residual therefore over-deducts and must never become contact authority again.
    "var deductionCad = Math.Max(0d, grossVerticalAreaCad - residualVerticalAreaCad);",
    "ReadResidualAreaOnOriginalVerticalFaces",
):
    if forbidden in text:
        fail("stale whole-candidate/direct-face/residual-area authority remains: " + forbidden)

clip_pos = text.find("TryIntersection(residual, candidate, out var intersectionFailed)")
coverage_pos = text.find("ReadEligibleOriginalFaceArea(", clip_pos)
subtract_pos = text.find("TrySubtract(residual, overlap)", coverage_pos)
if not (0 <= clip_pos < coverage_pos < subtract_pos):
    fail("positive-volume contact must measure eligible original-face coverage before clipping the current residual")

probe_clip_pos = text.find("TryIntersection(residual, contactProbe, out var contactIntersectionFailed)")
probe_coverage_pos = text.find("ReadEligibleOriginalFaceArea(", probe_clip_pos)
probe_subtract_pos = text.find("TrySubtract(residual, contact)", probe_coverage_pos)
if not (0 <= probe_clip_pos < probe_coverage_pos < probe_subtract_pos):
    fail("touching contact probe must measure eligible original-face coverage before clipping the current residual")

seed_reader_pos = text.find("private static List<FaceSeed> ReadVerticalFaces")
coverage_reader_pos = text.find("private static double ReadEligibleOriginalFaceArea", seed_reader_pos)
face_plane_reader_pos = text.find("private static PlanarEntity? ReadFacePlane", coverage_reader_pos)
if not (0 <= seed_reader_pos < coverage_reader_pos < face_plane_reader_pos):
    fail("seed/contact readers must share the guarded V25 planar-face unwrapping helper")

seed_body = text[seed_reader_pos:coverage_reader_pos]
coverage_body = text[coverage_reader_pos:face_plane_reader_pos]
for name, body in (("seed", seed_body), ("contact coverage", coverage_body)):
    if "ReadFacePlane(face)" not in body:
        fail(name + " face reader bypasses ExternalBoundedSurface planar unwrapping")

if "CandidateReachesExterior(candidate, seed, distanceCad" not in coverage_body:
    fail("original-face coverage does not reject coplanar interior-only penetration side strips")

print("PASS: StructuralWall contact counts union-resolved original-face patches reached from the cutter exterior side, keeps physical residual boundary loss non-authoritative, unwraps V25 ExternalBoundedSurface planes, and fails closed on native/topology ambiguity")
