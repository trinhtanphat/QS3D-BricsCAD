#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Rebar/RectangularStirrupPlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/RectangularStirrupSmoke.cs"
builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/StirrupRebarSolidBuilder.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/StirrupRebarGeometryCommands.cs"
ownership = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"

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

if not builder.exists():
    errors.append("missing V25 StirrupRebarSolidBuilder.cs")
else:
    text = builder.read_text(encoding="utf-8")
    for needle in (
        'HandlesKey = "GeneratedStirrupRebarHandles"', "RectangularStirrupPlanner.PlanSet",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "as Solid3d",
        "MaxTiesPerElement", "MaxTiesPerBatch", "ElementCategory.Beam", "ElementCategory.Column",
        'RequiredText(element, family, "StirrupNotation")', 'RequiredNonNegative(element, family, "RebarCoverM")',
        'RequiredNonNegative(element, family, "StirrupEndCoverM")', 'RequiredNonNegative(element, family, "StirrupBendRadiusM")',
        'RequiredNonNegative(element, family, "StirrupHookLengthM")', 'RequiredFinite(element, family, "StirrupHookTailAngleDeg")',
        '"RectangularStirrup.SegmentedCylinder"', '"geometry.rebar.stirrup"'
    ):
        if needle not in text:
            errors.append("V25 stirrup adapter safety/wiring missing: " + needle)
    if re.search(r'CadGeometryGuard\.Number\([^\n]*"(?:RebarCoverM|StirrupEndCoverM|StirrupBendRadiusM|StirrupHookLengthM|StirrupHookTailAngleDeg)"', text):
        errors.append("engineering stirrup inputs must not silently use CadGeometryGuard.Number fallbacks")

if not command.exists():
    errors.append("missing StirrupRebarGeometryCommands.cs")
else:
    text = command.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DREBAR3DSTIRRUP", CommandFlags.UsePickSet)]' not in text:
        errors.append("QS3DREBAR3DSTIRRUP command registration missing")
    if "StirrupRebarSolidBuilder.BuildSelected" not in text:
        errors.append("QS3DREBAR3DSTIRRUP does not invoke the stirrup builder")

if not ownership.exists():
    errors.append("generated rebar ownership guard missing")
elif 'Add(element, "GeneratedStirrupRebarHandles", owners);' not in ownership.read_text(encoding="utf-8"):
    errors.append("stirrup generated handles are not reserved in ownership guard")

print("QS3D stirrup preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: deterministic stirrup planning, explicit engineering inputs, ownership-safe V25 adapter, destructive-erase guards, batch limits and regression coverage are present.")
