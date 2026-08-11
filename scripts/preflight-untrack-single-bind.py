#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ViewportCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private static void UntrackSelectedElements")
    helper = text.find("private static List<string> ResolveUntrackTargetIds", start)
    finalize = text.find("private static void FinalizeUntrackUi", helper)
    if min(start, helper, finalize) < 0 or not start < helper < finalize:
        errors.append("cannot isolate Untrack read-only resolve lifecycle")
    else:
        command = text[start:helper]
        tokens = {
            "selection": "EntitySnapshotReader.ReadImpliedSelection(doc)",
            "handles": "var handles = snapshots.Select(x => x.Handle).ToArray();",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "preview_targets": "var previewTargetIds = ResolveUntrackTargetIds(previewProject, handles, predicate);",
            "zero": "if (previewTargetIds.Count == 0)",
            "zero_ui": "FinalizeUntrackUi(doc, 0, label);",
            "bind": 'ExistingProjectMutationContext.Require(doc, "Untrack semantic elements")',
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "current_targets": "var currentTargetIds = ResolveUntrackTargetIds(project, handles, predicate);",
            "same_targets": "expectedTargets.SetEquals(currentTargetIds)",
            "mutate": "result = SemanticUntrackService.Untrack(project, handles, predicate);",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Untrack single-bind lifecycle missing token: " + token)

        ordered = (
            "selection", "handles", "readonly", "project_id", "version", "preview_targets",
            "zero", "zero_ui", "bind", "fresh_id", "fresh_version", "current_targets",
            "same_targets", "mutate",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Untrack must resolve targets read-only, no-op zero targets, then bind/revalidate once before Core mutation")

        if command.count("ExistingProjectMutationContext.Require(") != 1:
            errors.append("Untrack command must bind canonical mutation context exactly once")
        if "ProjectContextCoordinator.GetOrCreate(" in command:
            errors.append("Untrack command must never bootstrap project state")

        zero_at = positions.get("zero", -1)
        bind_at = positions.get("bind", -1)
        if zero_at >= 0 and bind_at >= 0 and zero_at > bind_at:
            errors.append("zero semantic target must return before canonical mutation binding")

        resolver = text[helper:finalize]
        for token in (
            "SemanticHandleOwnershipResolver.Resolve(project, handles)",
            ".Where(x => predicate == null || predicate(x))",
            ".Select(x => x.Id)",
            ".Distinct(StringComparer.OrdinalIgnoreCase)",
            ".OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
            ".ToList();",
        ):
            if token not in resolver:
                errors.append("Untrack read-only target resolver missing token: " + token)
        for forbidden in ("ExistingProjectMutationContext", "ProjectContextCoordinator.GetOrCreate", "SemanticUntrackService.Untrack"):
            if forbidden in resolver:
                errors.append("Untrack target resolver must remain read-only: " + forbidden)

    for command_name in ("QS3DUNTRACK", "QS3DUNTRACKFINISH"):
        if ('CommandMethod("' + command_name + '"') not in text:
            errors.append("missing semantic untrack command owner: " + command_name)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: semantic untrack resolves ownership read-only, no-ops zero targets before binding, pins ProjectId/ChangeVersion/target IDs, binds once, revalidates, then delegates to the unchanged Core untrack executor.")
