#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SourceReconcileService.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("public static SourceReconcileResult ReconcileSelection(Document document)")
    resolve_start = text.find("private static List<Target> ResolveTargets", start)
    if start < 0 or resolve_start <= start:
        errors.append("cannot isolate Source Reconcile lifecycle")
    else:
        command = text[start:resolve_start]
        tokens = {
            "selection": "EntitySnapshotReader.ReadCurrentSelection(document)",
            "empty": "if (snapshots.Count == 0) return new SourceReconcileResult();",
            "readonly": "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
            "project_id": "var expectedProjectId = previewProject.ProjectId;",
            "version": "var expectedChangeVersion = previewProject.ChangeVersion;",
            "preview_targets": "var previewTargets = ResolveTargets(previewProject, snapshots);",
            "target_ids": "var expectedTargetIds = new HashSet<string>(",
            "bind": 'ExistingProjectMutationContext.Require(document, "Source Reconcile")',
            "fresh_id": "project.ProjectId, expectedProjectId",
            "fresh_version": "project.ChangeVersion != expectedChangeVersion",
            "canonical_targets": "var targets = ResolveTargets(project, snapshots);",
            "same_targets": "expectedTargetIds.SetEquals(targets.Select(x => x.Element.Id))",
            "snapshot": "ProjectStateSnapshot.Capture(project)",
        }
        positions = {}
        for name, token in tokens.items():
            at = command.find(token)
            positions[name] = at
            if at < 0:
                errors.append("Source Reconcile single-bind missing token: " + token)

        ordered = (
            "selection", "empty", "readonly", "project_id", "version", "preview_targets",
            "target_ids", "bind", "fresh_id", "fresh_version", "canonical_targets",
            "same_targets", "snapshot",
        )
        if all(positions[name] >= 0 for name in ordered):
            values = [positions[name] for name in ordered]
            if values != sorted(values):
                errors.append("Source Reconcile must validate ownership read-only before one canonical bind, then revalidate project/targets before snapshot mutation")

        if command.count('ExistingProjectMutationContext.Require(document, "Source Reconcile")') != 1:
            errors.append("Source Reconcile must bind canonical mutation context exactly once")
        if "ProjectContextCoordinator.GetOrCreate(document)" in command:
            errors.append("Source Reconcile must never bootstrap project state")
        if "project.Touch();" in command:
            errors.append("Source Reconcile must retain AuditTrail-owned revision without standalone project.Touch")

    resolve_end = text.find("private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets", resolve_start)
    if resolve_start < 0 or resolve_end <= resolve_start:
        errors.append("cannot isolate Source Reconcile target resolver")
    else:
        resolver = text[resolve_start:resolve_end]
        required = (
            "GeneratedHandleOwnershipIndex.Build(project)",
            "foreach (var snapshot in snapshots)",
            "generatedOwners.TryFindOwner(snapshot.Handle",
            "SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, snapshot.Handle)",
            "element.SourceHandles.Count != 1",
            "seenElements.Add(element.Id)",
            "targets.Add(new Target { Snapshot = snapshot, Element = element });",
            "return targets;",
        )
        for token in required:
            if token not in resolver:
                errors.append("Source Reconcile read-only resolver missing token: " + token)
        generated_lookup = resolver.find("generatedOwners.TryFindOwner(snapshot.Handle")
        source_lookup = resolver.find("SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, snapshot.Handle)")
        if generated_lookup < 0 or source_lookup < 0 or generated_lookup > source_lookup:
            errors.append("Source Reconcile must reject generated output before canonical source-owner resolution")
        for forbidden in (
            "ExistingProjectMutationContext",
            "ProjectContextCoordinator.GetOrCreate",
            "AuditTrail.ForProject",
            "project.Touch();",
            "MarkDirty(",
            "SetProperty(",
            "BuildSourceOwnerIndex",
            "sourceOwners.TryGetValue",
            "new Dictionary<string, List<ProjectElement>>",
        ):
            if forbidden in resolver:
                errors.append("Source Reconcile ownership resolver must remain read-only: " + forbidden)

    refresh_start = text.find("private static void RefreshSourceDerivedState")
    refresh_end = text.find("private static void UpdateOptionalCadMetadata", refresh_start)
    if refresh_start < 0 or refresh_end <= refresh_start:
        errors.append("cannot isolate Source Reconcile mutation body")
    else:
        refresh = text[refresh_start:refresh_end]
        if 'AuditTrail.ForProject(project).Record("source.reconcile", element.Id' not in refresh:
            errors.append("Source Reconcile must retain per-target source.reconcile AuditTrail revision ownership")
        if "project.Touch();" in refresh:
            errors.append("Source Reconcile mutation body must not add a standalone revision bump")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Source Reconcile validates source ownership read-only, binds canonical state once, revalidates ProjectId/ChangeVersion/target IDs before mutation, and preserves AuditTrail-owned revisions.")
