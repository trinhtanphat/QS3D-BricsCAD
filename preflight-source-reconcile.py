#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

service = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileCommands.cs"
ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs"
engine = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
dependency_graph = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
ownership_index = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipIndex.cs"
regen_smoke = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationSubsetSmoke.cs"
ownership_smoke = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedHandleOwnershipIndexSmoke.cs"
direct_dependency_smoke = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphDirectDependentsSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
doc = ROOT / "docs/SOURCE-EDIT-WORKFLOW.md"

checks = {
    service: [
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "var expectedProjectId = previewProject.ProjectId;",
        "var expectedChangeVersion = previewProject.ChangeVersion;",
        "var previewTargets = ResolveTargets(previewProject, snapshots);",
        "var expectedTargetIds = new HashSet<string>(",
        "ExistingProjectMutationContext.Require(document, \"Source Reconcile\")",
        "project.ChangeVersion != expectedChangeVersion",
        "expectedTargetIds.SetEquals(targets.Select(x => x.Element.Id))",
        "var generatedOwners = GeneratedHandleOwnershipIndex.Build(project);",
        "var sourceOwners = BuildSourceOwnerIndex(project);",
        "generatedOwners.TryFindOwner(snapshot.Handle, out var generatedOwner, out var generatedSlot)",
        "sourceOwners.TryGetValue(snapshot.Handle, out var matches)",
        "BuildSourceOwnerIndex(ProjectState project)",
        "new Dictionary<string, List<ProjectElement>>(StringComparer.OrdinalIgnoreCase)",
        "is QS3D-generated output owned by",
        "Select the authoritative source CAD instead.",
        "Source reconcile P0 requires exactly one authoritative source handle per semantic element",
        "ExpandInvalidationTargets",
        "graph.Rebuild(project.Elements);",
        "graph.GetDirectDependents(current.Id)",
        "graph.TryGetElement(dependentId, out var dependent)",
        "new Queue<ProjectElement>()",
        "EnqueueOpeningHost(current, graph, result, queue)",
        "EnqueueInvalidationTarget(dependent, result, queue)",
        "GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, invalidationTargets)",
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "RefreshSourceDerivedState",
        "dependent.MarkDirty(ElementDirtyFlags.All)",
        "RegenerateAffectedToStable",
        "MaxStableRegenerationPasses = 8",
        "affected.Count(HasSemanticDirty)",
        "engine.RegenerateDirtySubset(project, affectedIds)",
        "affected\n                .Where(HasSemanticDirty)",
        "invalidation.CommitMetadata()",
        "transaction.Commit();",
        "cadCommitted = true;",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
        'element.SetProperty("LengthM"',
        'element.SetProperty("AreaM2"',
        'element.SetProperty("PerimeterM"',
        '.Where(x => x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase))',
        "element.Properties.Remove(key);",
        "element.SetProperty(MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
        "element.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty",
        'element.SetProperty("CAD.SolidMetricSource", "Solid3d.MassProperties")',
        'element.Properties.Remove("VolumeM3")',
        "element.MarkDirty(ElementDirtyFlags.All)",
        'AuditTrail.ForProject(project).Record("source.reconcile"',
        "Opening " + '" + element.Id + " references missing host "',
        "did not converge within",
    ],
    command: [
        'CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)',
        "SourceReconcileService.ReconcileSelection(document)",
        "FinalizeUi(document, result)",
        "Generated host/rebar/curtain phụ thuộc đã được invalidate/remove an toàn",
        "UI sync warning: ",
        "ReportOperationFailure",
        "TryWriteMessage",
    ],
    ribbon: [
        '"QS3D_PROJECT_SYNCSOURCE"',
        '"Đồng bộ source CAD"',
        '"QS3DSYNCSOURCE"',
    ],
    engine: [
        "RegenerateDirtySubset(ProjectState project, IEnumerable<string> elementIds)",
        "var inputVersion = project.ChangeVersion;",
        "var sourceElements = project.Elements.ToArray();",
        "var unresolved = CanonicalTargetIds(elementIds, sourceElements.Length);",
        "RequireElementStructureFresh(project, sourceElements);",
        "private static HashSet<string> CanonicalTargetIds(IEnumerable<string> elementIds, int maxCount)",
        "Regeneration target id cannot be blank",
        "Regeneration target id must be canonical without surrounding whitespace",
        "Duplicate regeneration target id",
        "Regeneration target set cannot exceed project element count",
        "var targets = new List<ProjectElement>(unresolved.Count);",
        "var seenProjectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
        "foreach (var element in sourceElements)",
        "if (!seenProjectIds.Add(element.Id))",
        "if (unresolved.Remove(element.Id)) targets.Add(element);",
        "Unknown regeneration target: ",
        "ValidateSubsetDependencyExistence(targets, seenProjectIds);",
        "return RegenerateTransactional(project, targets, targets.Count);",
        "var dirty = _graph.TopologicalDirtyOrder(candidateList);",
        "_graph.TryGetElement(normalizedId, out var source)",
    ],
    dependency_graph: [
        "public IReadOnlyList<string> GetDirectDependents(string sourceId)",
        "public bool TryGetElement(string elementId, out ProjectElement? element)",
        "_elementsById.TryGetValue(normalized, out element)",
        "_dependents.TryGetValue(normalized, out var dependents)",
        "dependents.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)",
    ],
    ownership_index: [
        "public sealed class GeneratedHandleOwnershipIndex",
        "public static GeneratedHandleOwnershipIndex Build(ProjectState project)",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots",
        "Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase)",
        "public bool TryFindOwner(string handle, out ProjectElement? owner, out string propertyKey)",
        "if (entry.Ambiguity != null) throw new InvalidOperationException(entry.Ambiguity);",
    ],
    regen_smoke: [
        "RegeneratesOnlyRequestedElements",
        "RejectsMalformedRequestedIds",
        "RejectsUnknownTarget",
        "RejectsDuplicateProjectIds",
        "engine.RegenerateDirtySubset(project, new[] { selected.Id })",
        'Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { " Selected " }));',
        'Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { "Selected", "selected" }));',
        'Throws<ArgumentException>(() => engine.RegenerateDirtySubset(project, new[] { string.Empty }));',
        "var dirtyBefore = selected.Dirty;",
        "Equal(dirtyBefore, selected.Dirty);",
        "True(unrelated.Dirty != ElementDirtyFlags.None)",
        'True(!unrelated.Quantities.ContainsKey("Count"))',
        "Throws<KeyNotFoundException>",
        "Throws<InvalidOperationException>",
    ],
    ownership_smoke: [
        "ResolvesCaseInsensitiveTrimmedHandle",
        "SameLogicalAliasOnSameOwnerIsAllowed",
        "DifferentOwnersFailClosed",
        "DifferentLogicalSlotsOnSameOwnerFailClosed",
        "BuiltIndexIsMembershipSnapshot",
    ],
    direct_dependency_smoke: [
        "DirectLookupIsDeterministicAndNonTransitive",
        "LookupNormalizesSourceId",
        "ElementLookupNormalizesAndRetainsReference",
        "FailedDuplicateRebuildPreservesPreviousIndex",
        "MissingSourceIsEmpty",
    ],
    registration: [
        "RegenerationSubsetSmoke.Run();",
        "GeneratedHandleOwnershipIndexSmoke.Run();",
        "DependencyGraphDirectDependentsSmoke.Run();",
    ],
    doc: [
        "`QS3DSYNCSOURCE`",
        "native BricsCAD source edits",
        "ownership-safe generated invalidation/removal",
        "does **not** silently regenerate destructive/native downstream geometry",
        "source-implemented / statically guarded; licensed V25 interactive qualification pending",
    ],
}

