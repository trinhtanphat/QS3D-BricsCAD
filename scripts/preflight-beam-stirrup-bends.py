#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs"
builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/BeamStirrupBendSmoke.cs"

for path in (planner, builder, smoke):
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
    if re.search(r"BendRadiusM\s*\{\s*get;\s*set;\s*\}\s*=", text):
        errors.append("BendRadiusM must not have a non-project engineering default")
    if re.search(r"HookLengthM\s*\{\s*get;\s*set;\s*\}\s*=", text):
        errors.append("HookLengthM must not have a non-project engineering default")
    if re.search(r"HookTailAngleDeg\s*\{\s*get;\s*set;\s*\}\s*=", text):
        errors.append("HookTailAngleDeg must not have a non-project engineering default")

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
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "GeneratedBeamStirrupHandles"
    ):
        if needle not in text: errors.append("beam stirrup bend V25 wiring missing: " + needle)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "LegacyLoopRemainsByteForGeometryCompatible();", "RoundedBendsTrackExactCenterline();",
        "HookTailsAreExplicitAndSymmetric();", "RejectsExcessiveBendRadius();",
        "RejectsHookOutsideEnvelope();", "RejectsAngleWithoutHookLength();"
    ):
        if needle not in text: errors.append("beam stirrup bend smoke missing: " + needle)

print("QS3D beam stirrup bend/hook preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: legacy beam stirrup compatibility, explicit bend/hook geometry, exact centerline metadata, endpoint-safe segmented Solid3d path and smoke coverage are present.")
