#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src/QS3D.Core/Geometry/GridIntersectionPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/GRID-INTERSECTIONS.md"
errors = []

for path in (PLANNER, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing Grid intersection contract file: " + str(path.relative_to(ROOT)))

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        "private const int MaxCurves = 2000",
        "private const int MaxIntersections = 100000",
        "GridReferenceCurveKind.Line",
        "GridReferenceCurveKind.Arc",
        "NormalizeElementId(elementId)",
        "EnsureFinitePoint(point, \"Grid intersection result\")",
        "EnsureFiniteDerived(\"Grid LINE/ARC quadratic\"",
        "Grid intersection cross product exceeds the supported numeric range",
        "IntersectLines(first, second, tolerance)",
        "IntersectLineArc(first, second, tolerance)",
        "IntersectArcs(first, second, tolerance)",
        "Grid intersection input contains duplicate element id",
        "collinear/overlapping LINE references do not define one unique Grid intersection",
        "coincident ARC support circles are intentionally rejected",
        "Grid ARC sweep must be in (0, 2π]",
    ):
        if token not in text:
            errors.append("GridIntersectionPlanner.cs missing bounded/fail-closed token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CrossingLinesProduceOneDeterministicPoint",
        "EndpointTouchIsAcceptedButOverlapFailsClosed",
        "LineArcRespectsArcSweep",
        "ArcArcProducesTwoPointsWhenBothSweepsContainThem",
        "CoincidentArcSupportFailsClosed",
        "DuplicateElementIdsFailClosed",
        "ElementIdsAreCanonicalizedBeforeDuplicateCheck",
        "OverflowingDerivedGeometryFailsClosed",
    ):
        if token not in text:
            errors.append("GridIntersectionPlannerSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "GridIntersectionPlannerSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Grid intersection smoke exists but is not registered in RunAll()")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "GridIntersectionPlanner",
        "explicit positive counter-clockwise sweep",
        "collinear LINE references with a non-zero overlap",
        "coincident ARC support circles",
        "does not complete Grid constraints",
        "V25 LINE/ARC → `GridReferenceCurve` extraction",
    ):
        if token not in text:
            errors.append("GRID-INTERSECTIONS.md missing geometry/runtime boundary: " + token)

print("QS3D Grid intersection planner preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: finite LINE/ARC Grid intersections are bounded, semantic IDs are canonicalized, derived numeric overflow fails closed, smoke coverage is registered, and native extraction/ordering/constraints/visualization remain explicit V25 gates.")
