#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawReferenceWallCommands.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DirectDrawReferenceWallCommands.cs")
if not ENGINE.is_file():
    errors.append("missing RegenerationEngine.cs")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    engine = ENGINE.read_text(encoding="utf-8")
    start = source.find("private static void Execute(")
    end = source.find("private static ObjectId CreateWcsLine", start + 1)
    body = source[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append("cannot isolate reference-wall Execute")
    else:
        for token in (
            "createdElementId = createdElement.Id",
            "configureElement(createdElement)",
            ".RegenerateDirtySubset(project, new[] { createdElementId })",
            "WallSolidBuilder.BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall)",
            "ProjectStateSnapshot.Capture(project)",
            "EraseCreatedCad(document, project, createdElement, sourceId, generatedHandles)",
            "rollback.Restore(project)",
        ):
            if token not in body:
                errors.append("reference-wall scoped regeneration/rollback contract missing: " + token)
        if ".RegenerateDirty(project)" in body:
            errors.append("reference-wall Direct Draw must not regenerate unrelated dirty project elements")

        configure = body.find("configureElement(createdElement)")
        regen = body.find(".RegenerateDirtySubset(project, new[] { createdElementId })")
        build = body.find("WallSolidBuilder.BuildSelectedLineWalls")
        if min(configure, regen, build) < 0 or not (configure < regen < build):
            errors.append("reference-wall Direct Draw must configure -> scoped regenerate -> native build")

    if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
        errors.append("Core RegenerationEngine no longer exposes targeted regeneration")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: reference-wall Direct Draw regenerates only its newly captured wall before native build and preserves ownership-scoped rollback; unrelated dirty project elements remain untouched.")
