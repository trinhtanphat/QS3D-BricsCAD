#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/StructuralWallConcreteContactService.cs"
PROBE = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallContactProbeCommands.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall touching-contact probe fallback preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


source = SERVICE.read_text(encoding="utf-8")
probe_source = PROBE.read_text(encoding="utf-8")

required = (
    "var directIntersectionFailed = false;",
    "directIntersectionFailed = intersectionFailed;",
    "if (!intersectionFailed && overlap != null && SafeVolumeCad(overlap) > volumeCadTolerance)",
    "var contactProbeDistanceCad = Math.Max(distanceCad, 1e-5d / lengthToMeter);",
    "using (var contactProbe = Clone(candidate))",
    "if (!TryOffset(contactProbe, contactProbeDistanceCad))",
    "using (var contact = TryIntersection(residual, contactProbe, out var contactIntersectionFailed))",
    "var touchingSeeds = ReadEligibleTouchingFaceSeeds(",
    "foreach (var touchingSeed in touchingSeeds)",
    "using (var footprintContact = TryCreateFootprintContact(",
    "var footprintContactAreaCad = ReadEligibleOriginalFaceArea(",
    "if (!TrySubtract(residual, footprintContact))",
    "contactAreaCad += footprintContactAreaCad;",
    "diagnostics.ContactProbeCutCount++;",
    "if (diagnostics.FailedNativeCutCount > 0) return false;",
)
for token in required:
    if token not in source:
        fail("missing touching fallback/original-footprint contract token: " + token)

if "TryOffset(contactProbe, distanceCad)" in source:
    fail("native touching probe still uses the sub-modeler quantity tolerance directly")
if "SamePlane(x.Plane, plane, distanceCad)" not in source:
    fail("original-face plane identity no longer uses the tighter quantity tolerance")
if "CandidateReachesExterior(candidate, seed, distanceCad" not in source:
    fail("exterior-side eligibility was widened to the native probe distance")
if "var probeContactAreaCad = ReadEligibleOriginalFaceArea(" in source:
    fail("expanded OffsetBody region regained authority over touching deduction area")
if "TrySubtract(residual, contact)" in source:
    fail("expanded OffsetBody intersection regained authority over residual subtraction")

old_terminal = """if (intersectionFailed)\n                                {\n                                    diagnostics.FailedNativeCutCount++;\n                                    continue;\n                                }"""
if old_terminal in source:
    fail("preliminary zero-volume intersection failure still terminates before the touching probe")

preliminary = source.index("using (var overlap = TryIntersection(residual, candidate, out var intersectionFailed))")
probe = source.index("using (var contactProbe = Clone(candidate))", preliminary)
if preliminary >= probe:
    fail("touching probe no longer follows the preliminary direct intersection")

probe_end = source.index("diagnostics.ContactProbeCutCount++;", probe)
probe_block = source[probe:probe_end]
if probe_block.count("if (directIntersectionFailed) diagnostics.FailedNativeCutCount++;") < 2:
    fail("an unresolved deferred direct-intersection failure can be published as false no-contact")

for unresolved_token in (
    "if (contactIntersectionFailed)",
    "if (contact == null || SafeVolumeCad(contact) <= volumeCadTolerance)",
    "if (touchingSeedReadFailed)",
    "if (touchingSeeds.Count == 0)",
    "if (footprintIntersectionFailed)",
    "if (footprintContact == null || SafeVolumeCad(footprintContact) <= volumeCadTolerance)",
    "if (footprintFaceReadFailed)",
    "if (footprintContactAreaCad <= areaCadTolerance)",
    "if (!TrySubtract(residual, footprintContact))",
):
    if unresolved_token not in probe_block:
        fail("touching probe lost fail-closed/original-footprint stage: " + unresolved_token)

footprint = source.index("using (var footprintContact = TryCreateFootprintContact(", probe)
footprint_area = source.index("var footprintContactAreaCad = ReadEligibleOriginalFaceArea(", footprint)
footprint_subtract = source.index("if (!TrySubtract(residual, footprintContact))", footprint_area)
footprint_publish = source.index("contactAreaCad += footprintContactAreaCad;", footprint_subtract)
if not (probe < footprint < footprint_area < footprint_subtract < footprint_publish < probe_end):
    fail("touching fallback must discover topology first, then measure/subtract/publish only the original candidate footprint")

success_tail = source[footprint_subtract:probe_end]
if "contactAreaCad += footprintContactAreaCad;" not in success_tail:
    fail("successful touching probe no longer publishes only authoritative original-footprint area")
if "diagnostics.FailedNativeCutCount++;" not in success_tail:
    fail("touching footprint subtract failure is no longer fail-closed")

footprint_reader = source.index("private static Solid3d? TryCreateFootprintContact")
face_plane_reader = source.index("private static PlanarEntity? ReadFacePlane", footprint_reader)
footprint_body = source[footprint_reader:face_plane_reader]
for token in (
    "seed.InteriorSide * contactProbeDistanceCad",
    "footprintProbe.TransformBy(Matrix3d.Displacement(displacement));",
    "return TryIntersection(residual, footprintProbe, out failed);",
):
    if token not in footprint_body:
        fail("original touching footprint reconstruction lost normal-translation contract: " + token)

stage_contract = (
    ("DirectIntersectionFailureCount", "diagnostics.DirectIntersectionFailureCount++;", "direct_fail="),
    ("ContactProbeOffsetFailureCount", "diagnostics.ContactProbeOffsetFailureCount++;", "probe_offset_fail="),
    ("ContactProbeIntersectionFailureCount", "diagnostics.ContactProbeIntersectionFailureCount++;", "probe_intersect_fail="),
    ("ContactProbeEmptyRegionCount", "diagnostics.ContactProbeEmptyRegionCount++;", "probe_empty="),
    ("ContactProbeFaceReadFailureCount", "diagnostics.ContactProbeFaceReadFailureCount++;", "probe_face_fail="),
    ("ContactProbeNoEligibleFaceCount", "diagnostics.ContactProbeNoEligibleFaceCount++;", "probe_no_face="),
    ("ContactProbeSubtractFailureCount", "diagnostics.ContactProbeSubtractFailureCount++;", "probe_subtract_fail="),
)
for property_name, increment, output_token in stage_contract:
    if "public int " + property_name + " { get; internal set; }" not in source:
        fail("missing bounded stage diagnostic property: " + property_name)
    if increment not in source:
        fail("missing bounded stage diagnostic increment: " + increment)
    if output_token not in probe_source:
        fail("probe output missing bounded stage token: " + output_token)

for forbidden in ("error.Message", "error.StackTrace", "Handle.ToString()", "Environment.CurrentDirectory"):
    if forbidden in "\n".join(line for line in probe_source.splitlines() if "direct_fail=" in line or "probe_" in line):
        fail("bounded stage diagnostics expose unstable or sensitive detail: " + forbidden)

print("PASS: touching contact uses a unit-aware 10-micrometre native offset only for topology discovery, reconstructs authoritative area/subtraction from the original candidate footprint, keeps deferred failures fail-closed, and preserves bounded native-stage diagnostics")
