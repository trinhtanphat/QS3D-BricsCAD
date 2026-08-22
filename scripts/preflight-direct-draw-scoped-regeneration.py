#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawCommands.cs")
if not ENGINE.is_file():
    errors.append("missing RegenerationEngine.cs")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    engine = ENGINE.read_text(encoding="utf-8")
    start = source.find("private static void ExecuteDirect")
    end = source.find("private static int BuildSelected", start + 1)
    body = source[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append("cannot isolate ExecuteDirect")
    else:
        for token in (
            "configureElement?.Invoke(createdElement)",
            ".RegenerateDirtySubset(project, new[] { createdElement.Id })",
            "solids = BuildSelected(document, project, category)",
            "ProjectStateSnapshot.Capture(project)",
            "rollback.Restore(project)",
        ):
            if token not in body:
                errors.append("Direct Draw scoped-regeneration contract missing: " + token)
        if ".RegenerateDirty(project)" in body:
            errors.append("Direct Draw must not regenerate unrelated dirty project elements")

        configure = body.find("configureElement?.Invoke(createdElement)")
        regen = body.find(".RegenerateDirtySubset(project, new[] { createdElement.Id })")
        build = body.find("solids = BuildSelected(document, project, category)")
        if min(configure, regen, build) < 0 or not (configure < regen < build):
            errors.append("Direct Draw must configure -> scoped-regenerate the created element -> build native geometry")

    if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
        errors.append("Core RegenerationEngine no longer exposes targeted regeneration required by Direct Draw")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Direct Draw regenerates only the newly created semantic element before native build; unrelated dirty project elements stay outside the authoring side effect.")
