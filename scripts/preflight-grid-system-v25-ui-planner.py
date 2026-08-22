#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/GridSystemCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing V25 Grid system command source: " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private static PreviewResult BuildPreview(")
    end = text.find("private static void AccumulateExtent(", start)
    if start < 0 or end < 0:
        errors.append("GridSystemCommands.cs missing BuildPreview boundary")
    else:
        preview = text[start:end]
        required = (
            "var uLines = new List<LineSample>();",
            "var vLines = new List<LineSample>();",
            "uLines.Add(line);",
            "vLines.Add(line);",
            "var orderedU = GridSpatialOrderingPlanner.OrderParallelLines(",
            "uLines.Select(ToReferenceCurve)",
            "uAxis,",
            "var orderedV = GridSpatialOrderingPlanner.OrderParallelLines(",
            "vLines.Select(ToReferenceCurve)",
            "vAxis,",
            ".Select(x => new GridLinearStation(x.ElementId, x.Coordinate))",
            "UStations = uStations.AsReadOnly()",
            "VStations = vStations.AsReadOnly()",
        )
        for token in required:
            if token not in preview:
                errors.append("GridSystemCommands.BuildPreview missing planner-consumption token: " + token)

        if preview.count("GridSpatialOrderingPlanner.OrderParallelLines(") != 2:
            errors.append("GridSystemCommands.BuildPreview must spatial-order exactly the two rectangular Grid families")
        if "uStations.Add(new GridLinearStation" in preview or "vStations.Add(new GridLinearStation" in preview:
            errors.append("GridSystemCommands.BuildPreview must not bypass GridSpatialOrderingPlanner with direct station accumulation")

    required_flow = (
        "var preview = BuildPreview(extraction.Lines);",
        "var planned = GridSystemPlanner.PlanRectangular(",
        "preview.Input,",
        "var intersections = GridIntersectionPlanner.FindIntersections(planned);",
        "return GridReferenceCurve.Line(line.ElementId, line.Start, line.End);",
    )
    for token in required_flow:
        if token not in text:
            errors.append("GridSystemCommands.cs missing reviewed planner flow token: " + token)

print("QS3D V25 Grid system UI/planner integration preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DGRIDSYSTEMPREVIEW routes both rectangular LINE families through deterministic GridSpatialOrderingPlanner output before GridSystemPlanner and GridIntersectionPlanner review.")
