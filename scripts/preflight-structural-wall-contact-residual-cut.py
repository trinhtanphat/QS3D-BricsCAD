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
)
for token in required:
    if token not in text:
        fail("missing clipped-residual/diagnostic contract: " + token)

for forbidden in (
    "TrySubtract(residual, candidate);",
    "TrySubtract(residual, contactProbe);",
    "using (var intersection = TryIntersection(target, candidate))",
):
    if forbidden in text:
        fail("stale whole-candidate subtraction path remains: " + forbidden)

clip_pos = text.find("TryIntersection(residual, candidate, out var intersectionFailed)")
subtract_pos = text.find("TrySubtract(residual, overlap)", clip_pos)
residual_measure_pos = text.find("ReadResidualAreaOnOriginalVerticalFaces", subtract_pos)
if not (0 <= clip_pos < subtract_pos < residual_measure_pos):
    fail("positive-volume contact must clip against current residual before residual face-area measurement")

print("PASS: StructuralWall contact uses clipped current-residual cuts, exposes bounded diagnostics, and fails closed on confirmed native cut failure")
