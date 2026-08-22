#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs"

if not reader.is_file():
    errors.append("missing RoomBoundarySegmentReader.cs")
else:
    text = reader.read_text(encoding="utf-8")
    for needle in (
        "entity is Spline spline",
        "GetDistanceAtParameter(spline.EndParam)",
        "GetPointAtDist(distance)",
        "MaxSplineSegments",
        "splineChordM",
        "RequireElevation(point.Z",
    ):
        if needle not in text:
            errors.append("direct SPLINE room-boundary guard missing: " + needle)

if not command.is_file():
    errors.append("missing RoomBoundaryCommands.cs")
else:
    text = command.read_text(encoding="utf-8")
    for needle in (
        'MetadataNumber(project, "RoomBoundarySplineChordM", 0.02d',
        "ReadCurrentSelection(document, arcSagitta, tolerance, splineChord)",
        'element.Properties["BoundarySplineChordM"]',
        'LINE, POLYLINE, ARC hoặc SPLINE',
    ):
        if needle not in text:
            errors.append("QS3DROOMAUTO SPLINE wiring missing: " + needle)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: direct plan-view SPLINE room-boundary sampling, planarity checks, bounded sampling and project chord metadata are present.")
