#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PlanTo3DCommands.cs"
DOC = ROOT / "docs" / "PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
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
        "var selectionUnit = CadUnitService.GetLengthUnit(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var defaultsProject)",
        "var expectedProjectId = hasDefaultsProject ? defaultsProject.ProjectId : null",
        "PromptPositiveMeters",
        "PromptFiniteMeters",
        "CadUnitService.GetLengthUnit(document) != selectionUnit",
        "var refreshedSources = PreflightSources(document, selectedIds)",
        "RequireSameSources(sources, refreshedSources)",
        "ExistingProjectMutationContext.Require(document, operation)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out _)",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "RequireFreshSources(project, sources)",
        "ProjectStateSnapshot.Capture(project)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("ConvertPlanWalls missing preview/commit freshness lifecycle token")
    else:
        initial_space, selection, initial_preflight, unit, preview, expected_id, prompt_positive, prompt_finite, unit_check, refreshed, same_sources, require_existing, appeared_project, create_project, ownership, snapshot = positions
        if not (initial_space < selection < initial_preflight < unit < preview < expected_id < prompt_positive < prompt_finite):
            errors.append("2D conversion must establish drawing/source/project defaults before user parameter prompts")
        if not (prompt_finite < unit_check < refreshed < same_sources < require_existing < appeared_project < create_project < ownership < snapshot):
            errors.append("2D conversion must revalidate drawing/source/project context before snapshot or semantic/native mutation")
        if body.count("RequireModelSpace(document)") < 2:
            errors.append("2D conversion must re-check Model Space/UCS after prompts")

for token in (
    "private static void RequireSameSources",
    "left.Id.Equals(right.Id)",
    "left.Kind != right.Kind",
    "string.Equals(left.Handle, right.Handle, StringComparison.OrdinalIgnoreCase)",
    "QS3D project đã thay đổi trong lúc xác nhận 2D -> 3D",
    "QS3D project đã xuất hiện trong lúc xác nhận 2D -> 3D",
    "Drawing unit policy đã thay đổi trong lúc xác nhận 2D -> 3D",
    "GeneratedGeometryService.FindMatchingOwnedHandles",
    "GeneratedGeometryService.HasMatchingOwnership",
    "rollback.Restore(project)",
):
    if token not in source:
        errors.append("2D conversion freshness/rollback contract missing: " + token)

for forbidden in (
    "new ProjectState(",
    "ProjectContextCoordinator.GetOrCreate(document);\n                RequireFreshSources(project, sources);\n                var rollback",
):
    if forbidden in body:
        errors.append("2D conversion regressed to an unsafe project bootstrap/mutation boundary: " + forbidden)

for token in (
    "preview-to-commit",
    "same `ProjectId`",
    "project appears",
    "re-preflight",
    "LOCAL-008",
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
        errors.append("LOCAL-008 2D conversion handoff missing: " + token)

if errors:
    print("QS3D Plan-to-3D project lifecycle preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DCONVERT2D/QS3DPLAN2WALLS revalidate active drawing, Model Space/UCS, units, exact selected sources and canonical project identity before snapshot/mutation while preserving ownership-scoped batch compensation.")