for path, needles in checks.items():
    if not path.is_file():
        errors.append("missing source reconcile dependency: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing source reconcile contract: " + needle)

if service.is_file():
    text = service.read_text(encoding="utf-8")

    selection = text.find("EntitySnapshotReader.ReadCurrentSelection(document)")
    empty = text.find("if (snapshots.Count == 0) return new SourceReconcileResult();", selection)
    readonly = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)", empty)
    preview_resolve = text.find("var previewTargets = ResolveTargets(previewProject, snapshots);", readonly)
    bind = text.find('var project = ExistingProjectMutationContext.Require(document, "Source Reconcile");', preview_resolve)
    freshness = text.find("project.ChangeVersion != expectedChangeVersion", bind)
    canonical_resolve = text.find("var targets = ResolveTargets(project, snapshots);", freshness)
    target_freshness = text.find("expectedTargetIds.SetEquals(targets.Select(x => x.Element.Id))", canonical_resolve)
    snapshot = text.find("ProjectStateSnapshot.Capture(project)", target_freshness)
    if min(selection, empty, readonly, preview_resolve, bind, freshness, canonical_resolve, target_freshness, snapshot) < 0 or not (
        selection < empty < readonly < preview_resolve < bind < freshness < canonical_resolve < target_freshness < snapshot
    ):
        errors.append("Source reconcile must complete selection, validate ownership read-only, bind canonical project once, revalidate project/target freshness, then snapshot before mutation")
    if text.count("ExistingProjectMutationContext.Require(document, \"Source Reconcile\")") != 1:
        errors.append("Source reconcile must bind canonical mutation context exactly once")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("Source reconcile must not bootstrap project state")

    prepare = text.find("GeneratedDependentGeometryInvalidator.Prepare")
    refresh = text.find("RefreshSourceDerivedState(project")
    regen = text.find("RegenerateAffectedToStable(project")
    metadata = text.find("invalidation.CommitMetadata()")
    commit = text.find("transaction.Commit();", metadata)
    flag = text.find("cadCommitted = true;", commit)
    restore = text.find("rollback.Restore(project)", flag)
    if min(prepare, refresh, regen, metadata, commit, flag, restore) < 0 or not (prepare < refresh < regen < metadata < commit < flag < restore):
        errors.append("Source reconcile must invalidate CAD -> refresh authoritative semantic state -> converge regeneration -> commit generated metadata -> CAD commit, with project restore only on pre-commit failure")
    if "project.Touch();" in text:
        errors.append("Source reconcile revision must remain AuditTrail-owned; standalone project.Touch is redundant")

    resolve_start = text.find("private static List<Target> ResolveTargets")
    resolve_end = text.find("private static Dictionary<string, List<ProjectElement>> BuildSourceOwnerIndex", resolve_start)
    resolve = text[resolve_start:resolve_end] if resolve_start >= 0 and resolve_end > resolve_start else ""
    generated_build = resolve.find("GeneratedHandleOwnershipIndex.Build(project)")
    source_build = resolve.find("BuildSourceOwnerIndex(project)")
    selection_loop = resolve.find("foreach (var snapshot in snapshots)")
    if min(generated_build, source_build, selection_loop) < 0 or not (generated_build < selection_loop and source_build < selection_loop):
        errors.append("Source reconcile ownership indexes must be built once per validation phase before the selected-snapshot loop")
    if "GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle" in resolve:
        errors.append("Source reconcile must not rescan the whole project for generated ownership on every selected handle")
    if ".Where(x => x.SourceHandles.Any" in resolve:
        errors.append("Source reconcile must not rescan all project elements for source ownership on every selected handle")

    closure_start = text.find("private static IReadOnlyList<ProjectElement> ExpandInvalidationTargets")
    closure_end = text.find("private static void EnqueueInvalidationTarget", closure_start)
    closure = text[closure_start:closure_end] if closure_start >= 0 and closure_end > closure_start else ""
    graph_build = closure.find("graph.Rebuild(project.Elements)")
    queue_loop = closure.find("while (queue.Count > 0)")
    if min(graph_build, queue_loop) < 0 or graph_build > queue_loop:
        errors.append("Source reconcile must build the reverse dependency/element index once before queue-based invalidation closure traversal")
    if re.search(r"\b(?:byId|elementsById)\s*=\s*new\s+Dictionary<string,\s*ProjectElement>", closure) or "foreach (var element in project.Elements)" in closure:
        errors.append("Source reconcile closure must reuse DependencyGraph's retained element index instead of rescanning project elements into byId")
    if "while (expanded)" in closure or "candidate.DependsOn.Any(result.ContainsKey)" in closure:
        errors.append("Source reconcile invalidation closure must not repeatedly rescan all project elements")
    if "graph.TryGetElement(dependentId" not in closure or "EnqueueOpeningHost(current, graph" not in closure:
        errors.append("Source reconcile closure must resolve direct dependents and linked opening hosts through the committed DependencyGraph element index")

    stable_start = text.find("private static int RegenerateAffectedToStable")
    stable_end = text.find("private static bool HasSemanticDirty", stable_start)
    stable = text[stable_start:stable_end] if stable_start >= 0 and stable_end > stable_start else ""
    if "project.Elements" in stable:
        errors.append("Source reconcile convergence accounting must scan only the affected closure, not the full project")
    if stable.count("affected.Count(HasSemanticDirty)") < 2 or "affected\n                .Where(HasSemanticDirty)" not in stable:
        errors.append("Source reconcile convergence must count/report unresolved semantic dirtiness directly from the affected closure")

    if "GeneratedHandleOwnershipLookupStatus" in text:
        errors.append("GeneratedHandleOwnershipLookupStatus is not part of the current Core ownership API")

    refresh_start = text.find("private static void RefreshSourceDerivedState")
    refresh_end = text.find("private static void UpdateOptionalCadMetadata", refresh_start)
    refresh_body = text[refresh_start:refresh_end] if refresh_start >= 0 and refresh_end > refresh_start else ""
    clear_cad = refresh_body.find('.Where(x => x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase))')
    set_entity_type = refresh_body.find('element.SetProperty("CAD.EntityType"')
    apply_snapshot_metadata = refresh_body.find("foreach (var pair in snapshot.Metadata)")
    if min(clear_cad, set_entity_type, apply_snapshot_metadata) < 0 or not (clear_cad < set_entity_type < apply_snapshot_metadata):
        errors.append("Source reconcile must clear the replace-on-capture CAD.* namespace before applying current source identity and optional snapshot metadata")

    if "engine.RegenerateDirty(project)" in text:
        errors.append("Source reconcile must regenerate only the affected semantic closure, not unrelated dirty project elements")
    if "new Build3DCommands" in text or "QS3DBUILD3D" in text or "SendStringToExecute" in text:
        errors.append("Source reconcile service must not auto-rebuild native/generated geometry")

