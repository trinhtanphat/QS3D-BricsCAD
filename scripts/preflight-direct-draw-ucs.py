#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
errors = []

if not source.is_file():
    errors.append("missing DirectDrawCommands.cs")
else:
    text = source.read_text(encoding="utf-8")
    required = (
        "UcsAxisTolerance = 1e-9d",
        "RequireSupportedUcs(document);",
        "document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d",
        "var zAxis = coordinateSystem.Zaxis;",
        "var length = zAxis.Length;",
        "Math.Abs(z - 1d) > UcsAxisTolerance",
        "UCS có mặt phẳng XY song song WCS XY",
        "line.TransformBy(document.Editor.CurrentUserCoordinateSystem);",
        "polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);",
    )
    for needle in required:
        if needle not in text:
            errors.append("Direct Draw planar-UCS contract missing: " + needle)

    model_guard = text.find("private static void RequireModelSpace(Document document)")
    ucs_call = text.find("RequireSupportedUcs(document);", model_guard)
    ucs_guard = text.find("private static void RequireSupportedUcs(Document document)", model_guard)
    if min(model_guard, ucs_call, ucs_guard) < 0 or not (model_guard < ucs_call < ucs_guard):
        errors.append("Direct Draw must validate supported UCS immediately after the Model Space guard")

    line_method = text.find("private static ObjectId CreateLine(")
    line_create = text.find("var line = new Line(start, end);", line_method)
    line_transform = text.find("line.TransformBy(document.Editor.CurrentUserCoordinateSystem);", line_method)
    line_append = text.find("modelSpace.AppendEntity(line)", line_method)
    if min(line_method, line_create, line_transform, line_append) < 0 or not (line_create < line_transform < line_append):
        errors.append("LINE source must be transformed from current UCS before ModelSpace append")

    poly_method = text.find("private static ObjectId CreatePolyline(")
    poly_closed = text.find("polyline.Closed = closed;", poly_method)
    poly_transform = text.find("polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);", poly_method)
    poly_append = text.find("modelSpace.AppendEntity(polyline)", poly_method)
    if min(poly_method, poly_closed, poly_transform, poly_append) < 0 or not (poly_closed < poly_transform < poly_append):
        errors.append("POLYLINE source must be completed in UCS-local coordinates then transformed before ModelSpace append")

    forbidden = (
        "document.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity",
        "document.Editor.CurrentUserCoordinateSystem = new Matrix3d",
    )
    for needle in forbidden:
        if needle in text:
            errors.append("Direct Draw must not mutate the user's current UCS: " + needle)

print("QS3D Direct Draw UCS preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw keeps prompt geometry in current planar UCS, transforms LINE/POLYLINE sources into database WCS before append, and rejects tilted/3D UCS without mutating the user's UCS.")
