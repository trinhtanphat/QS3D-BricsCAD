#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridSpatialOrderingSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/GRID-SPATIAL-ORDERING.md"
errors = []

for path in (PLANNER, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing Grid spatial ordering contract file: " + str(path.relative_to(ROOT)))

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    required = (
        "public static IReadOnlyList<GridSpatialOrderingEntry> OrderParallelLines(",
        "public static IReadOnlyList<GridReviewedOrderingEntry> OrderReviewedSet(",
        "public enum GridReviewedGroupPrecedence",
        "LinesThenArcs",
        "ArcsThenLines",
        "private const int MaxCurves = 2000",
        "GridReferenceCurveKind.Line",
        "GridReferenceCurveKind.Arc",
        "Grid alignment tolerance must be finite and in (0, 1)",
        "Grid ordering axis must be finite and non-zero",
        "Grid spatial ordering input contains duplicate element id",
        "currently supports parallel LINE references only",
        "Grid reviewed ordering input contains duplicate element id",
        "explicit reviewed radial center",
        "same radius within tolerance",
        "is not perpendicular to the explicit ordering axis within tolerance",
        "project to the same ordering coordinate within tolerance",
        "if (descending) entries.Reverse();",
        "if (descendingArcs) arcEntries.Reverse();",
    )
    for token in required:
        if token not in text:
            errors.append("GridSpatialOrderingPlanner.cs missing bounded/fail-closed token: " + token)

    if "OrderParallelLines(lines, lineOrderingAxis, descendingLines, alignmentTolerance, coordinateTolerance)" not in text:
        errors.append("reviewed mixed ordering must reuse the canonical parallel-LINE planner")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ParallelLinesOrderByExplicitAxis",
        "DescendingOrderIsDeterministic",
        "NonParallelLineFailsClosed",
        "ArcOrderingRequiresSeparatePolicy",
        "DuplicateIdsFailClosed",
        "AmbiguousProjectedCoordinateFailsClosed",
        "InvalidAxisFailsClosed",
        "ReviewedMixedOrderIsPermutationInvariant",
        "ReviewedGroupPrecedenceIsExplicit",
        "ReviewedArcCenterMismatchFailsClosed",
        "ReviewedArcRadiusTieFailsClosed",
        "ReviewedCrossKindDuplicateIdFailsClosed",
        "ReviewedInvalidArcSweepFailsClosed",
    ):
        if token not in text:
            errors.append("GridSpatialOrderingSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "GridSpatialOrderingSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Grid spatial ordering smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "parallel LINE Grid family",
        "explicit non-zero 2D ordering axis",
        "project to the same coordinate within tolerance",
        "reviewed mixed LINE + ARC ordering",
        "explicit reviewed radial center",
        "LinesThenArcs",
        "ArcsThenLines",
        "selection order does not define output order",
        "Core spatial-order planning",
        "PENDING_LOCAL",
    ):
        if token not in text:
            errors.append("GRID-SPATIAL-ORDERING.md missing ordering/runtime boundary: " + token)

print("QS3D Grid spatial ordering preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: parallel LINE ordering remains compatible and reviewed mixed LINE/ARC ordering is explicit-policy, permutation-invariant, bounded and fail-closed.")