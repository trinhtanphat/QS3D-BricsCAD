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
    "ReadEligibleTouchingFaceSeeds(",
    "TryCreateFootprintContact(",
    "TrySubtract(residual, footprintContact)",
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
    # #3770: the expanded positive probe is topology evidence only. Authoritative touching
    # area/subtraction must come from a normal-translated clone of the original candidate.
    "var touchingSeeds = ReadEligibleTouchingFaceSeeds(",
    "foreach (var touchingSeed in touchingSeeds)",
    "using (var footprintContact = TryCreateFootprintContact(",
    "var footprintContactAreaCad = ReadEligibleOriginalFaceArea(",
    "contactAreaCad += footprintContactAreaCad;",
    "deductionM2 = Math.Min(grossVerticalAreaCad, contactAreaCad) * areaScale;",
    "diagnostics.ResidualVerticalAreaM2 = Math.Max(0d, grossVerticalAreaCad - contactAreaCad) * areaScale;",
)
for token in required:
    if token not in text:
        fail("missing clipped-residual/original-footprint-contact contract: " + token)

for forbidden in (
    "TrySubtract(residual, candidate);",
    "TrySubtract(residual, contactProbe);",
    "TrySubtract(residual, contact)",
    "using (var intersection = TryIntersection(target, candidate))",
    "face.Surface as PlanarEntity",
    "external.BaseSurface as PlanarEntity",
    # #3697 regression: a penetrating cutter creates new side-strip residual boundaries.
    # Gross-minus-residual therefore over-deducts and must never become contact authority again.
    "var deductionCad = Math.Max(0d, grossVerticalAreaCad - residualVerticalAreaCad);",
    "ReadResidualAreaOnOriginalVerticalFaces",
    # #3770 regression: measuring the OffsetBody region directly grows tangential edges by the
    # native probe distance and changes a finite partial contact footprint.
    "var probeContactAreaCad = ReadEligibleOriginalFaceArea(",
):
    if forbidden in text:
        fail("stale whole-candidate/expanded-probe/residual-area authority remains: " + forbidden)

clip_pos = text.find("TryIntersection(residual, candidate, out var intersectionFailed)")
coverage_pos = text.find("ReadEligibleOriginalFaceArea(", clip_pos)
subtract_pos = text.find("TrySubtract(residual, overlap)", coverage_pos)
if not (0 <= clip_pos < coverage_pos < subtract_pos):
    fail("positive-volume contact must measure eligible original-face coverage before clipping the current residual")

probe_clip_pos = text.find("TryIntersection(residual, contactProbe, out var contactIntersectionFailed)")
touching_seed_pos = text.find("ReadEligibleTouchingFaceSeeds(", probe_clip_pos)
footprint_pos = text.find("TryCreateFootprintContact(", touching_seed_pos)
footprint_coverage_pos = text.find("ReadEligibleOriginalFaceArea(", footprint_pos)
probe_subtract_pos = text.find("TrySubtract(residual, footprintContact)", footprint_coverage_pos)
if not (0 <= probe_clip_pos < touching_seed_pos < footprint_pos < footprint_coverage_pos < probe_subtract_pos):
    fail("touching probe must discover eligible original-face seeds, reconstruct the original candidate footprint, measure it, then clip the residual")

seed_reader_pos = text.find("private static List<FaceSeed> ReadVerticalFaces")
coverage_reader_pos = text.find("private static double ReadEligibleOriginalFaceArea", seed_reader_pos)
touching_reader_pos = text.find("private static IReadOnlyList<FaceSeed> ReadEligibleTouchingFaceSeeds", coverage_reader_pos)
footprint_reader_pos = text.find("private static Solid3d? TryCreateFootprintContact", touching_reader_pos)
face_plane_reader_pos = text.find("private static PlanarEntity? ReadFacePlane", footprint_reader_pos)
if not (0 <= seed_reader_pos < coverage_reader_pos < touching_reader_pos < footprint_reader_pos < face_plane_reader_pos):
    fail("seed/contact/footprint readers must share the guarded V25 planar-face unwrapping helper")

seed_body = text[seed_reader_pos:coverage_reader_pos]
coverage_body = text[coverage_reader_pos:touching_reader_pos]
touching_body = text[touching_reader_pos:footprint_reader_pos]
footprint_body = text[footprint_reader_pos:face_plane_reader_pos]
for name, body in (("seed", seed_body), ("contact coverage", coverage_body), ("touching seed", touching_body)):
    if "ReadFacePlane(face)" not in body:
        fail(name + " face reader bypasses ExternalBoundedSurface planar unwrapping")

for name, body in (("contact coverage", coverage_body), ("touching seed", touching_body)):
    if "CandidateReachesExterior(candidate, seed, distanceCad" not in body:
        fail(name + " reader does not reject coplanar interior-only penetration side strips")

for token in (
    "seed.InteriorSide * contactProbeDistanceCad",
    "footprintProbe.TransformBy(Matrix3d.Displacement(displacement));",
    "return TryIntersection(residual, footprintProbe, out failed);",
):
    if token not in footprint_body:
        fail("touching footprint reconstruction lost original-candidate normal translation contract: " + token)

print("PASS: StructuralWall contact counts union-resolved original-face patches, uses the expanded touching probe only for topology discovery, reconstructs authoritative touching area from the original candidate footprint, keeps physical residual boundary loss non-authoritative, unwraps V25 ExternalBoundedSurface planes, and fails closed on native/topology ambiguity")
