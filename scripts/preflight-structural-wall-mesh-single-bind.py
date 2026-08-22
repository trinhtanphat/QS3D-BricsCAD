#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/StructuralWallMeshCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing StructuralWallMeshCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    required = (
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveWallTargets(previewProject, selectedHandles)",
        "previewProject.ProjectId",
        "previewProject.ChangeVersion",
        "ExistingProjectMutationContext.Require(document, \"Wall Mesh 3D\")",
        "ResolveWallTargets(project, selectedHandles)",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
        "StructuralWallMeshSolidBuilder.BuildSelected(document, project)",
        "x.Category == ElementCategory.StructuralWall",
        "x.SourceHandles.Any(selectedHandles.Contains)",
    )
    for token in required:
        if token not in text:
            errors.append("StructuralWall command missing single-bind/freshness contract: " + token)

    selection = text.find("CadSelectionGuard.AcquireCurrentSelection(document)")
    preview = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
    preview_targets = text.find("ResolveWallTargets(previewProject, selectedHandles)")
    zero_target = text.find("if (previewTargets.Count == 0)")
    bind = text.find("ExistingProjectMutationContext.Require(document, \"Wall Mesh 3D\")")
    revalidate = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Id))")
    build = text.find("StructuralWallMeshSolidBuilder.BuildSelected(document, project)")
    if min(selection, preview, preview_targets, zero_target, bind, revalidate, build) < 0:
        errors.append("StructuralWall command lifecycle ordering tokens are incomplete")
    elif not selection < preview < preview_targets < zero_target < bind < revalidate < build:
        errors.append("StructuralWall command must resolve zero targets read-only before one canonical bind and revalidate before native build")

    start = text.find("public void BuildStructuralWallMesh3D()")
    helper = text.find("private static List<ProjectElement> ResolveWallTargets", start)
    body = text[start:helper] if start >= 0 and helper > start else ""
    if body.count("ExistingProjectMutationContext.Require(") != 1:
        errors.append("QS3DWALLREBAR3D must bind the canonical mutation project exactly once")
    if "ProjectContextCoordinator.GetOrCreate(" in body:
        errors.append("QS3DWALLREBAR3D must not bootstrap a project directly")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Structural Wall Mesh 3D resolves semantic targets read-only, no-ops before mutation bind when empty, binds once, and revalidates freshness before native build.")
