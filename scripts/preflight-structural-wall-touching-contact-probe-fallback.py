#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/StructuralWallConcreteContactService.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall touching-contact probe fallback preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


source = SERVICE.read_text(encoding="utf-8")

required = (
    "var directIntersectionFailed = false;",
    "directIntersectionFailed = intersectionFailed;",
    "if (!intersectionFailed && overlap != null && SafeVolumeCad(overlap) > volumeCadTolerance)",
    "using (var contactProbe = Clone(candidate))",
    "if (!TryOffset(contactProbe, distanceCad))",
    "using (var contact = TryIntersection(residual, contactProbe, out var contactIntersectionFailed))",
    "var probeContactAreaCad = ReadEligibleOriginalFaceArea(",
    "if (!TrySubtract(residual, contact))",
    "diagnostics.ContactProbeCutCount++;",
    "if (diagnostics.FailedNativeCutCount > 0) return false;",
)
for token in required:
    if token not in source:
        fail("missing touching fallback contract token: " + token)

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
    "if (probeFaceReadFailed)",
    "if (probeContactAreaCad <= areaCadTolerance)",
    "if (!TrySubtract(residual, contact))",
):
    if unresolved_token not in probe_block:
        fail("touching probe lost fail-closed stage: " + unresolved_token)

success_tail = source[source.index("if (!TrySubtract(residual, contact))", probe):probe_end]
if "contactAreaCad += probeContactAreaCad;" not in success_tail:
    fail("successful touching probe no longer publishes only eligible original-face area")
if "diagnostics.FailedNativeCutCount++;" not in success_tail:
    fail("touching probe subtract failure is no longer fail-closed")

print("PASS: zero-volume direct-intersection failure is deferred to the native touching probe, unresolved probes remain fail-closed, and successful probe subtraction retains original-face area authority")
