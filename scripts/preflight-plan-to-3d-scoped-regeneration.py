#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs"
PREVIEW = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawProjectPreviewContext.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing PlanTo3DCommands.cs")
if not ENGINE.is_file():
    errors.append("missing RegenerationEngine.cs")
if not PREVIEW.is_file():
    errors.append("missing DirectDrawProjectPreviewContext.cs")

if not errors:
    source = SOURCE.read_text(encoding="utf-8")
    preview = PREVIEW.read_text(encoding="utf-8")
    engine = ENGINE.read_text(encoding="utf-8")
    convert_start = source.find("private static void ConvertPlanWalls")
    acquire_start = source.find("private static IReadOnlyList<ObjectId>? AcquireSelection", convert_start + 1)
    convert = source[convert_start:acquire_start] if convert_start >= 0 and acquire_start > convert_start else ""
    if not convert:
        errors.append("cannot isolate ConvertPlanWalls")
    else:
        required = (
            "regenerator.RegenerateDirtySubset(project, new[] { element.Id })",
            "element.MarkDirty(ElementDirtyFlags.Properties)",
            "WallSolidBuilder.BuildSelectedLineWalls",
            "PolylineWallSolidBuilder.BuildSelected",
            "RequireSameSources(sources, refreshedSources)",
            "projectPreview.ResolveForMutation(document, operation)",
        )
        for token in required:
            if token not in convert:
                errors.append("PlanTo3D scoped regeneration/freshness contract missing: " + token)
        if "regenerator.RegenerateDirty(project)" in convert:
            errors.append("QS3DCONVERT2D must not regenerate unrelated dirty project elements")

        for token in (
            "CadUnitService.GetLengthUnit(document) != ExpectedLengthUnit",
            "document.Editor.CurrentUserCoordinateSystem.Equals(ExpectedUcs)",
            "project.ChangeVersion != ExpectedChangeVersion.Value",
        ):
            if token not in preview:
                errors.append("shared PlanTo3D preview context missing unit/UCS/project freshness token: " + token)

        mark_at = convert.find("element.MarkDirty(ElementDirtyFlags.Properties)")
        regen_at = convert.find("regenerator.RegenerateDirtySubset(project, new[] { element.Id })")
        line_build_at = convert.find("WallSolidBuilder.BuildSelectedLineWalls")
        poly_build_at = convert.find("PolylineWallSolidBuilder.BuildSelected")
        build_at = min(line_build_at, poly_build_at) if line_build_at >= 0 and poly_build_at >= 0 else -1
        if min(mark_at, regen_at, build_at) < 0 or not (mark_at < regen_at < build_at):
            errors.append("PlanTo3D must apply properties -> scoped semantic regeneration -> native wall build")

    if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
        errors.append("Core RegenerationEngine no longer exposes targeted regeneration required by PlanTo3D")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: 2D-plan wall conversion revalidates source/unit freshness and regenerates only each newly captured wall before native build, leaving unrelated dirty semantic elements untouched.")
