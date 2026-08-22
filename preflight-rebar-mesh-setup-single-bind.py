#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RebarMeshSetupCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DREBARMESHSETUP", CommandFlags.UsePickSet)]')
    end = text.find("private static List<ProjectElement> ResolveMeshTargets", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QS3DREBARMESHSETUP")
    else:
        command = text[start:end]
        tokens = {
            "selection": "EntitySnapshotReader.ReadCurrentSelection(document)",
            "handles": "var selectedHandles = new HashSet<string>(",
            "zero_handles": "if (selectedHandles.Count == 0) return;",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "preview_resolve": "var previewMatches = ResolveMeshTargets(previewProject, selectedHandles);",
            "preview_count": "if (previewMatches.Count != 1)",
            "element_id": "var expectedElementId = previewMatches[0].Id;",
            "category": "var expectedCategory = previewMatches[0].Category;",
            "bind": 'ExistingProjectMutationContext.Require(document, "Rebar Mesh Setup")',
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_resolve": "var matches = ResolveMeshTargets(project, selectedHandles);",
            "same_id": "matches[0].Id, expectedElementId",
            "same_category": "matches[0].Category != expectedCategory",
            "window": "new RebarMeshSetupWindow(document, project, element, () =>",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Rebar Mesh Setup missing lifecycle token: " + token)

        ordered = (
            "selection", "handles", "zero_handles", "readonly", "project_id", "version",
            "preview_resolve", "preview_count", "element_id", "category", "bind",
            "fresh_id", "fresh_version", "canonical_resolve", "same_id", "same_category", "window",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Rebar Mesh Setup must resolve one read-only target before canonical bind and revalidate freshness/identity before opening UI")

        if command.count("ExistingProjectMutationContext.Require(") != 1:
            errors.append("Rebar Mesh Setup command must bind canonical mutation context exactly once")
        if "ProjectContextCoordinator.GetOrCreate(" in command:
            errors.append("Rebar Mesh Setup command must not bootstrap project state")

    helper_start = text.find("private static List<ProjectElement> ResolveMeshTargets")
    target_start = text.find("private static bool IsMeshTarget", helper_start)
    if helper_start < 0 or target_start <= helper_start:
        errors.append("missing ResolveMeshTargets helper")
    else:
        helper = text[helper_start:target_start]
        for token in (
            "project.Elements",
            "IsMeshTarget(x)",
            "x.SourceHandles.Any(selectedHandles.Contains)",
            ".Take(3)",
            ".ToList()",
        ):
            if token not in helper:
                errors.append("ResolveMeshTargets missing token: " + token)

    if target_start >= 0:
        target = text[target_start:]
        for token in (
            "ElementCategory.Slab",
            "ElementCategory.StructuralWall",
            "ElementCategory.Foundation",
        ):
            if token not in target:
                errors.append("mesh target category contract missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Rebar Mesh Setup resolves exactly one supported target read-only, no-ops invalid selection before mutation binding, then binds/revalidates canonical state once before opening the modeless editor.")
