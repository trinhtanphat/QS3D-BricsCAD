#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawP1Commands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    workflows = (
        ("DrawGlassWallAdvanced", "QS3DDRAWGLASSWALLADV", "AcquirePath(document, \"Vách Kính\""),
        ("DrawWallPierAdvanced", "QS3DDRAWWALLPIERADV", "AcquireFixedPath(document, \"Trụ Tường\""),
        ("DrawStructuralWallAdvanced", "QS3DDRAWSTRUCTWALLADV", "AcquireFixedPath(document, \"Vách BTCT\""),
        ("DrawFoundationAdvanced", "QS3DDRAWFOUNDATIONADV", "AcquirePath(document, \"Móng\""),
    )

    for index, (method, command, acquire_token) in enumerate(workflows):
        start = text.find("public void " + method + "()")
        if start < 0:
            errors.append("missing advanced workflow: " + method)
            continue
        next_method = text.find("[CommandMethod(", start + 1)
        end = next_method if next_method >= 0 else len(text)
        body = text[start:end]

        unit_capture = body.find("var promptUnit = CadUnitService.GetLengthUnit(document);")
        ucs_capture = body.find("var promptUcs = document.Editor.CurrentUserCoordinateSystem;")
        acquire = body.find(acquire_token)
        freshness = body.find("RequirePromptContextUnchanged(document, promptUnit, promptUcs, \"" + command + "\");")
        execute = body.find("Execute(")

        if unit_capture < 0:
            errors.append(command + " must capture drawing unit before interactive geometry/prompt work")
        if ucs_capture < 0:
            errors.append(command + " must capture UCS before interactive geometry/prompt work")
        if acquire < 0:
            errors.append(command + " missing expected geometry acquisition")
        if min(unit_capture, ucs_capture) >= 0 and acquire >= 0 and max(unit_capture, ucs_capture) > acquire:
            errors.append(command + " must capture unit/UCS before geometry acquisition")
        if freshness < 0:
            errors.append(command + " must revalidate prompt context before mutation")
        if execute < 0:
            errors.append(command + " missing Execute mutation boundary")
        if freshness >= 0 and execute >= 0 and freshness > execute:
            errors.append(command + " must revalidate prompt context before Execute")

    helper = text.find("private static void RequirePromptContextUnchanged(")
    if helper < 0:
        errors.append("missing RequirePromptContextUnchanged helper")
    else:
        helper_end = text.find("private static void RequireModelSpace", helper)
        helper_body = text[helper:helper_end if helper_end >= 0 else len(text)]
        required = (
            "EnsureActive(document, operation + \" / prompt freshness\");",
            "RequireModelSpace(document);",
            "document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs)",
            "CadUnitService.GetLengthUnit(document) != promptUnit",
        )
        for token in required:
            if token not in helper_body:
                errors.append("prompt freshness helper missing: " + token)

if errors:
    print("Direct Draw P1 prompt freshness preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Direct Draw P1 prompt freshness preflight PASS")
