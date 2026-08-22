#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
PREVIEW_CONTEXT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "DirectDrawProjectPreviewContext.cs"
DOC = ROOT / "docs" / "PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
preview_context = read(PREVIEW_CONTEXT)
doc = read(DOC)
inbox = read(INBOX)

start = source.find("private static void ConvertPlanWalls")
end = source.find("private static IReadOnlyList<ObjectId>? AcquireSelection", start + 1) if start >= 0 else -1
body = source[start:end] if start >= 0 and end > start else ""
if not body:
    errors.append("cannot isolate ConvertPlanWalls")
else:
    tokens = (
        "RequireModelSpace(document)",
        "AcquireSelection(document)",
        "PreflightSources(document, selectedIds)",
        "DirectDrawProjectPreviewContext.Capture(document)",
        "var defaultsProject = projectPreview.DefaultsProject",
        "PromptPositiveMeters",
        "PromptFiniteMeters",
        "var refreshedSources = PreflightSources(document, selectedIds)",
        "RequireSameSources(sources, refreshedSources)",
        "projectPreview.ResolveForMutation(document, operation)",
        "RequireFreshSources(project, sources)",
        "ProjectStateSnapshot.Capture(project)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("ConvertPlanWalls missing guarded preview/commit freshness lifecycle token")
    else:
        initial_space, selection, initial_preflight, preview, defaults, prompt_positive, prompt_finite, refreshed, same_sources, resolve, ownership, snapshot = positions
        if not (initial_space < selection < initial_preflight < preview < defaults < prompt_positive < prompt_finite):
            errors.append("2D conversion must snapshot drawing/project defaults before user parameter prompts")
        if not (prompt_finite < refreshed < same_sources < resolve < ownership < snapshot):
            errors.append("2D conversion must revalidate exact sources and resolve the guarded project preview before snapshot or semantic/native mutation")
        if body.count("RequireModelSpace(document)") < 2:
            errors.append("2D conversion must re-check Model Space/UCS after prompts")

    for forbidden in (
        "var expectedProjectId =",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var defaultsProject)",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ExistingProjectMutationContext.Require(document, operation)",
    ):
        if forbidden in body:
            errors.append("2D conversion must not duplicate the shared preview mutation bridge: " + forbidden)

for token in (
    "public string GeometryFingerprint { get; set; } = string.Empty;",
    "GeometryFingerprint = BuildLineGeometryFingerprint(line)",
    "GeometryFingerprint = BuildOpenPolylineGeometryFingerprint(polyline)",
    "private static void RequireSameSources",
    "left.Id.Equals(right.Id)",
    "left.Kind != right.Kind",
    "string.Equals(left.Handle, right.Handle, StringComparison.OrdinalIgnoreCase)",
    "string.IsNullOrWhiteSpace(left.GeometryFingerprint)",
    "string.IsNullOrWhiteSpace(right.GeometryFingerprint)",
    "string.Equals(left.GeometryFingerprint, right.GeometryFingerprint, StringComparison.Ordinal)",
    "GeneratedGeometryService.FindMatchingOwnedHandles",
    "GeneratedGeometryService.HasMatchingOwnership",
    "rollback.Restore(project)",
):
    if token not in source:
        errors.append("2D conversion freshness/rollback contract missing: " + token)

for token in (
    "ExpectedProjectId",
    "ExpectedChangeVersion",
    "ExpectedLengthUnit",
    "ExpectedUcs",
    "project.ChangeVersion != ExpectedChangeVersion.Value",
    "CadUnitService.GetLengthUnit(document) != ExpectedLengthUnit",
    "CurrentUserCoordinateSystem.Equals(ExpectedUcs)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out _) || HasBackingStore(document)",
    "ProjectContextCoordinator.Forget(document)",
):
    if token not in preview_context:
        errors.append("shared Direct Draw preview context missing lifecycle invariant required by Plan-to-3D: " + token)

for forbidden in (
    "new ProjectState(",
    "ProjectContextCoordinator.GetOrCreate(document);\n                RequireFreshSources(project, sources);\n                var rollback",
):
    if forbidden in body:
        errors.append("2D conversion regressed to an unsafe project bootstrap/mutation boundary: " + forbidden)

for token in (
    "preview-to-commit",
    "same `ProjectId`",
    "ChangeVersion",
    "project appears",
    "re-preflight",
    "LOCAL-014",
):
    if token not in doc:
        errors.append("PLAN-TO-3D workflow documentation missing: " + token)

for token in (
    "QS3DCONVERT2D",
    "preview-to-commit freshness",
    "project appears",
    "Model Space/UCS",
    "source eligibility",
):
    if token not in inbox:
        errors.append("LOCAL-014 2D conversion handoff missing: " + token)

if errors:
    print("QS3D Plan-to-3D project lifecycle preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DCONVERT2D/QS3DPLAN2WALLS snapshot the shared guarded preview, revalidate exact source geometry, and reject stale project identity/version, unit policy, UCS, or appeared backing store before snapshot/mutation while preserving ownership-scoped compensation.")
