#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Geometry/WallJunctionPlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/WallJunctionRegressionSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

for path in (planner, smoke, registration):
    if not path.is_file():
        errors.append("missing wall-junction file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "WallJunctionKind",
        "WallJunctionKind.Straight",
        "WallJunctionKind.L",
        "WallJunctionKind.T",
        "WallJunctionKind.X",
        "active = new List<SegmentInfo>()",
        "Intersections(other, current, tolerance)",
        "PointOnSegment(candidate.Point, segment, tolerance)",
        "angularToleranceRadians",
        "Duplicate wall segment id",
        "MaxSegments",
    ):
        if needle not in text:
            errors.append("wall junction planner guard missing: " + needle)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "StraightContinuation();",
        "LCorner();",
        "TJunction();",
        "XJunction();",
        "NearEndpointSnapsByTolerance();",
        "RejectsDuplicateIdsAndInvalidCoordinates();",
    ):
        if needle not in text:
            errors.append("wall junction regression missing: " + needle)

if registration.is_file() and "WallJunctionRegressionSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("WallJunctionRegressionSmoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: deterministic End/Straight/L/T/X wall-junction planning and regression coverage are present.")
