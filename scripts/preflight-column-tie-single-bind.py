#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ColumnTieQuantityCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DREBARTIEQTY", CommandFlags.UsePickSet)]')
    helper = text.find("private static List<ProjectElement> ResolveColumnTargets", start)
    if start < 0 or helper <= start:
        errors.append("cannot isolate QS3DREBARTIEQTY single-bind lifecycle")
    else:
        command = text[start:helper]
        tokens = {
            "selection": "document.Editor.SelectImplied()",
            "interactive": "document.Editor.GetSelection()",
            "handles": "if (selected.Count == 0) return;",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "preview_targets": "var previewTargets = ResolveColumnTargets(previewProject, selected);",
            "preview_zero": "if (previewTargets.Count == 0)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "target_ids": "var expectedTargetIds = new HashSet<string>(",
            "bind": "ExistingProjectMutationContext.TryGet(document, out var project)",
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_targets": "var targets = ResolveColumnTargets(project, selected);",
            "canonical_zero": "if (targets.Count == 0)",
            "same_targets": "expectedTargetIds.SetEquals(targets.Select(x => x.Id))",
            "snapshot": "ProjectStateSnapshot.Capture(project)",
            "calculate": "ColumnTieProjectQuantityService.Calculate(element, project.FindFamily(element.FamilyId))",
            "audit": 'AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,',
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Tie QTY single-bind missing token: " + token)

        ordered = (
            "selection", "interactive", "handles", "readonly", "preview_targets", "preview_zero",
            "project_id", "version", "target_ids", "bind", "fresh_id", "fresh_version",
            "canonical_targets", "canonical_zero", "same_targets", "snapshot", "calculate", "audit",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Tie QTY must resolve Column targets read-only, no-op zero targets, then canonicalize/revalidate once before quantity mutation")

        if command.count("ExistingProjectMutationContext.TryGet(") != 1:
            errors.append("Tie QTY must canonicalize mutation context exactly once")
        if "ExistingProjectMutationContext.Require(" in command:
            errors.append("Tie QTY must not introduce a second canonical mutation bind")
        if "ProjectContextCoordinator.GetOrCreate(" in command:
            errors.append("Tie QTY must never bootstrap project state")
        if "project.Touch();" in command:
            errors.append("Tie QTY must retain AuditTrail-owned revision semantics")

    helper_start = text.find("private static List<ProjectElement> ResolveColumnTargets")
    finalize = text.find("private static void FinalizeUi", helper_start)
    if helper_start < 0 or finalize <= helper_start:
        errors.append("missing ResolveColumnTargets helper")
    else:
        resolver = text[helper_start:finalize]
        for token in (
            "project.Elements",
            "x.Category == ElementCategory.Column",
            "x.SourceHandles.Any(selected.Contains)",
            ".OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)",
            ".ToList();",
        ):
            if token not in resolver:
                errors.append("ResolveColumnTargets missing token: " + token)
        for forbidden in ("ExistingProjectMutationContext", "ProjectContextCoordinator.GetOrCreate", "AuditTrail", "Quantities["):
            if forbidden in resolver:
                errors.append("ResolveColumnTargets must remain read-only: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Tie QTY resolves Column targets read-only, returns before mutation binding on zero targets, pins project/version/target IDs, canonicalizes once, revalidates, then preserves audit-owned quantity mutation and rollback.")
