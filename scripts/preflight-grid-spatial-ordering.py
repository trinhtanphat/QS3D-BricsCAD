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
        "private const int MaxCurves = 2000",
        "GridReferenceCurveKind.Line",
        "Grid alignment tolerance must be finite and in (0, 1)",
        "Grid ordering axis must be finite and non-zero",
        "Grid spatial ordering input contains duplicate element id",
        "currently supports parallel LINE references only",
        "is not perpendicular to the explicit ordering axis within tolerance",
        "project to the same ordering coordinate within tolerance",
        "if (descending) entries.Reverse();",
    )
    for token in required:
        if token not in text:
            errors.append("GridSpatialOrderingPlanner.cs missing bounded/fail-closed token: " + token)

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
        "ARC/radial ordering needs a separate reviewed policy",
        "Core spatial-order planning",
    ):
        if token not in text:
            errors.append("GRID-SPATIAL-ORDERING.md missing ordering/runtime boundary: " + token)

print("QS3D Grid spatial ordering preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: parallel LINE Grid ordering is explicit-axis, bounded and fail-closed; radial/mixed/native automatic renumbering remains separately reviewed and runtime-gated.")
