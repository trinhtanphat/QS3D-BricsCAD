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

    if "ProjectContextCoordinator.TryGetReadOnly(document, out var defaultsProject)" in text:
        errors.append("Direct Draw P1 Family defaults must use DirectDrawProjectPreviewContext instead of an unguarded read-only snapshot")

    preview_tokens = {
        "DirectDrawProjectPreviewContext.Capture(document)": 8,
        "var defaultsProject = projectPreview.DefaultsProject;": 8,
        "var hasDefaultsProject = projectPreview.HasProject;": 8,
        "projectPreview);": 8,
    }
    for token, expected in preview_tokens.items():
        actual = text.count(token)
        if actual != expected:
            errors.append("Direct Draw P1 preview freshness contract mismatch for " + token + ": expected " + str(expected) + ", found " + str(actual))

    quick_workflows = (
        ("DrawGlassWall", "QS3DDRAWGLASSWALL"),
        ("DrawWallPier", "QS3DDRAWWALLPIER"),
        ("DrawStructuralWall", "QS3DDRAWSTRUCTWALL"),
        ("DrawFoundation", "QS3DDRAWFOUNDATION"),
    )
    for method, command in quick_workflows:
        start = text.find("public void " + method + "()")
        if start < 0:
            errors.append("missing quick workflow: " + method)
            continue
        next_method = text.find("[CommandMethod(", start + 1)
        end = next_method if next_method >= 0 else len(text)
        body = text[start:end]
        capture = body.find("var projectPreview = DirectDrawProjectPreviewContext.Capture(document);")
        defaults = body.find("var defaultsProject = projectPreview.DefaultsProject;")
        execute = body.find("Execute(")
        pass_preview = body.rfind("projectPreview);")
        if capture < 0 or defaults < capture:
            errors.append(command + " must capture guarded project preview before reading Family defaults")
        if execute < 0 or pass_preview < execute:
            errors.append(command + " must pass the same guarded project preview through Execute")
        if "ProjectContextCoordinator.TryGetReadOnly(" in body:
            errors.append(command + " must not sample Family defaults from an unguarded read-only project")

    workflows = (
        ("DrawGlassWallAdvanced", "QS3DDRAWGLASSWALLADV", "AcquirePath(document, \"Vách Kính\""),
        ("DrawWallPierAdvanced", "QS3DDRAWWALLPIERADV", "AcquireFixedPath(document, \"Trụ Tường\""),
        ("DrawStructuralWallAdvanced", "QS3DDRAWSTRUCTWALLADV", "AcquireFixedPath(document, \"Vách BTCT\""),
        ("DrawFoundationAdvanced", "QS3DDRAWFOUNDATIONADV", "AcquirePath(document, \"Móng\""),
    )

    for method, command, acquire_token in workflows:
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
        project_preview = body.find("var projectPreview = DirectDrawProjectPreviewContext.Capture(document);")
        pass_preview = body.rfind("projectPreview);")

        if unit_capture < 0:
            errors.append(command + " must capture drawing unit before interactive geometry/prompt work")
        if ucs_capture < 0:
            errors.append(command + " must capture UCS before interactive geometry/prompt work")
        if acquire < 0:
            errors.append(command + " missing expected geometry acquisition")
        if min(unit_capture, ucs_capture) >= 0 and acquire >= 0 and max(unit_capture, ucs_capture) > acquire:
            errors.append(command + " must capture unit/UCS before geometry acquisition")
        if project_preview < 0:
            errors.append(command + " must capture guarded project preview before reading Family defaults")
        if freshness < 0:
            errors.append(command + " must revalidate prompt context before mutation")
        if execute < 0:
            errors.append(command + " missing Execute mutation boundary")
        if freshness >= 0 and execute >= 0 and freshness > execute:
            errors.append(command + " must revalidate prompt context before Execute")
        if execute >= 0 and pass_preview < execute:
            errors.append(command + " must pass guarded project preview through Execute")

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
    print("Direct Draw P1 prompt/preview freshness preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Direct Draw P1 prompt/preview freshness preflight PASS")
