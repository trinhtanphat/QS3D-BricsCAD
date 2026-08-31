#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing BeamRebarCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    required = (
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveBeamTargets(previewProject, selectedHandles)",
        "previewProject.ProjectId",
        "previewProject.ChangeVersion",
        "ExistingProjectMutationContext.Require(document, \"Beam Rebar 3D\")",
        "ResolveBeamTargets(project, selectedHandles)",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
        "BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)",
        "x.Category == ElementCategory.Beam",
        "x.SourceHandles.Any(selectedHandles.Contains)",
    )
    for token in required:
        if token not in text:
            errors.append("Beam Rebar command missing single-bind/freshness/snapshot contract: " + token)

    selection = text.find("CadSelectionGuard.AcquireCurrentSelection(document)")
    preview = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
    preview_targets = text.find("ResolveBeamTargets(previewProject, selectedHandles)")
    zero_target = text.find("if (previewTargets.Count == 0)")
    bind = text.find("ExistingProjectMutationContext.Require(document, \"Beam Rebar 3D\")")
    revalidate = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Id))")
    build = text.find("BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)")
    if min(selection, preview, preview_targets, zero_target, bind, revalidate, build) < 0:
        errors.append("Beam Rebar lifecycle ordering tokens are incomplete")
    elif not selection < preview < preview_targets < zero_target < bind < revalidate < build:
        errors.append("Beam Rebar must resolve zero targets read-only before one canonical bind, revalidate, then pass the admitted selection snapshot to native build")

    start = text.find("public void BuildBeamRebar3D()")
    helper = text.find("private static List<ProjectElement> ResolveBeamTargets", start)
    body = text[start:helper] if start >= 0 and helper > start else ""
    if body.count("ExistingProjectMutationContext.Require(") != 1:
        errors.append("QS3DBEAMREBAR3D must bind the canonical mutation project exactly once")
    if "ProjectContextCoordinator.GetOrCreate(" in body:
        errors.append("QS3DBEAMREBAR3D must not bootstrap a project directly")
    if "BeamRebarSolidBuilder.BuildSelected(document, project)" in body:
        errors.append("QS3DBEAMREBAR3D must not drop the admitted selection snapshot before native build")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Beam Rebar 3D resolves semantic Beam targets read-only, no-ops before mutation bind when empty, binds once, revalidates freshness, and passes the exact admitted selection snapshot into native build.")
