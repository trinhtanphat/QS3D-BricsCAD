#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing RebarGeometryCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    required = (
        "CadSelectionGuard.ReadImpliedSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveColumnTargets(previewProject, selectedHandles)",
        "previewProject.ProjectId",
        "previewProject.ChangeVersion",
        "ExistingProjectMutationContext.Require(document, \"Rebar 3D\")",
        "ResolveColumnTargets(project, selectedHandles)",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
        "ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds)",
        "x.Category == ElementCategory.Column",
        "x.SourceHandles.Any(selectedHandles.Contains)",
    )
    for token in required:
        if token not in text:
            errors.append("Column Rebar command missing single-bind/freshness contract: " + token)

    selection = text.find("CadSelectionGuard.ReadImpliedSelection(document)")
    preview = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
    preview_targets = text.find("ResolveColumnTargets(previewProject, selectedHandles)")
    zero_target = text.find("if (previewTargets.Count == 0)")
    bind = text.find("ExistingProjectMutationContext.Require(document, \"Rebar 3D\")")
    revalidate = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Id))")
    build = text.find("ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds)")
    if min(selection, preview, preview_targets, zero_target, bind, revalidate, build) < 0:
        errors.append("Column Rebar lifecycle ordering tokens are incomplete")
    elif not selection < preview < preview_targets < zero_target < bind < revalidate < build:
        errors.append("Column Rebar must resolve zero targets read-only before one canonical bind and revalidate before same-snapshot native build")

    start = text.find("public void BuildRebar3D()")
    helper = text.find("private static List<ProjectElement> ResolveColumnTargets", start)
    body = text[start:helper] if start >= 0 and helper > start else ""
    if body.count("ExistingProjectMutationContext.Require(") != 1:
        errors.append("QS3DREBAR3D must bind the canonical mutation project exactly once")
    if "CadSelectionGuard.AcquireCurrentSelection(document)" in body or "GetSelection(" in body:
        errors.append("QS3DREBAR3D must remain PICKFIRST-only and must not add an interactive selection prompt")
    if "ProjectContextCoordinator.GetOrCreate(" in body:
        errors.append("QS3DREBAR3D must not bootstrap a project directly")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Column Rebar 3D preserves PICKFIRST-only selection, resolves semantic Column targets read-only, no-ops before mutation bind when empty, binds once, revalidates freshness, and passes the admitted snapshot into native build.")
