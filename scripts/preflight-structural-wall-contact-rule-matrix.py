#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "StructuralWallConcreteContactService.cs"
CAPTURE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"
CORE = ROOT / "src" / "QS3D.Core" / "Services" / "StructuralRegenerator.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


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
require(contact, "TryOffset(contactProbe, distanceCad)", "zero-volume face-contact BREP probe")
require(
    contact,
    "TryIntersection(residual, contactProbe, out var contactIntersectionFailed)",
    "residual-clipped contact-probe intersection",
)
require(
    contact,
    "TrySubtract(residual, contact)",
    "clipped face-contact residual subtraction",
)
for forbidden in (
    "TryIntersection(target, candidate",
    "TrySubtract(residual, candidate)",
    "TrySubtract(residual, contactProbe)",
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

print("PASS: StructuralWall Rule 1/2 contact uses native original-face exterior coverage with residual union, preserves opening/capture semantics, and retains V26 shared-source coverage")
