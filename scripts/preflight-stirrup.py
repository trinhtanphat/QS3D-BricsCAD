#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Rebar/RectangularStirrupPlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/RectangularStirrupSmoke.cs"

if not planner.exists():
    errors.append("missing RectangularStirrupPlanner.cs")
else:
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "CoverM", "DiameterMm", "BendRadiusM", "MaximumSagittaM",
        "HookLengthM", "HookTailAngleDeg", "BulgeArcTessellator.Tessellate",
        "LinearRebarLayoutPlanner.Plan", "CenterlineLengthM", "TotalCenterlineLengthM",
        "EnsureInside", "RebarShapePath(\"RECT-STIRRUP\""
    ):
        if needle not in text:
            errors.append("stirrup planner invariant missing: " + needle)
    if "HookTailAngleDeg { get; set; } =" in text or "HookLengthM { get; set; } =" in text or "BendRadiusM { get; set; } =" in text:
        errors.append("stirrup bend/hook engineering inputs must not silently acquire standard-specific defaults")

if not smoke.exists():
    errors.append("missing RectangularStirrupSmoke.cs")
else:
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "MiteredClosedTie();", "RoundedTie();", "SymmetricHookTails();",
        "SpacingDistribution();", "CountDistribution();", "RejectsInvalidEnvelope();",
        "RejectsExcessiveBend();", "RejectsHookOutsideEnvelope();", "RejectsAmbiguousDistribution();"
    ):
        if needle not in text:
            errors.append("stirrup smoke coverage missing: " + needle)

print("QS3D stirrup preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: deterministic rectangular stirrup geometry, explicit bend/hook inputs, distribution and edge-case coverage are present.")
