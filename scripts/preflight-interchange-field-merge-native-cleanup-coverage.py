#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE_POLICY = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "GeneratedHandleOwnershipPolicy.cs"
GUARD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedNativeCleanupCoverageGuard.cs"
SERVICE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeImportService.cs"
INVALIDATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedDependentGeometryInvalidator.cs"
REBUILD_PLAN = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeGeneratedRebuildPlan.cs"
REBUILD_EXECUTOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeGeneratedRebuildExecutor.cs"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def main():
    failures = []
    for path in (CORE_POLICY, GUARD, SERVICE, INVALIDATOR, REBUILD_PLAN, REBUILD_EXECUTOR):
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    policy = CORE_POLICY.read_text(encoding="utf-8")
    guard = GUARD.read_text(encoding="utf-8")
    service = SERVICE.read_text(encoding="utf-8")
    invalidator = INVALIDATOR.read_text(encoding="utf-8")
    rebuild_plan_source = REBUILD_PLAN.read_text(encoding="utf-8")
    rebuild_executor = REBUILD_EXECUTOR.read_text(encoding="utf-8")

    require(policy, 'StartsWith("Generated", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)
    require(policy, 'EndsWith("Handle", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)
    require(policy, 'EndsWith("Handles", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)

    require(guard, "var ownerSlot = (property.Key ?? string.Empty).Trim();", "native cleanup coverage guard", failures)
    require(guard, "CoreOwnershipPolicy.IsOwnerSlot(ownerSlot)", "native cleanup coverage guard", failures)
    require(guard, "CoreOwnershipPolicy.IsRebarOwnerSlot(ownerSlot)", "native cleanup coverage guard", failures)
    require(guard, '"GeneratedSolidHandle"', "native cleanup coverage guard", failures)
    require(guard, '"PhysicalOpeningCutSolidHandle"', "native cleanup coverage guard", failures)
    require(guard, '"GeneratedCurtainFrameHandles"', "native cleanup coverage guard", failures)
    require(guard, '"GeneratedCurtainPanelHandles"', "native cleanup coverage guard", failures)
    require(guard, "GridAnnotationBuilder.HandlesKey", "native cleanup coverage guard", failures)
    require(guard, "GeneratedSemanticTagHealthService.HandlesKey", "semantic tag cleanup coverage guard", failures)
    require(guard, "has no BricsCAD native cleanup handler", "native cleanup coverage guard", failures)
    require(guard, "EnsurePhysicalOpeningAliasMatchesHostSolid", "native cleanup coverage guard", failures)
    require(guard, "does not match", "physical-opening alias guard", failures)

    # Pin the public source contract of the bounded rebuild plan. NativeGeometry + Quantity
    # are the only automatic kinds. Workbook/Trace are deliberately reserved and therefore
    # must flow through the unsupported-mask failure rather than silently broadening scope.
    require(rebuild_plan_source, "InterchangeGeneratedOutputKind.NativeGeometry |", "FieldMerge rebuild supported kinds", failures)
    require(rebuild_plan_source, "InterchangeGeneratedOutputKind.Quantity;", "FieldMerge rebuild supported kinds", failures)
    require(rebuild_plan_source, "Workbook = 1 << 2", "FieldMerge reserved Workbook kind", failures)
    require(rebuild_plan_source, "Trace = 1 << 3", "FieldMerge reserved Trace kind", failures)
    require(rebuild_plan_source, "(requestedKinds & ~SupportedKinds) != 0", "FieldMerge unsupported-output fail-closed mask", failures)
    require(rebuild_plan_source, "Only atomic NativeGeometry and Quantity rebuilds are supported", "FieldMerge unsupported-output diagnostic", failures)
    require(rebuild_plan_source, "ElementIds.Count == 0 || OutputKinds == InterchangeGeneratedOutputKind.None", "FieldMerge rebuild no-op contract", failures)
    require(rebuild_plan_source, ".Distinct(StringComparer.OrdinalIgnoreCase)", "FieldMerge rebuild deterministic id dedupe", failures)
    require(rebuild_plan_source, ".OrderBy(id => id, StringComparer.OrdinalIgnoreCase)", "FieldMerge rebuild deterministic id order", failures)

    # Prepare must stay observational/fail-closed and prove the exact reviewed closure can be
    # rebuilt before invalidation. Specialized owner slots/categories and ambiguous CAD source
    # ownership are not allowed to fall through to a partial generic rebuild.
    require(rebuild_executor, "FieldMerge rebuild plan escaped the reviewed affected closure", "bounded rebuild closure guard", failures)
    require(rebuild_executor, "NativeGeometry rebuild was not explicitly requested", "explicit native-rebuild intent guard", failures)
    require(rebuild_executor, "does not support generated owner slot", "specialized owner-slot fail-closed guard", failures)
    require(rebuild_executor, "StructuralSolidBuilder.Supports(element.Category)", "native rebuild category guard", failures)
    require(rebuild_executor, "applied slabOpen peers require the retiring solid handle", "slab-opening specialized replay guard", failures)
    require(rebuild_executor, "requires exactly one live CAD source", "native rebuild source cardinality guard", failures)
    require(rebuild_executor, "one CAD source claimed by multiple affected elements", "native rebuild duplicate source ownership guard", failures)
    require(rebuild_executor, "StructuralSolidBuilder.BuildSelected(document, project, categoryGroup.Key)", "production structural rebuild path", failures)
    require(rebuild_executor, "RegenerateDirtySubset(project, manifest.Plan.ElementIds)", "bounded quantity regeneration path", failures)
    require(rebuild_executor, "RestoreImpliedSelectionBestEffort", "selection restoration boundary", failures)

    early_guard = service.find("GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);")
    document_lock = service.find("using (document.LockDocument())")
    locked_require = service.find("var lockedProject = ExistingProjectMutationContext.Require(")
    same_project = service.find("ReferenceEquals(lockedProject, project)")
    locked_targets = service.find("var lockedInvalidationTargets = ResolveAffectedTargets(")
    transaction = service.find("StartTransaction()")
    snapshot = service.find("ProjectStateSnapshot.Capture(lockedProject)")
    locked_guard = service.find("GeneratedNativeCleanupCoverageGuard.EnsureSupported(lockedInvalidationTargets);")
    pre_native_authority = service.find('"Interchange field merge / pre-native cleanup"')
    rebuild_prepare = service.find("InterchangeFieldMergeGeneratedRebuildExecutor.Prepare(")
    external_transition = service.find("SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document)")
    invalidation = service.find("GeneratedDependentGeometryInvalidator.Prepare(")
    pre_core_authority = service.find('"Interchange field merge / pre-core apply"')
    core_import = service.find("ProjectInterchangeFieldMergeImporter.Import(")
    metadata = service.find("invalidation.CommitMetadata();")
    rebuild_execute = service.find("InterchangeFieldMergeGeneratedRebuildExecutor.Execute(")
    pre_commit_authority = service.find('"Interchange field merge / pre-CAD commit"')
    stage_after = service.find("undoTransition.StageAfter(")
    commit = service.find("transaction.Commit();")

    ordered = [
        early_guard,
        document_lock,
        locked_require,
        same_project,
        locked_targets,
        transaction,
        snapshot,
        locked_guard,
        pre_native_authority,
        rebuild_prepare,
        external_transition,
        invalidation,
        pre_core_authority,
        core_import,
        metadata,
        rebuild_execute,
        pre_commit_authority,
        stage_after,
        commit,
    ]
    if min(ordered) < 0:
        failures.append(
            "field merge service is missing cleanup coverage, canonical locked rebind, sidecar authority checks, rollback snapshot, bounded rebuild prepare/execute, external Undo scope, native invalidation, exact Core apply, metadata sweep, after-state staging, or CAD commit"
        )
    elif ordered != sorted(ordered):
        failures.append(
            "field merge native ordering must be early coverage -> document lock/rebind -> transaction/snapshot -> locked coverage -> pre-native authority -> rebuild preflight -> external Undo scope -> invalidator -> pre-Core authority -> exact Core apply -> retiring metadata cleanup -> bounded rebuild -> pre-commit authority -> after-state -> CAD commit"
        )

    if service.count("ProjectContextCoordinator.RequireBackingStoreUnchanged(") != 3:
        failures.append("field merge must recheck sidecar/backing-store authority exactly at pre-native, pre-Core and pre-CAD-commit phases")

    require(service, "AutomaticRebuildKinds =", "automatic rebuild declaration", failures)
    require(service, "InterchangeGeneratedOutputKind.NativeGeometry |", "automatic native rebuild intent", failures)
    require(service, "InterchangeGeneratedOutputKind.Quantity;", "automatic quantity rebuild intent", failures)
    require(service, "reviewedPlan.Authorization", "exact reviewed authorization handoff", failures)
    require(service, "SourceReconcileUndoCoordinator.BeginTransition(", "outer FieldMerge Undo transition", failures)
    require(service, "undoTransition.StageNativeMarker();", "outer native Undo marker", failures)
    require(service, "GeneratedDependentGeometryInvalidator.Prepare(", "locked invalidator call", failures)
    require(service, "transaction,\n                                lockedProject,\n                                lockedInvalidationTargets)", "locked invalidator inputs", failures)
    require(service, "ProjectInterchangeFieldMergeImporter.Import(", "locked Core mutation call", failures)
    require(service, "lockedProject,\n                                json,", "locked Core mutation target", failures)
    require(service, "if (!cadCommitted && rollback != null)", "conditional semantic rollback", failures)
    require(service, "rollback.Restore(project)", "semantic rollback target", failures)
    require(service, "undoTransition.ConfirmCommitted();", "outer Undo transition commit confirmation", failures)

    require(invalidator, "CoreOwnershipPolicy.RebarHandleKeys", "native invalidator", failures)
    require(invalidator, "EraseCurtainFrames", "native invalidator", failures)
    require(invalidator, "EraseCurtainPanels", "native invalidator", failures)
    require(invalidator, "EraseGridAnnotations", "native invalidator", failures)
    require(invalidator, "GeneratedGeometryService.PrepareReplacement", "native invalidator", failures)

    require(invalidator, "EnsureSemanticTagsLive", "semantic tag native invalidator", failures)
    require(invalidator, "EraseSemanticTags", "semantic tag native invalidator", failures)
    require(invalidator, "EnsureSemanticTagOwned", "semantic tag ownership guard", failures)
    require(invalidator, "GeneratedSemanticTagHealthService.HandlesKey", "semantic tag owner slot", failures)
    require(invalidator, "if (!(entity is MText))", "semantic tag MText type guard", failures)
    require(invalidator, 'RemoveByPrefix(element, "GeneratedSemanticTag")', "semantic tag metadata cleanup", failures)
    require(invalidator, "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element", "semantic tag native ownership check", failures)

    if failures:
        print("QS3D Interchange FieldMerge native cleanup/rebuild coverage preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: FieldMerge rejects unsupported generated ownership slots before native mutation.")
    print("PASS: the exact canonical project and affected targets are rebound/re-resolved under the document lock.")
    print("PASS: NativeGeometry + Quantity are the only automatic rebuild classes; Workbook/Trace/unknown kinds fail closed.")
    print("PASS: rebuild no-op/id normalization and deterministic ordering contracts are source-pinned.")
    print("PASS: bounded rebuild preflight refuses escaped closure, specialized owners/categories and ambiguous/duplicate CAD sources before invalidation.")
    print("PASS: bounded native rebuild uses the production structural builder and quantity regeneration stays on the reviewed affected subset.")
    print("PASS: cleanup coverage is rechecked under the document lock before bounded rebuild preflight and native invalidation.")
    print("PASS: bounded rebuild is prepared before invalidation and executes only after exact Core apply + old-owner metadata cleanup.")
    print("PASS: one external semantic/native Undo scope covers invalidate + apply + rebuild before the outer CAD commit.")
    print("PASS: sidecar authority is rechecked before native cleanup, before Core apply, and before CAD commit.")
    print("PASS: pre-commit failure remains guarded by ProjectState snapshot rollback.")
    print("PASS: physical-opening owner aliases must identify the same generated host Solid3d handle.")
    print("PASS: known native cleanup handlers remain present for solid/rebar/curtain/grid ownership slots.")
    print("PASS: generated semantic MText tags have complete-set validation, ownership/type checks, erase coverage and metadata cleanup.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
