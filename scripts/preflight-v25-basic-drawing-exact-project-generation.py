#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

capture_start = source.find("private static BasicDrawingContext CaptureContext(")
capture_end = source.find("private static string RequireCanonicalIdentity(", capture_start if capture_start >= 0 else 0)
capture = source[capture_start:capture_end if capture_end >= 0 else len(source)] if capture_start >= 0 else ""
if capture_start < 0:
    errors.append("missing BasicDrawingCommands.CaptureContext")
else:
    cache_probe = capture.find("var hasCachedProject = ProjectContextCoordinator.TryGetCached(document, out var cachedProject);")
    cache_match = capture.find("if (hasCachedProject && !ReferenceEquals(cachedProject, project))", cache_probe if cache_probe >= 0 else 0)
    context_capture = capture.find("return new BasicDrawingContext(\n                project,\n                hasCachedProject,", cache_match if cache_match >= 0 else 0)
    if min(cache_probe, cache_match, context_capture) < 0:
        errors.append("CaptureContext must snapshot cached project authority and retain it with the captured ProjectState")
    elif not (cache_probe < cache_match < context_capture):
        errors.append("CaptureContext cached-authority validation must precede BasicDrawingContext construction")

fresh_start = source.find("private static void RequireFreshContext(")
fresh_end = source.find("private static Polyline CreateRectangle(", fresh_start if fresh_start >= 0 else 0)
fresh = source[fresh_start:fresh_end if fresh_end >= 0 else len(source)] if fresh_start >= 0 else ""
if fresh_start < 0:
    errors.append("missing BasicDrawingCommands.RequireFreshContext")
else:
    reacquire = fresh.find("if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))")
    cache_probe = fresh.find("var hasCachedProject = ProjectContextCoordinator.TryGetCached(document, out var cachedProject);", reacquire if reacquire >= 0 else 0)
    transition = fresh.find("if (hasCachedProject != expected.HasCachedProject ||", cache_probe if cache_probe >= 0 else 0)
    cached_expected = fresh.find("!ReferenceEquals(cachedProject, expected.Project)", transition if transition >= 0 else 0)
    read_expected = fresh.find("!ReferenceEquals(project, expected.Project)", cached_expected if cached_expected >= 0 else 0)
    semantic = fresh.find("if (!string.Equals(project.ProjectId, expected.ProjectId, StringComparison.OrdinalIgnoreCase)", read_expected if read_expected >= 0 else 0)
    family = fresh.find("var family = ProjectFamilyActivationService.GetActive(project);", semantic if semantic >= 0 else 0)
    if min(reacquire, cache_probe, transition, cached_expected, read_expected, semantic, family) < 0:
        errors.append("RequireFreshContext must fence cached project-generation transitions before semantic/family checks")
    elif not (reacquire < cache_probe < transition < cached_expected < read_expected < semantic < family):
        errors.append("cached project-generation fence must run immediately after project reacquisition and before semantic/family checks")
    if "if (!ReferenceEquals(project, expected.Project))" in fresh:
        errors.append("uncached sidecar rehydration must not be rejected by unconditional ProjectState reference equality")

context_start = source.find("private sealed class BasicDrawingContext")
context = source[context_start:] if context_start >= 0 else ""
if context_start < 0:
    errors.append("missing BasicDrawingContext")
else:
    required = [
        "ProjectState project,",
        "bool hasCachedProject,",
        "Project = project ?? throw new ArgumentNullException(nameof(project));",
        "HasCachedProject = hasCachedProject;",
        "public ProjectState Project { get; }",
        "public bool HasCachedProject { get; }",
    ]
    for needle in required:
        if needle not in context:
            errors.append("BasicDrawingContext cached-project contract missing: " + needle)

print("QS3D V25 Basic Drawing cached project-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Basic Drawing rejects authoritative cached project-generation changes while preserving semantic fallback for uncached sidecar rehydration.")
