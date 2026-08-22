#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing OpeningBooleanCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DCUTSELECTEDOPENINGS", CommandFlags.UsePickSet)]')
    end = text.find("private static void Execute(", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QS3DCUTSELECTEDOPENINGS")
    else:
        selected = text[start:end]
        tokens = {
            "selection": "EntitySnapshotReader.ReadCurrentSelection(document)",
            "handles": "var handles = new HashSet<string>(",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "preview_resolve": "var openingIds = ResolveOpeningIds(previewProject, handles);",
            "zero": "if (openingIds.Count == 0)",
            "bind": 'ExistingProjectMutationContext.Require(document, "Selected physical opening cut")',
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_resolve": "var currentOpeningIds = ResolveOpeningIds(project, handles);",
            "same_targets": "expectedTargets.SetEquals(currentOpeningIds)",
            "execute": 'Execute(document, openingIds, "QS3DCUTSELECTEDOPENINGS", "Physical opening chọn", project);',
        }
        positions = {}
        for name, token in tokens.items():
            at = selected.find(token)
            positions[name] = at
            if at < 0:
                errors.append("selected opening cut missing lifecycle token: " + token)

        ordered = (
            "selection", "handles", "readonly", "project_id", "version",
            "preview_resolve", "zero", "bind", "fresh_id", "fresh_version",
            "canonical_resolve", "same_targets", "execute",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("selected opening cut must resolve read-only targets, no-op zero targets, then bind/revalidate exactly once before execution")

        if selected.count("ExistingProjectMutationContext.Require(") != 1:
            errors.append("QS3DCUTSELECTEDOPENINGS must bind canonical mutation context exactly once")
        if "ProjectContextCoordinator.GetOrCreate(" in selected:
            errors.append("QS3DCUTSELECTEDOPENINGS must not bootstrap project state")

    execute_start = text.find("private static void Execute(")
    resolve_start = text.find("private static IReadOnlyList<string> ResolveOpeningIds", execute_start)
    if execute_start < 0 or resolve_start <= execute_start:
        errors.append("cannot isolate shared opening Execute helper")
    else:
        execute = text[execute_start:resolve_start]
        required = (
            "ProjectState? boundProject = null",
            "var project = boundProject ?? ExistingProjectMutationContext.Require(document, label);",
            "OpeningBooleanCutGuard.RequireFreshGeneratedHosts(project, null);",
            "OpeningBooleanCutGuard.RequireSelectedTargetsReady(document, project, openingIds);",
            "OpeningBooleanService.CutLinkedOpenings(document, project)",
            "OpeningBooleanService.CutLinkedOpenings(document, project, openingIds)",
        )
        for token in required:
            if token not in execute:
                errors.append("shared opening Execute helper missing token: " + token)
        if execute.count("ExistingProjectMutationContext.Require(") != 1:
            errors.append("shared opening Execute helper must contain only the null-bound fallback canonical bind")

    all_start = text.find('[CommandMethod("QS3DCUTOPENINGS", CommandFlags.Modal)]')
    selected_start = text.find('[CommandMethod("QS3DCUTSELECTEDOPENINGS", CommandFlags.UsePickSet)]')
    if all_start < 0 or selected_start <= all_start:
        errors.append("cannot isolate QS3DCUTOPENINGS")
    else:
        all_command = text[all_start:selected_start]
        if 'Execute(document, null, "QS3DCUTOPENINGS", "Physical opening");' not in all_command:
            errors.append("all-opening cut routing must remain unchanged")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: selected Opening Boolean resolves targets read-only, no-ops zero targets, binds canonical project once, revalidates project/version/target set, then reuses that bound project for physical cut.")
