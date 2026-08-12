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
        "internal static bool TryZoomSelection(Document document)",
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
        "var paddedWidth = width * 1.25d",
        "var paddedHeight = height * 1.25d",
        "if (!FinitePositive(paddedWidth) || !FinitePositive(paddedHeight)) return false;",
        "view.Width = paddedWidth",
        "view.Height = paddedHeight",
        "private static void EnsureTiledModelSpace(Document document)",
        "if (document.Database.TileMode) return;",
        "document.Database.TileMode = true;",
    ):
        if needle not in text:
            errors.append("ViewportCommands.cs missing DCS/model-space safety token: " + needle)

    command_count = len(re.findall(r'\[CommandMethod\("QS3DZOOMSELECTED"', text, re.IGNORECASE))
    if command_count != 1:
        errors.append("QS3DZOOMSELECTED must have exactly one command owner; found %d" % command_count)

    zoom_start = text.find("internal static bool TryZoomSelection(Document document)")
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

        padded_width = body.find("var paddedWidth = width * 1.25d")
        padded_height = body.find("var paddedHeight = height * 1.25d")
        padded_check = body.find("if (!FinitePositive(paddedWidth) || !FinitePositive(paddedHeight)) return false;")
        assign_width = body.find("view.Width = paddedWidth")
        assign_height = body.find("view.Height = paddedHeight")
        if min(padded_width, padded_height, padded_check, assign_width, assign_height) < 0:
            errors.append("Zoom Selected must compute, validate, and assign finite padded view dimensions.")
        elif not (padded_width < padded_check and padded_height < padded_check < assign_width and padded_check < assign_height):
            errors.append("Padded zoom dimensions must be validated before mutating the current view.")
        if "view.Width = width * 1.25d" in body or "view.Height = height * 1.25d" in body:
            errors.append("Zoom Selected must not assign unvalidated padding expressions directly to the current view.")

    viewport_commands_end = text.find('[CommandMethod("QS3DUNTRACK"')
    viewport_commands = text[:viewport_commands_end] if viewport_commands_end >= 0 else text
    if "SwitchToModelSpace()" in viewport_commands:
        errors.append("Viewport commands must not call Editor.SwitchToModelSpace() blindly; it throws eInvalidInput when the Model tab is already active.")
    expected_model_focus_commands = (
        'CommandMethod("QS3DVIEW3D"',
        'CommandMethod("QS3DVIEWTOP"',
        'CommandMethod("QS3DORBIT"',
        'CommandMethod("QS3DFOCUSMODEL"',
        'CommandMethod("QS3DZOOMALL"',
    )
    for command in expected_model_focus_commands:
        start = viewport_commands.find(command)
        if start < 0:
            errors.append("Viewport command missing: " + command)
            continue
        line_end = viewport_commands.find("\n", start)
        command_line = viewport_commands[start:line_end if line_end >= 0 else len(viewport_commands)]
        if "EnsureTiledModelSpace(doc)" not in command_line:
            errors.append(command + " must use idempotent EnsureTiledModelSpace before changing the model view.")

print("QS3D viewport zoom preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: viewport commands use idempotent TILEMODE-aware model focus; QS3DZOOMSELECTED transforms WCS extents into current-view DCS, preserves camera orientation, and validates padded dimensions before mutating the view.")
