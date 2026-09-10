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
    if "return new BasicDrawingContext(\n                project," not in capture:
        errors.append("CaptureContext must retain the exact ProjectState generation in BasicDrawingContext")

fresh_start = source.find("private static void RequireFreshContext(")
fresh_end = source.find("private static Polyline CreateRectangle(", fresh_start if fresh_start >= 0 else 0)
fresh = source[fresh_start:fresh_end if fresh_end >= 0 else len(source)] if fresh_start >= 0 else ""
if fresh_start < 0:
    errors.append("missing BasicDrawingCommands.RequireFreshContext")
else:
    reacquire = fresh.find("if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))")
    exact = fresh.find("if (!ReferenceEquals(project, expected.Project))", reacquire if reacquire >= 0 else 0)
    semantic = fresh.find("if (!string.Equals(project.ProjectId, expected.ProjectId, StringComparison.OrdinalIgnoreCase)", reacquire if reacquire >= 0 else 0)
    family = fresh.find("var family = ProjectFamilyActivationService.GetActive(project);", reacquire if reacquire >= 0 else 0)
    if min(reacquire, exact, semantic, family) < 0:
        errors.append("RequireFreshContext must contain exact ProjectState and semantic freshness fences")
    elif not (reacquire < exact < semantic < family):
        errors.append("exact ProjectState generation fence must run immediately after project reacquisition and before semantic/family checks")

context_start = source.find("private sealed class BasicDrawingContext")
context = source[context_start:] if context_start >= 0 else ""
if context_start < 0:
    errors.append("missing BasicDrawingContext")
else:
    required = [
        "ProjectState project,",
        "Project = project ?? throw new ArgumentNullException(nameof(project));",
        "public ProjectState Project { get; }",
    ]
    for needle in required:
        if needle not in context:
            errors.append("BasicDrawingContext exact-project contract missing: " + needle)

print("QS3D V25 Basic Drawing exact-project-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Basic Drawing binds pre-commit geometry handoff to the exact captured ProjectState generation before semantic freshness checks.")
