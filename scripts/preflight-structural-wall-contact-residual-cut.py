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
)
for token in required:
    if token not in text:
        fail("missing clipped-residual/diagnostic contract: " + token)

for forbidden in (
    "TrySubtract(residual, candidate);",
    "TrySubtract(residual, contactProbe);",
    "using (var intersection = TryIntersection(target, candidate))",
    "face.Surface as PlanarEntity",
    "external.BaseSurface as PlanarEntity",
):
    if forbidden in text:
        fail("stale whole-candidate/direct-face-surface path remains: " + forbidden)

clip_pos = text.find("TryIntersection(residual, candidate, out var intersectionFailed)")
subtract_pos = text.find("TrySubtract(residual, overlap)", clip_pos)
residual_measure_pos = text.find("ReadResidualAreaOnOriginalVerticalFaces", subtract_pos)
if not (0 <= clip_pos < subtract_pos < residual_measure_pos):
    fail("positive-volume contact must clip against current residual before residual face-area measurement")

seed_reader_pos = text.find("private static List<FaceSeed> ReadVerticalFaces")
face_plane_reader_pos = text.find("private static PlanarEntity? ReadFacePlane", seed_reader_pos)
residual_reader_pos = text.find("private static double ReadResidualAreaOnOriginalVerticalFaces", seed_reader_pos)
if not (0 <= seed_reader_pos < residual_reader_pos < face_plane_reader_pos):
    fail("vertical seed and residual readers must share the guarded V25 planar-face unwrapping helper")

seed_body = text[seed_reader_pos:residual_reader_pos]
residual_body = text[residual_reader_pos:face_plane_reader_pos]
for name, body in (("seed", seed_body), ("residual", residual_body)):
    if "ReadFacePlane(face)" not in body:
        fail(name + " face reader bypasses ExternalBoundedSurface planar unwrapping")

print("PASS: StructuralWall contact uses clipped current-residual cuts, unwraps V25 ExternalBoundedSurface planes without direct PlanarEntity casts, exposes bounded diagnostics, and fails closed on confirmed native cut failure")
