#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "StructuralWallConcreteContactService.cs"
CAPTURE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"
CORE = ROOT / "src" / "QS3D.Core" / "Services" / "StructuralRegenerator.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"
QUALIFICATION = ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681QualificationCommands.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall contact rule-matrix preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(label + " is missing")


contact = CONTACT.read_text(encoding="utf-8")
capture = CAPTURE.read_text(encoding="utf-8")
core = CORE.read_text(encoding="utf-8")
v26 = V26.read_text(encoding="utf-8")
qualification = QUALIFICATION.read_text(encoding="utf-8")

# Rule 1/2 concrete-neighbour scope: all currently supported concrete structural hosts.
for category in (
    "ElementCategory.StructuralWall",
    "ElementCategory.Beam",
    "ElementCategory.Slab",
    "ElementCategory.Column",
    "ElementCategory.Foundation",
    "ElementCategory.Stair",
):
    require(contact, category, "concrete-contact category " + category)

# Bounding boxes may reject distant objects only. Formwork deduction must remain native
# Solid3d/BREP geometry. The current residual union-resolves overlapping cutters, while the
# authoritative area is coverage on original target-face planes reached from the cutter exterior.
# This distinction prevents a penetrating cutter from turning newly exposed side strips into
# false formwork contact (#3697).
require(
    contact,
    "if (!BoundingBoxesMayOverlap(target, candidate, distanceCad)) continue;",
    "bounding-box broad phase",
)
require(
    contact,
    "TryIntersection(residual, candidate, out var intersectionFailed)",
    "residual-clipped native volume/contact intersection",
)
require(
    contact,
    "ReadEligibleOriginalFaceArea(",
    "original-face native contact coverage",
)
require(
    contact,
    "CandidateReachesExterior(",
    "exact cutter exterior-side classification",
)
require(
    contact,
    "TryReadSolidVertexSideRange(",
    "native BREP vertex side range",
)
require(
    contact,
    "TrySubtract(residual, overlap)",
    "clipped volume-overlap residual subtraction",
)
require(
    contact,
    "var contactProbeDistanceCad = Math.Max(distanceCad, 1e-5d / lengthToMeter);",
    "unit-aware native touching-probe distance floor",
)
require(
    contact,
    "TryOffset(contactProbe, contactProbeDistanceCad)",
    "zero-volume face-contact BREP topology probe",
)
require(
    contact,
    "TryIntersection(residual, contactProbe, out var contactIntersectionFailed)",
    "residual-clipped contact-probe topology intersection",
)

# #3770: the positive OffsetBody probe may establish touching topology, but it must never become
# authoritative contact footprint. The expanded probe grows finite/partial patches tangentially
# (for the licensed matrix, 200 x 400 mm becomes 0.080004 m2). Resolve which original target-face
# seeds are touched, then clip/subtract a translated clone of the original candidate so its
# tangential footprint is preserved exactly and overlapping neighbours remain union-resolved.
require(
    contact,
    "ReadEligibleTouchingFaceSeeds(",
    "touching topology to original-face seed classification",
)
require(
    contact,
    "TryCreateFootprintContact(",
    "original-candidate footprint-preserving touching cutter",
)
require(
    contact,
    "TrySubtract(residual, footprintContact)",
    "footprint-clipped face-contact residual subtraction",
)
for forbidden in (
    "TrySubtract(residual, contact)",
    "contactAreaCad += probeContactAreaCad",
):
    if forbidden in contact:
        fail("expanded touching probe became authoritative contact geometry again: " + forbidden)

# Deterministic matrix contract: exact finite touching footprints stay exact. Do not hide the
# licensed #3770 delta by loosening the matrix tolerance.
for token, label in (
    ("private const double ToleranceM2 = 1e-6d;", "strict local matrix tolerance"),
    ("private const double ExpectedOneEndM2 = 0.1600d;", "full touching contact expectation"),
    ("private const double ExpectedPartialM2 = 0.0800d;", "partial touching contact expectation"),
):
    require(qualification, token, label)

for forbidden in (
    "TryIntersection(target, candidate",
    "TrySubtract(residual, candidate)",
    "TrySubtract(residual, contactProbe)",
    "TryOffset(contactProbe, distanceCad)",
    "grossVerticalAreaCad - residualVerticalAreaCad",
    "ReadResidualAreaOnOriginalVerticalFaces",
):
    if forbidden in contact:
        fail("stale non-residual-clipped/residual-area contact authority reappeared: " + forbidden)
require(
    contact,
    "if (diagnostics.FailedNativeCutCount > 0) return false;",
    "failed native-cut fail-closed measurement guard",
)
require(
    contact,
    "if (Math.Abs(normal.Z) >= HorizontalFaceNormalZ) continue;",
    "vertical-face-only filter excluding top/bottom",
)
require(
    contact,
    "deductionM2 = Math.Min(grossVerticalAreaCad, contactAreaCad) * areaScale;",
    "union-resolved original-face contact deduction",
)
require(
    contact,
    "diagnostics.ResidualVerticalAreaM2 = Math.Max(0d, grossVerticalAreaCad - contactAreaCad) * areaScale;",
    "logical residual/net contact diagnostic",
)
require(
    contact,
    'wall.Category != ElementCategory.StructuralWall',
    "StructuralWall-only target guard",
)

# Contact evidence must be refreshed after every concrete semantic capture and stale live-BREP
# evidence must be cleared rather than reusing an obsolete deduction.
require(capture, "StructuralWallConcreteContactService.IsConcreteContactCategory(category)", "concrete capture refresh gate")
if capture.count("RefreshStructuralWallConcreteContacts(document, project);") < 2:
    fail("both batch and single-snapshot capture paths must refresh StructuralWall concrete contacts")
require(capture, 'wall.Properties["ConcreteContactAreaM2"] = encoded;', "contact-area publication")
require(capture, 'wall.Properties.Remove("ConcreteContactAreaM2")', "stale contact-area clearing")

# Core Rule 1/2 audit contract and opening reveal semantics.
for quantity_key in (
    '"GrossFormworkM2"',
    '"ConcreteContactDeductionM2"',
    '"OpeningRevealFormworkAdjustmentM2"',
    '"FormworkM2"',
):
    require(core, quantity_key, "wall formwork audit quantity " + quantity_key)
require(core, 'SemanticNumber.Get(element, "ConcreteContactAreaM2")', "Core contact deduction input")
require(core, 'opening.Properties.ContainsKey("SillOffsetMm")', "sill/window bottom-reveal rule")
require(core, 'QuantityMath.Add(revealLength, width', "four-side sill/window reveal")

# V26 intentionally compiles the shared V25 adapter source. This catches accidental host-major
# omission of the contact service without duplicating a second implementation.
require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared V25 source include")
for forbidden in (
    "..\\QS3D.BricsCAD.V25\\Reporting\\**\\*.cs",
    "StructuralWallConcreteContactService.cs",
):
    exclude_pos = v26.find("Exclude=")
    if exclude_pos >= 0:
        exclude_end = v26.find(">", exclude_pos)
        exclude_text = v26[exclude_pos:exclude_end if exclude_end >= 0 else len(v26)]
        if forbidden in exclude_text:
            fail("V26 shared-source Exclude must not omit " + forbidden)

print("PASS: StructuralWall Rule 1/2 contact uses native original-face exterior coverage with residual union, preserves the topology-only touching probe and original candidate footprint, exact partial/full matrix values, opening/capture semantics, and V26 shared-source coverage")
