#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
viewport = ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs"
if not viewport.is_file():
    errors.append("missing viewport command source: " + str(viewport.relative_to(ROOT)))
else:
    text = viewport.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DZOOMSELECTED"',
        "var worldToDisplay = WorldToDisplay(view)",
        "extents.TransformBy(worldToDisplay)",
        "Matrix3d.PlaneToWorld(view.ViewDirection)",
        "Matrix3d.Displacement(view.Target - Point3d.Origin)",
        "Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target)",
        "return matrix.Inverse()",
        "view.CenterPoint = new Point2d(centerX, centerY)",
        "MinimumViewSpan(view)",
        "Finite(extentMin)",
        "Finite(extentMax)",
    ):
        if needle not in text:
            errors.append("ViewportCommands.cs missing DCS zoom token: " + needle)

    command_count = len(re.findall(r'\[CommandMethod\("QS3DZOOMSELECTED"', text, re.IGNORECASE))
    if command_count != 1:
        errors.append("QS3DZOOMSELECTED must have exactly one command owner; found %d" % command_count)

    zoom_start = text.find("private static bool TryZoomSelection")
    transform_start = text.find("private static Matrix3d WorldToDisplay", zoom_start)
    if zoom_start < 0 or transform_start < 0:
        errors.append("TryZoomSelection/WorldToDisplay boundary is missing.")
    else:
        body = text[zoom_start:transform_start]
        transformed = body.find("extents.TransformBy(worldToDisplay)")
        first_union = body.find("min = extentMin")
        if transformed < 0 or first_union < 0 or transformed > first_union:
            errors.append("Selection extents must be transformed from WCS to DCS before union/framing.")
        if "view.ViewDirection =" in body or "view.Target =" in body or "view.ViewTwist =" in body:
            errors.append("Zoom Selected must frame the current view without changing camera direction/target/twist.")

print("QS3D viewport zoom preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: QS3DZOOMSELECTED transforms WCS entity extents into the current view DCS before framing, preserves the camera orientation, and rejects non-finite bounds.")
