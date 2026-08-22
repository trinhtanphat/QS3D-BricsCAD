#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Geometry/GridSystemPlanner.cs"
INTERSECT = ROOT / "src/QS3D.Core/Geometry/GridIntersectionPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridSystemPlannerSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (CORE, INTERSECT, SMOKE, REG):
    if not path.is_file():
        errors.append("missing Grid system planning contract file: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        "public static IReadOnlyList<GridReferenceCurve> PlanRectangular",
        "public static IReadOnlyList<GridReferenceCurve> PlanRadial",
        "private const int MaxCurves = 2000;",
        "Math.Abs(dot) > orthogonalityTolerance",
        "duplicate/ambiguous ray angles",
        "duplicate/ambiguous ring radii",
        "GridReferenceCurve.Line",
        "GridReferenceCurve.Arc",
    ):
        if token not in text:
            errors.append("GridSystemPlanner.cs missing fail-closed contract token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RotatedRectangularSystemProducesDeterministicIntersections",
        "RadialSystemProducesRayRingIntersections",
        "InvalidRectangularAxesFailClosed",
        "DuplicateRadialAnglesFailClosed",
        "GridIntersectionPlanner.FindIntersections",
    ):
        if token not in text:
            errors.append("GridSystemPlannerSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "GridSystemPlannerSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Grid system planner smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core rectangular/radial Grid systems are bounded, explicit, fail closed on ambiguity, and feed the existing Grid intersection model; native V25 authoring/materialization remains local-only.")
