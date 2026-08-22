#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawProjectPreviewContext.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawProjectPreviewContext.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "ExpectedChangeVersion",
        "project.ChangeVersion,",
        "project.ChangeVersion != ExpectedChangeVersion.Value",
        "ExpectedLengthUnit",
        "CadUnitService.GetLengthUnit(document)",
        "CadUnitService.GetLengthUnit(document) != ExpectedLengthUnit",
        "ExpectedUcs",
        "document.Editor.CurrentUserCoordinateSystem",
        "CurrentUserCoordinateSystem.Equals(ExpectedUcs)",
        "EnsureCadContextFresh(document);",
        "HasBackingStore(document)",
        "File.Exists(path + \".bak\")",
        "ProjectContextCoordinator.Forget(document);",
    )
    for token in required:
        if token not in text:
            errors.append("Direct Draw preview freshness missing: " + token)

    freshness = text.find("EnsureCadContextFresh(document);")
    project_branch = text.find("if (HasProject)")
    if freshness < 0 or project_branch < 0 or freshness > project_branch:
        errors.append("CAD freshness must be checked before resolving any mutation project")

    version_check = text.find("project.ChangeVersion != ExpectedChangeVersion.Value")
    existing_return = text.find("return project;", project_branch)
    if version_check < 0 or existing_return < 0 or version_check > existing_return:
        errors.append("semantic ChangeVersion freshness must be checked before returning the existing project")

    create = text.find("var created = ProjectContextCoordinator.GetOrCreate(document);")
    forget = text.find("ProjectContextCoordinator.Forget(document);", create)
    if create < 0 or forget < 0 or forget < create:
        errors.append("sidecar race guard must forget a speculative project bind after GetOrCreate")

if errors:
    print("Direct Draw preview freshness preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Direct Draw preview freshness preflight PASS")
