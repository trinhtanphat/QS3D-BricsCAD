#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ShapeRebarGeometryCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DREBAR3DSHAPE", CommandFlags.UsePickSet)]')
    helper = text.find("private static List<ProjectElement> ResolveShapeTargets", start)
    finalize = text.find("private static void FinalizeUi", helper)
    if min(start, helper, finalize) < 0 or not start < helper < finalize:
        errors.append("cannot isolate Shape Rebar single-bind lifecycle")
    else:
        command = text[start:helper]
        tokens = {
            "selection": "CadSelectionGuard.AcquireCurrentSelection(document)",
            "empty": "if (selectedIds.Length == 0)",
            "handles": "var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
            "empty_handles": "if (selectedHandles.Count == 0)",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "preview_targets": "var previewTargets = ResolveShapeTargets(previewProject, selectedHandles);",
            "zero": "if (previewTargets.Count == 0)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "target_ids": "var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);",
            "bind": 'ExistingProjectMutationContext.Require(document, "Shape Rebar 3D")',
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_targets": "var targets = ResolveShapeTargets(project, selectedHandles);",
            "same_targets": "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
            "build": "ShapeRebarSolidBuilder.BuildSelected(document, project)",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Shape Rebar single-bind missing token: " + token)

        ordered = (
            "selection", "empty", "handles", "empty_handles", "readonly", "preview_targets", "zero",
            "project_id", "version", "target_ids", "bind", "fresh_id", "fresh_version",
            "canonical_targets", "same_targets", "build",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Shape Rebar must resolve eligible semantic targets read-only before one canonical bind, revalidate freshness, then call the native builder")

        if command.count("ExistingProjectMutationContext.Require(") != 1:
            errors.append("Shape Rebar wrapper must bind canonical project exactly once")
        if "ProjectContextCoordinator.GetOrCreate(" in command:
            errors.append("Shape Rebar wrapper must not bootstrap project state")

        resolver = text[helper:finalize]
        for token in (
            "project.Elements",
            "x.SourceHandles.Any(selectedHandles.Contains)",
            'x.Properties.TryGetValue("RebarNotation", out var notation)',
            "!string.IsNullOrWhiteSpace(notation)",
            ".OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
            ".ToList();",
        ):
            if token not in resolver:
                errors.append("ResolveShapeTargets missing builder-eligibility token: " + token)
        for forbidden in (
            "ExistingProjectMutationContext",
            "ProjectContextCoordinator.GetOrCreate",
            "ShapeRebarSolidBuilder.BuildSelected",
            "AuditTrail",
            "project.Touch();",
        ):
            if forbidden in resolver:
                errors.append("ResolveShapeTargets must remain read-only: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Shape Rebar mirrors builder eligibility read-only, no-ops zero targets before binding, pins project/version/target IDs, binds once, revalidates, then delegates to the unchanged native builder.")
