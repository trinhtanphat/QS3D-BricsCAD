#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs": {
        "required": (
            "UcsAxisTolerance = 1e-9d",
            "RequireSupportedUcs(document);",
            "document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d",
            "var zAxis = coordinateSystem.Zaxis;",
            "Math.Abs(z - 1d) > UcsAxisTolerance",
            "line.TransformBy(document.Editor.CurrentUserCoordinateSystem);",
            "polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);",
        ),
        "line": "var line = new Line(start, end);",
        "poly": "polyline.Closed = closed;",
    },
    "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs": {
        "required": (
            "UcsAxisTolerance = 1e-9d",
            "RequireSupportedUcs(document);",
            "document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d",
            "var zAxis = coordinateSystem.Zaxis;",
            "Math.Abs(z - 1d) > UcsAxisTolerance",
            "line.TransformBy(document.Editor.CurrentUserCoordinateSystem);",
        ),
        "line": "var line = new Line(safeStart, safeEnd);",
        "poly": None,
    },
}

for relative, contract in contracts.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing extended UCS source: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in contract["required"]:
        if needle not in text:
            errors.append(relative + " missing UCS contract: " + needle)

    model_guard = text.find("private static void RequireModelSpace(Document document)")
    ucs_call = text.find("RequireSupportedUcs(document);", model_guard)
    ucs_guard = text.find("private static void RequireSupportedUcs(Document document)", model_guard)
    if min(model_guard, ucs_call, ucs_guard) < 0 or not (model_guard < ucs_call < ucs_guard):
        errors.append(relative + " must validate supported UCS immediately after the Model Space guard")

    line_method = text.find("private static ObjectId CreateLine(")
    line_create = text.find(contract["line"], line_method)
    line_transform = text.find("line.TransformBy(document.Editor.CurrentUserCoordinateSystem);", line_method)
    line_append = text.find("modelSpace.AppendEntity(line)", line_method)
    if min(line_method, line_create, line_transform, line_append) < 0 or not (line_create < line_transform < line_append):
        errors.append(relative + " LINE source must transform from current UCS before ModelSpace append")

    if contract["poly"]:
        poly_method = text.find("private static ObjectId CreatePolyline(")
        poly_ready = text.find(contract["poly"], poly_method)
        poly_transform = text.find("polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);", poly_method)
        poly_append = text.find("modelSpace.AppendEntity(polyline)", poly_method)
        if min(poly_method, poly_ready, poly_transform, poly_append) < 0 or not (poly_ready < poly_transform < poly_append):
            errors.append(relative + " POLYLINE source must transform from current UCS before ModelSpace append")

    for forbidden in (
        "document.Editor.CurrentUserCoordinateSystem = Matrix3d.Identity",
        "document.Editor.CurrentUserCoordinateSystem = new Matrix3d",
    ):
        if forbidden in text:
            errors.append(relative + " must not mutate the user's current UCS: " + forbidden)

print("QS3D Direct Draw extended UCS preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: P1 and Door/Opening Direct Draw keep prompts in current planar UCS, transform persisted source geometry into database WCS before append, and reject tilted/3D UCS without mutating the user's UCS.")