if engine.is_file():
    text = engine.read_text(encoding="utf-8")
    subset_start = text.find("public int RegenerateDirtySubset")
    subset_end = text.find("private int RegenerateTransactional", subset_start)
    subset = text[subset_start:subset_end] if subset_start >= 0 and subset_end > subset_start else ""
    if "new Dictionary<string, ProjectElement>" in subset:
        errors.append("Targeted regeneration must not build a full by-id dictionary and then scan the project again")
    if "project.Elements.Where" in subset:
        errors.append("Targeted regeneration must not perform a second full project scan to recover requested targets")
    capture = subset.find("var sourceElements = project.Elements.ToArray();")
    materialize_ids = subset.find("var unresolved = CanonicalTargetIds(elementIds, sourceElements.Length);")
    first_freshness = subset.find("RequireElementStructureFresh(project, sourceElements);", materialize_ids)
    captured_scan = subset.find("foreach (var element in sourceElements)")
    dependency_validation = subset.find("ValidateSubsetDependencyExistence(targets, seenProjectIds);", captured_scan)
    second_freshness = subset.find("RequireElementStructureFresh(project, sourceElements);", dependency_validation)
    if min(capture, materialize_ids, first_freshness, captured_scan, dependency_validation, second_freshness) < 0 or not (
        capture < materialize_ids < first_freshness < captured_scan < dependency_validation < second_freshness
    ):
        errors.append("Targeted regeneration must snapshot project membership before caller target enumeration, resolve in one captured project-order scan, then revalidate structure before commit")
    if subset.count("foreach (var element in sourceElements)") != 1:
        errors.append("Targeted regeneration must resolve requested IDs in exactly one captured project-order scan")
    if "foreach (var element in project.Elements)" in subset:
        errors.append("Targeted regeneration must not switch back to the live public project collection after caller target enumeration")

    regen_start = text.find("private int Regenerate(ProjectState project")
    regen_body = text[regen_start:] if regen_start >= 0 else ""
    if "_graph.Rebuild(project.Elements)" in regen_body:
        errors.append("Regeneration pass loop must not rebuild the reverse dependency index; TopologicalDirtyOrder reads candidate DependsOn directly")

    mark_start = text.find("public void MarkChanged(ProjectState project")
    mark_end = text.find("public int RegenerateDirty(ProjectState project)", mark_start)
    mark = text[mark_start:mark_end] if mark_start >= 0 and mark_end > mark_start else ""
    if "new Dictionary<string, ProjectElement>" in mark or "foreach (var element in project.Elements)" in mark:
        errors.append("MarkChanged must reuse DependencyGraph's retained element index instead of rescanning project elements after Rebuild")

commands = []
source_root = ROOT / "src/QS3D.BricsCAD.V25"
if source_root.is_dir():
    for path in source_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
if sum(1 for name in commands if name.upper() == "QS3DSYNCSOURCE") != 1:
    errors.append("QS3DSYNCSOURCE must be registered exactly once")

print("QS3D authoritative source reconcile preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DSYNCSOURCE validates generated/source ownership read-only before a single canonical bind, revalidates project/target freshness, preserves AuditTrail-owned revision, bounded affected-closure regeneration, snapshot-first targeted regeneration freshness, rollback, and explicit native rebuild boundaries.")
