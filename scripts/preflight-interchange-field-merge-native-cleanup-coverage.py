#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE_POLICY = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "GeneratedHandleOwnershipPolicy.cs"
GUARD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedNativeCleanupCoverageGuard.cs"
SERVICE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeImportService.cs"
INVALIDATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "GeneratedDependentGeometryInvalidator.cs"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def main():
    failures = []
    for path in (CORE_POLICY, GUARD, SERVICE, INVALIDATOR):
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
    invalidation = service.find("GeneratedDependentGeometryInvalidator.Prepare(")
    pre_core_authority = service.find('"Interchange field merge / pre-core apply"')
    core_import = service.find("ProjectInterchangeFieldMergeImporter.Import(")
    metadata = service.find("invalidation.CommitMetadata();")
    rebuild_execute = service.find("InterchangeFieldMergeGeneratedRebuildExecutor.Execute(")
    pre_commit_authority = service.find('"Interchange field merge / pre-CAD commit"')
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
        invalidation,
        pre_core_authority,
        core_import,
        metadata,
        rebuild_execute,
        pre_commit_authority,
        commit,
    ]
    if min(ordered) < 0:
        failures.append(
            "field merge service is missing cleanup coverage, canonical locked rebind, sidecar authority phase checks, rollback snapshot, bounded rebuild prepare/execute, native invalidation, Core apply, metadata sweep, or CAD commit"
        )
    elif ordered != sorted(ordered):
        failures.append(
            "field merge native ordering must be early coverage -> document lock/rebind -> transaction/snapshot -> locked coverage -> pre-native authority -> rebuild preflight -> invalidator -> pre-Core authority -> Core apply -> metadata -> bounded rebuild -> pre-commit authority -> CAD commit"
        )

    if service.count("ProjectContextCoordinator.RequireBackingStoreUnchanged(") != 3:
        failures.append("field merge must recheck sidecar/backing-store authority exactly at pre-native, pre-Core and pre-CAD-commit phases")

    # Verify the locked canonical objects feed destructive work without coupling the gate
    # to indentation. The ordering assertions above prove these calls occur in the guarded
    # mutation path; these argument checks keep the exact locked project/transaction/closure.
    require(service, "GeneratedDependentGeometryInvalidator.Prepare(", "locked invalidator call", failures)
    require(service, "transaction,\n                                lockedProject,\n                                lockedInvalidationTargets)", "locked invalidator inputs", failures)
    require(service, "ProjectInterchangeFieldMergeImporter.Import(", "locked Core mutation call", failures)
    require(service, "lockedProject,\n                                json,", "locked Core mutation target", failures)
    require(service, "if (!cadCommitted && rollback != null)", "conditional semantic rollback", failures)
    require(service, "rollback.Restore(project)", "semantic rollback target", failures)

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
        print("QS3D Interchange FieldMerge native cleanup coverage preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: FieldMerge rejects unsupported generated ownership slots before native mutation.")
    print("PASS: the exact canonical project and affected targets are rebound/re-resolved under the document lock.")
    print("PASS: cleanup coverage is rechecked under the document lock before bounded rebuild preflight and native invalidation.")
    print("PASS: bounded rebuild is prepared before invalidation and executes only after Core apply + old-owner metadata cleanup.")
    print("PASS: sidecar authority is rechecked before native cleanup, before Core apply, and before CAD commit.")
    print("PASS: physical-opening owner aliases must identify the same generated host Solid3d handle.")
    print("PASS: known native cleanup handlers remain present for solid/rebar/curtain/grid ownership slots.")
    print("PASS: generated semantic MText tags have complete-set validation, ownership/type checks, erase coverage and metadata cleanup.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
