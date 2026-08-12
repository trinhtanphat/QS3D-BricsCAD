#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs"
errors = []

if not PATH.is_file():
    errors.append("missing BeamStirrupCommands.cs")
else:
    text = PATH.read_text(encoding="utf-8")
    required = (
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "ResolveBeamTargets(previewProject, selectedHandles)",
        "previewProject.ProjectId",
        "previewProject.ChangeVersion",
        "ExistingProjectMutationContext.Require(document, \"Beam Stirrup 3D\")",
        "ResolveBeamTargets(project, selectedHandles)",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
        "BeamStirrupSolidBuilder.BuildSelected(document, project)",
        "x.Category == ElementCategory.Beam",
        "x.SourceHandles.Any(selectedHandles.Contains)",
    )
    for token in required:
        if token not in text:
            errors.append("Beam Stirrup command missing single-bind/freshness contract: " + token)

    selection = text.find("CadSelectionGuard.AcquireCurrentSelection(document)")
    preview = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)")
    preview_targets = text.find("ResolveBeamTargets(previewProject, selectedHandles)")
    zero_target = text.find("if (previewTargets.Count == 0)")
    bind = text.find("ExistingProjectMutationContext.Require(document, \"Beam Stirrup 3D\")")
    revalidate = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Id))")
    build = text.find("BeamStirrupSolidBuilder.BuildSelected(document, project)")
    if min(selection, preview, preview_targets, zero_target, bind, revalidate, build) < 0:
        errors.append("Beam Stirrup lifecycle ordering tokens are incomplete")
    elif not selection < preview < preview_targets < zero_target < bind < revalidate < build:
        errors.append("Beam Stirrup must resolve zero targets read-only before one canonical bind and revalidate before native build")

    start = text.find("public void BuildBeamStirrups()")
    health = text.find("[CommandMethod(\"QS3DBEAMSTIRRUPHEALTH\"", start)
    body = text[start:health] if start >= 0 and health > start else ""
    if body.count("ExistingProjectMutationContext.Require(") != 1:
        errors.append("QS3DREBARSTIRRUP3D must bind the canonical mutation project exactly once")
    if "ProjectContextCoordinator.GetOrCreate(" in body:
        errors.append("QS3DREBARSTIRRUP3D must not bootstrap a project directly")
    for alias in ('CommandMethod("QS3DBEAMSTIRRUP3D"', 'CommandMethod("QS3DREBARSTIRRUP3D"'):
        if alias not in text:
            errors.append("Beam Stirrup command alias missing: " + alias)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Beam Stirrup 3D preserves aliases, resolves semantic Beam targets read-only, no-ops before mutation bind when empty, binds once, and revalidates freshness before native build.")
