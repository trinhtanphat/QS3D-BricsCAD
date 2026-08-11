#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
P0 = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
OPENING = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs"
errors = []


def section(text, start_token, end_token=None):
    start = text.find(start_token)
    if start < 0:
        return ""
    if end_token is None:
        return text[start:]
    end = text.find(end_token, start + len(start_token))
    return text[start:end if end >= 0 else len(text)]


def require_order(body, name, tokens):
    positions = []
    for token in tokens:
        pos = body.find(token)
        if pos < 0:
            errors.append(name + " missing: " + token)
            return
        positions.append(pos)
    if positions != sorted(positions):
        errors.append(name + " lifecycle order is stale: " + " -> ".join(tokens))


if not P0.is_file():
    errors.append("missing DirectDrawCommands.cs")
else:
    text = P0.read_text(encoding="utf-8")
    for method, next_method, command in (
        ("public void DrawColumn()", "[CommandMethod(\"QS3DDRAWCOLUMNADV\"", "QS3DDRAWCOLUMN"),
        ("public void DrawColumnAdvanced()", "private static void ExecuteDirect(", "QS3DDRAWCOLUMNADV"),
    ):
        body = section(text, method, next_method)
        if not body:
            errors.append("missing P0 workflow: " + command)
            continue
        require_order(body, command, (
            "var promptUnit = (object)CadUnitService.GetLengthUnit(document);",
            "var promptUcs = document.Editor.CurrentUserCoordinateSystem;",
            "document.Editor.GetPoint(",
            "RequirePromptContextUnchanged(document, promptUnit, promptUcs, \"" + command + "\");",
        ))

    for helper_name, next_helper in (
        ("private static IReadOnlyList<Point3d>? AcquireFixedPath(", "private static IReadOnlyList<Point3d>? AcquirePath("),
        ("private static IReadOnlyList<Point3d>? AcquirePath(", "private static void ValidatePlanView("),
    ):
        body = section(text, helper_name, next_helper)
        if not body:
            errors.append("missing P0 acquisition helper: " + helper_name)
            continue
        require_order(body, helper_name, (
            "var promptUnit = (object)CadUnitService.GetLengthUnit(document);",
            "var promptUcs = editor.CurrentUserCoordinateSystem;",
            "editor.GetPoint(",
            "RequirePromptContextUnchanged(document, promptUnit, promptUcs, label);",
            "ValidatePlanView(document, points, label);",
        ))

    helper = section(text, "private static void RequirePromptContextUnchanged(", "private static void RequireModelSpace(")
    for token in (
        "EnsureActive(document, operation + \" / geometry prompt freshness\");",
        "RequireModelSpace(document);",
        "Equals(CadUnitService.GetLengthUnit(document), promptUnit)",
        "document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs)",
    ):
        if token not in helper:
            errors.append("P0 prompt freshness helper missing: " + token)

if not OPENING.is_file():
    errors.append("missing DirectDrawOpeningCommands.cs")
else:
    text = OPENING.read_text(encoding="utf-8")
    body = section(text, "private static void DrawOpening(", "private static void Execute(")
    require_order(body, "Door/Opening", (
        "var promptUnit = (object)CadUnitService.GetLengthUnit(document);",
        "var promptUcs = document.Editor.CurrentUserCoordinateSystem;",
        "AcquireTwoPoints(document,",
        "RequirePromptContextUnchanged(document, promptUnit, promptUcs, operation);",
        "var widthDrawing = CadGeometryGuard.Hypot(",
        "var projectPreview = DirectDrawProjectPreviewContext.Capture(document);",
    ))

    helper = section(text, "private static void RequirePromptContextUnchanged(", "private static void RequireModelSpace(")
    for token in (
        "EnsureActive(document, operation + \" / geometry prompt freshness\");",
        "RequireModelSpace(document);",
        "Equals(CadUnitService.GetLengthUnit(document), promptUnit)",
        "document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs)",
    ):
        if token not in helper:
            errors.append("Door/Opening prompt freshness helper missing: " + token)

if errors:
    print("Direct Draw P0/Opening prompt freshness preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Direct Draw P0/Opening prompt freshness preflight PASS")
