#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing SlabMeshCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    required = (
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveSlabTargets(previewProject, selectedHandles)",
        "previewProject.ProjectId",
        "previewProject.ChangeVersion",
        "ExistingProjectMutationContext.Require(document, \"Slab Mesh 3D\")",
        "ResolveSlabTargets(project, selectedHandles)",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
        "SlabMeshSolidBuilder.BuildSelected(document, project)",
        "x.Category == ElementCategory.Slab",
        "x.SourceHandles.Any(selectedHandles.Contains)",
    )
    for token in required:
        if token not in text:
            errors.append("Slab command missing single-bind/freshness contract: " + token)

    selection = text.find("CadSelectionGuard.AcquireCurrentSelection(document)")
    preview = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
    preview_targets = text.find("ResolveSlabTargets(previewProject, selectedHandles)")
    zero_target = text.find("if (previewTargets.Count == 0)")
    bind = text.find("ExistingProjectMutationContext.Require(document, \"Slab Mesh 3D\")")
    revalidate = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Id))")
    build = text.find("SlabMeshSolidBuilder.BuildSelected(document, project)")
    if min(selection, preview, preview_targets, zero_target, bind, revalidate, build) < 0:
        errors.append("Slab command lifecycle ordering tokens are incomplete")
    elif not selection < preview < preview_targets < zero_target < bind < revalidate < build:
        errors.append("Slab command must resolve zero targets read-only before one canonical bind and revalidate before native build")

    command_start = text.find("public void BuildSlabMesh3D()")
    health_start = text.find("[CommandMethod(\"QS3DSLABREBARHEALTH\"", command_start)
    command = text[command_start:health_start] if command_start >= 0 and health_start > command_start else ""
    if command.count("ExistingProjectMutationContext.Require(") != 1:
        errors.append("QS3DSLABREBAR3D must bind the canonical mutation project exactly once")
    if "ProjectContextCoordinator.GetOrCreate(" in command:
        errors.append("QS3DSLABREBAR3D must not bootstrap a project directly")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Slab Mesh 3D resolves semantic targets read-only, no-ops before mutation bind when empty, binds once, and revalidates freshness before native build.")
