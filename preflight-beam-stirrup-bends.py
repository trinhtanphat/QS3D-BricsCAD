#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs"
builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs"
health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs"
invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/BeamStirrupBendSmoke.cs"
health_smoke = ROOT / "tests/QS3D.Core.SmokeTests/BeamStirrupMetadataHealthSmoke.cs"

for path in (planner, builder, health, invalidator, smoke, health_smoke):
    if not path.is_file(): errors.append("missing beam stirrup bend file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "BendRadiusM", "MaximumSagittaM", "HookLengthM", "HookTailAngleDeg",
        "BulgeArcTessellator.Tessellate", "CenterlineLengthM", "PolylineLengthM", "HasHookTails",
        "EnsureInside", "QuarterCircleBulge", "2d * Math.PI * bendRadiusM",
        "bendRadiusM <= 1e-12d && hookLengthM <= 1e-12d"
    ):
        if needle not in text: errors.append("beam stirrup bend planner missing: " + needle)
    if re.search(r"BendRadiusM\s*\{\s*get;\s*set;\s*\}\s*=", text): errors.append("BendRadiusM must not have a non-project engineering default")
    if re.search(r"HookLengthM\s*\{\s*get;\s*set;\s*\}\s*=", text): errors.append("HookLengthM must not have a non-project engineering default")
    if re.search(r"HookTailAngleDeg\s*\{\s*get;\s*set;\s*\}\s*=", text): errors.append("HookTailAngleDeg must not have a non-project engineering default")

if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        '"RebarStirrupBendRadiusM", 0d', '"RebarStirrupHookLengthM", 0d',
        '"RebarStirrupHookTailAngleDeg", 0d', '"RebarStirrupMaximumSagittaM", .001d',
        "BendRadiusM = bendRadiusM", "HookLengthM = hookLengthM", "HookTailAngleDeg = hookTailAngleDeg",
        '"GeneratedBeamStirrupCenterlineLengthM"', '"GeneratedBeamStirrupTotalCenterlineLengthM"',
        '"GeneratedBeamStirrupPolylineLengthM"', '"GeneratedBeamStirrupBendRadiusM"',
        '"GeneratedBeamStirrupHookLengthM"', '"GeneratedBeamStirrupHookTailAngleDeg"',
        '"Beam.Line.RectangularHookedPath"', '"Beam.Line.RectangularRoundedLoop"',
        "var closed = loop[0].DistanceTo(loop[loop.Count - 1]) <= 1e-12d;",
        "var before = closed || index > 1 ? overlap : 0d;",
        "var after = closed || index < loop.Count - 1 ? overlap : 0d;",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "GeneratedBeamStirrupHandles",
        "ClearGeneratedBeamStirrupStale()"
    ):
        if needle not in text: errors.append("beam stirrup bend V25 wiring missing: " + needle)

if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in (
        "GeneratedBeamStirrupCenterlineLengthM", "GeneratedBeamStirrupTotalCenterlineLengthM",
        "GeneratedBeamStirrupPolylineLengthM", "GeneratedBeamStirrupBendRadiusM",
        "GeneratedBeamStirrupHookLengthM", "GeneratedBeamStirrupHookTailAngleDeg",
        "Beam.Line.RectangularClosedLoop", "Beam.Line.RectangularRoundedLoop", "Beam.Line.RectangularHookedPath",
        "BEAM_STIRRUP_GENERATED_LENGTH_MISMATCH", "BEAM_STIRRUP_GENERATED_MODE_MISMATCH",
        "BEAM_STIRRUP_GENERATED_MODE_INVALID", "advanced stirrup metadata",
        "Old generated snapshots predate bend/hook length metadata", "GeneratedHandleOwnershipPolicy.IsOwnerSlot"
    ):
        if needle not in text: errors.append("beam stirrup advanced health missing: " + needle)

if invalidator.is_file():
    text = invalidator.read_text(encoding="utf-8")
    prefix_cleanup = (
        "CoreOwnershipPolicy.RebarHandleKeys" in text
        and "MetadataPrefixForHandleKey" in text
        and "RemoveByPrefix(element, MetadataPrefixForHandleKey(key))" in text
    )
    if not prefix_cleanup:
        for key in (
            "GeneratedBeamStirrupCenterlineLengthM", "GeneratedBeamStirrupTotalCenterlineLengthM",
            "GeneratedBeamStirrupPolylineLengthM", "GeneratedBeamStirrupBendRadiusM",
            "GeneratedBeamStirrupHookLengthM", "GeneratedBeamStirrupHookTailAngleDeg"
        ):
            if 'Remove(element, "' + key + '")' not in text:
                errors.append("beam stirrup invalidation does not clear: " + key)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "LegacyLoopRemainsByteForGeometryCompatible();", "RoundedBendsTrackExactCenterline();",
        "HookTailsAreExplicitAndSymmetric();", "RejectsExcessiveBendRadius();",
        "RejectsHookOutsideEnvelope();", "RejectsAngleWithoutHookLength();"
    ):
        if needle not in text: errors.append("beam stirrup bend smoke missing: " + needle)

if health_smoke.is_file():
    text = health_smoke.read_text(encoding="utf-8")
    for needle in (
        "LegacySnapshotRemainsCompatible();", "AdvancedSnapshotIsAccepted();",
        "LengthMismatchIsReported();", "HookModeMismatchIsReported();", "MissingAdvancedModeIsReported();"
    ):
        if needle not in text: errors.append("beam stirrup metadata health smoke missing: " + needle)

print("QS3D beam stirrup bend/hook lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: legacy compatibility, explicit bend/hook geometry, exact length metadata, endpoint-safe V25 path, health consistency and prefix-safe invalidation cleanup are present.")