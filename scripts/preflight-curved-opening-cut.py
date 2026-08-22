#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurvedOpeningFootprintPlanner.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs",
    "src/QS3D.BricsCAD.V25/CurvedOpeningBooleanCommands.cs",
    "tests/QS3D.Core.SmokeTests/CurvedOpeningFootprintSmoke.cs",
    "tests/QS3D.Core.SmokeTests/CurvedOpeningFootprintRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing curved-opening file: " + relative)

planner = ROOT / required[0]
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "CurvedOpeningFootprintInput",
        "MaximumCenterlineOffsetM",
        "AmbiguityMarginM",
        "Opening width/position extends beyond the curved host centerline",
        "ambiguous between non-adjacent curved-host branches",
        "WallFootprintEngine().Build",
        "CutterPolygon",
        "CenterStationM",
    ):
        if needle not in text:
            errors.append("curved opening footprint guard missing: " + needle)

service = ROOT / required[1]
if service.is_file():
    text = service.read_text(encoding="utf-8")
    for needle in (
        "CurvedOpeningFootprintPlanner.Plan",
        "OpeningCutPlanner.Plan",
        "HasBulge(hostSource)",
        "Region.CreateFromCurves",
        "CreateExtrudedSolid",
        "BooleanOperationType.BoolSubtract",
        'PhysicalOpeningCutMode"] = "CurvedCenterlineFootprint"',
        "CadElementVerticalPlacement.Resolve(",
        "CadHostedOpeningVerticalPlacement.Resolve(",
        "hostPlacement.BottomDrawing",
        "CadGeometryGuard.ToDrawingUnits(document, baseElevationM",
        "CurvedFingerprint(",
        "Build 3D lại host trước khi khoét curved openings",
    ):
        if needle not in text:
            errors.append("curved opening boolean guard missing: " + needle)
    fingerprint_index = text.find("var fingerprint = CurvedFingerprint")
    idempotence_index = text.find('host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle"')
    subtract_index = text.find("BooleanOperationType.BoolSubtract")
    if fingerprint_index < 0 or idempotence_index < 0 or subtract_index < 0:
        errors.append("curved opening idempotence/subtract ordering tokens are incomplete")
    elif not (fingerprint_index < idempotence_index < subtract_index):
        errors.append("curved opening fingerprint/idempotence check must occur before destructive BoolSubtract")

command = ROOT / required[2]
if command.is_file():
    text = command.read_text(encoding="utf-8")
    if 'CommandMethod("QS3DCUTOPENINGSCURVED"' not in text:
        errors.append("missing QS3DCUTOPENINGSCURVED command")
    if "CurvedOpeningBooleanService.CutLinkedOpenings" not in text:
        errors.append("QS3DCUTOPENINGSCURVED is not wired to curved boolean service")

smoke = ROOT / required[3]
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "StraightPathMatchesExpectedSpan();",
        "CornerSpanIncludesIntermediateVertex();",
        "RejectsFarAndAmbiguousBranches();",
        "RejectsOpeningPastHostEnd();",
    ):
        if needle not in text:
            errors.append("curved opening regression missing: " + needle)

registration = ROOT / required[4]
if registration.is_file() and "CurvedOpeningFootprintSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("CurvedOpeningFootprintSmoke is not registered")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DCUTOPENINGSCURVED") != 1:
    errors.append("QS3DCUTOPENINGSCURVED must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curved host opening footprint planning, pre-destructive fingerprint/idempotence guards, Region extrusion, BoolSubtract command wiring and deterministic regressions are present.")
