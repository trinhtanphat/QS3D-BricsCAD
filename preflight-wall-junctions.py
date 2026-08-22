#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Geometry/WallJunctionPlanner.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionCommands.cs"
hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/WallJunctionRegressionSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

for path in (planner, command, hub, smoke, registration):
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

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DWALLJUNCTIONS"',
        "new WallJunctionPlanner().Plan",
        'MetadataNumber(project, "WallJunctionToleranceM", 0.005d',
        'MetadataNumber(project, "WallArcSagittaM", 0.002d',
        "BulgeArcTessellator.Tessellate",
        'AuditTrail.ForProject(project).Record("wall.junction.analyze"',
    ):
        if needle not in text:
            errors.append("wall junction command guard missing: " + needle)

if hub.is_file() and 'Tag="QS3DWALLJUNCTIONS"' not in hub.read_text(encoding="utf-8"):
    errors.append("Domain Hub does not expose QS3DWALLJUNCTIONS")

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

print("PASS: deterministic End/Straight/L/T/X wall-junction planning, command/UI wiring and regression coverage are present.")
