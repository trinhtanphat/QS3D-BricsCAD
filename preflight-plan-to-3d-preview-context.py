#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing PlanTo3DCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        "DirectDrawProjectPreviewContext.Capture(document)",
        "var defaultsProject = projectPreview.DefaultsProject;",
        "projectPreview.ResolveForMutation(document, operation)",
        "RequireSameSources(sources, refreshedSources);",
        "RequireFreshSources(project, sources);",
        "RegenerateDirtySubset(project, new[] { element.Id })",
    )
    for token in required:
        if token not in text:
            errors.append("missing Plan-to-3D lifecycle token: " + token)

    forbidden = (
        "var expectedProjectId =",
        "var hasDefaultsProject = ProjectContextCoordinator.TryGetReadOnly",
        "project = ProjectContextCoordinator.GetOrCreate(document);",
    )
    for token in forbidden:
        if token in text:
            errors.append("Plan-to-3D must not duplicate the shared project-preview mutation bridge: " + token)

    capture = text.find("DirectDrawProjectPreviewContext.Capture(document)")
    prompt = text.find("PromptPositiveMeters(document.Editor")
    resolve = text.find("projectPreview.ResolveForMutation(document, operation)")
    source_refresh = text.find("RequireSameSources(sources, refreshedSources);")
    mutation = text.find("ProjectStateSnapshot.Capture(project)")
    if min(capture, resolve, source_refresh, mutation) < 0:
        pass
    elif not (capture < source_refresh < resolve < mutation):
        errors.append("project preview must be captured before prompts/source refresh and resolved only after source revalidation, before mutation")
    if prompt >= 0 and capture >= prompt:
        errors.append("advanced prompt flow must snapshot the project preview before reading user-entered wall values")

    if 'try { document.Editor.WriteMessage("\\n" + operation + " error: " + ex.Message); }' not in text:
        errors.append("Guard must report editor errors best-effort without masking the original operation failure")
    if 'try { PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); }' not in text:
        errors.append("Guard must report palette status best-effort without masking the original operation failure")

print("QS3D Plan-to-3D project preview lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Plan-to-3D reuses the guarded project preview context and revalidates sources before mutation.")
