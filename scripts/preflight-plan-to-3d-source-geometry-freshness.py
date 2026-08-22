#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing PlanTo3DCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    convert_start = text.find("private static void ConvertPlanWalls")
    acquire_start = text.find("private static IReadOnlyList<ObjectId>? AcquireSelection", convert_start)
    candidate_start = text.find("private sealed class SourceCandidate")
    preflight_start = text.find("private static IReadOnlyList<SourceCandidate> PreflightSources")
    same_start = text.find("private static void RequireSameSources", preflight_start)
    line_start = text.find("private static string BuildLineGeometryFingerprint", same_start)
    polyline_start = text.find("private static string BuildOpenPolylineGeometryFingerprint", line_start)
    append_start = text.find("private static void AppendPoint3d", polyline_start)
    normal_start = text.find("private static void RequireWorldPlanNormal", append_start)
    if min(convert_start, acquire_start, candidate_start, preflight_start, same_start, line_start, polyline_start, append_start, normal_start) < 0:
        errors.append("cannot isolate PlanTo3D source geometry freshness regions")
    else:
        convert = text[convert_start:acquire_start]
        candidate = text[candidate_start:preflight_start]
        preflight = text[preflight_start:same_start]
        same = text[same_start:line_start]
        line = text[line_start:polyline_start]
        polyline = text[polyline_start:append_start]
        helpers = text[append_start:normal_start]

        if convert.count("PreflightSources(document, selectedIds)") < 3:
            errors.append("ConvertPlanWalls must preflight sources initially, after prompts, and again after project-context resolution")

        resolve_at = convert.find("var project = projectPreview.ResolveForMutation(document, operation);")
        commit_preflight_at = convert.find("var commitSources = PreflightSources(document, selectedIds);", resolve_at)
        commit_compare_at = convert.find("RequireSameSources(sources, commitSources);", commit_preflight_at)
        commit_assign_at = convert.find("sources = commitSources;", commit_compare_at)
        semantic_fresh_at = convert.find("RequireFreshSources(project, sources);", commit_assign_at)
        snapshot_at = convert.find("var rollback = ProjectStateSnapshot.Capture(project);", semantic_fresh_at)
        if min(resolve_at, commit_preflight_at, commit_compare_at, commit_assign_at, semantic_fresh_at, snapshot_at) < 0 or not (
            resolve_at < commit_preflight_at < commit_compare_at < commit_assign_at < semantic_fresh_at < snapshot_at
        ):
            errors.append(
                "commit boundary must re-read and compare CAD geometry after ResolveForMutation and before semantic freshness/snapshot"
            )

        if "public string GeometryFingerprint { get; set; } = string.Empty;" not in candidate:
            errors.append("SourceCandidate must carry a non-null geometry fingerprint")

        for token in (
            "GeometryFingerprint = BuildLineGeometryFingerprint(line)",
            "GeometryFingerprint = BuildOpenPolylineGeometryFingerprint(polyline)",
        ):
            if token not in preflight:
                errors.append("PreflightSources geometry snapshot missing: " + token)

        for token in (
            "string.IsNullOrWhiteSpace(left.GeometryFingerprint)",
            "string.IsNullOrWhiteSpace(right.GeometryFingerprint)",
            "string.Equals(left.GeometryFingerprint, right.GeometryFingerprint, StringComparison.Ordinal)",
        ):
            if token not in same:
                errors.append("RequireSameSources geometry fail-closed check missing: " + token)

        for token in (
            'new StringBuilder("QS3D_PLAN_SOURCE_V1|kind=LINE|start=")',
            "line.StartPoint",
            "line.EndPoint",
            "line.Normal",
            "line.Thickness",
            "return HashGeometrySnapshot(canonical);",
        ):
            if token not in line:
                errors.append("LINE canonical geometry snapshot missing: " + token)

        for token in (
            'new StringBuilder("QS3D_PLAN_SOURCE_V1|kind=OPEN_POLYLINE|closed=")',
            "polyline.Closed",
            "polyline.Elevation",
            "polyline.Normal",
            "polyline.NumberOfVertices",
            "polyline.GetPoint2dAt(index)",
            "polyline.GetBulgeAt(index)",
            "for (var index = 0; index < polyline.NumberOfVertices; index++)",
            "return HashGeometrySnapshot(canonical);",
        ):
            if token not in polyline:
                errors.append("open POLYLINE canonical geometry snapshot missing: " + token)
        if "index < polyline.NumberOfVertices - 1" in polyline:
            errors.append("open POLYLINE snapshot must include every public vertex bulge, including the terminal slot")

        loop_at = polyline.find("for (var index = 0; index < polyline.NumberOfVertices; index++)")
        vertex_at = polyline.find("polyline.GetPoint2dAt(index)", loop_at)
        bulge_at = polyline.find("polyline.GetBulgeAt(index)", vertex_at)
        hash_at = polyline.find("return HashGeometrySnapshot(canonical);", bulge_at)
        if min(loop_at, vertex_at, bulge_at, hash_at) < 0 or not (loop_at < vertex_at < bulge_at < hash_at):
            errors.append("open POLYLINE fingerprint must serialize every vertex and bulge before hashing")

        for token in (
            'CadGeometryGuard.Finite(value, label).ToString("R", CultureInfo.InvariantCulture)',
            "using (var sha = SHA256.Create())",
            "sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()))",
            'value.ToString("x2", CultureInfo.InvariantCulture)',
        ):
            if token not in helpers:
                errors.append("canonical finite/SHA-256 geometry helper missing: " + token)

if errors:
    print("QS3D Plan-to-3D source geometry freshness preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: PlanTo3D captures deterministic finite SHA-256 snapshots of complete LINE/open-POLYLINE public geometry and revalidates them after project-context resolution before semantic/native mutation.")
