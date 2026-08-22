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
            "DirectDrawProjectPreviewContext.Capture(document)",
            "RequireSameSources(sources, refreshedSources)",
            "projectPreview.ResolveForMutation(document, operation)",
            "RequireFreshSources(project, sources)",
            "element.MarkDirty(ElementDirtyFlags.Properties)",
            "regenerator.RegenerateDirtySubset(project, new[] { element.Id })",
            "WallSolidBuilder.BuildSelectedLineWalls",
            "PolylineWallSolidBuilder.BuildSelected",
        )
        for token in required:
            if token not in convert:
                errors.append("PlanTo3D scoped regeneration/freshness contract missing: " + token)
        if "regenerator.RegenerateDirty(project)" in convert:
            errors.append("QS3DCONVERT2D must not regenerate unrelated dirty project elements")

        refresh_at = convert.find("RequireSameSources(sources, refreshedSources)")
        resolve_at = convert.find("projectPreview.ResolveForMutation(document, operation)")
        ownership_at = convert.find("RequireFreshSources(project, sources)", resolve_at)
        mark_at = convert.find("element.MarkDirty(ElementDirtyFlags.Properties)")
        regen_at = convert.find("regenerator.RegenerateDirtySubset(project, new[] { element.Id })")
        line_build_at = convert.find("WallSolidBuilder.BuildSelectedLineWalls")
        poly_build_at = convert.find("PolylineWallSolidBuilder.BuildSelected")
        build_at = min(line_build_at, poly_build_at) if line_build_at >= 0 and poly_build_at >= 0 else -1
        if min(refresh_at, resolve_at, ownership_at, mark_at, regen_at, build_at) < 0 or not (
            refresh_at < resolve_at < ownership_at < mark_at < regen_at < build_at
        ):
            errors.append("PlanTo3D must revalidate source -> resolve guarded project -> recheck ownership -> apply properties -> scoped semantic regeneration -> native wall build")

        for stale in (
            "CadUnitService.GetLengthUnit(document) != selectionUnit",
            "var selectionUnit =",
            "ProjectContextCoordinator.GetOrCreate(document)",
        ):
            if stale in convert:
                errors.append("PlanTo3D must not duplicate stale project/unit freshness path: " + stale)

    for token in (
        "ExpectedLengthUnit",
        "CadUnitService.GetLengthUnit(document) != ExpectedLengthUnit",
        "ExpectedUcs",
        "CurrentUserCoordinateSystem.Equals(ExpectedUcs)",
        "ExpectedChangeVersion",
        "project.ChangeVersion != ExpectedChangeVersion.Value",
    ):
        if token not in preview:
            errors.append("shared preview freshness contract required by PlanTo3D missing: " + token)

    if "public int RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)" not in engine:
        errors.append("Core RegenerationEngine no longer exposes targeted regeneration required by PlanTo3D")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: 2D-plan wall conversion revalidates exact sources, resolves the shared unit/UCS/project freshness guard, and regenerates only each newly captured wall before native build, leaving unrelated dirty semantic elements untouched.")
