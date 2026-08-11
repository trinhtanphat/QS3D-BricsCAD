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

    # Core ownership is intentionally extensible. Native cleanup must therefore prove
    # coverage instead of assuming every future Generated*Handle(s) slot has an eraser.
    require(policy, 'StartsWith("Generated", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)
    require(policy, 'EndsWith("Handle", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)
    require(policy, 'EndsWith("Handles", StringComparison.OrdinalIgnoreCase)', "core ownership policy", failures)

    require(guard, "CoreOwnershipPolicy.IsOwnerSlot(property.Key)", "native cleanup coverage guard", failures)
    require(guard, "CoreOwnershipPolicy.IsRebarOwnerSlot(ownerSlot)", "native cleanup coverage guard", failures)
    require(guard, '"GeneratedSolidHandle"', "native cleanup coverage guard", failures)
    require(guard, '"PhysicalOpeningCutSolidHandle"', "native cleanup coverage guard", failures)
    require(guard, '"GeneratedCurtainFrameHandles"', "native cleanup coverage guard", failures)
    require(guard, '"GeneratedCurtainPanelHandles"', "native cleanup coverage guard", failures)
    require(guard, "GridAnnotationBuilder.HandlesKey", "native cleanup coverage guard", failures)
    require(guard, "has no BricsCAD native cleanup handler", "native cleanup coverage guard", failures)
    require(guard, "EnsurePhysicalOpeningAliasMatchesHostSolid", "native cleanup coverage guard", failures)
    require(guard, "does not match", "physical-opening alias guard", failures)

    guard_token = "GeneratedNativeCleanupCoverageGuard.EnsureSupported(invalidationTargets);"
    first_guard = service.find(guard_token)
    second_guard = service.find(guard_token, first_guard + len(guard_token)) if first_guard >= 0 else -1
    snapshot = service.find("ProjectStateSnapshot.Capture(project)")
    transaction = service.find("StartTransaction()")
    invalidation = service.find("GeneratedDependentGeometryInvalidator.Prepare(")
    if min(first_guard, second_guard, snapshot, transaction, invalidation) < 0:
        failures.append(
            "field merge service must contain both cleanup coverage checks, rollback snapshot, transaction, and invalidator preparation"
        )
    else:
        if not (first_guard < snapshot < transaction):
            failures.append("field merge must perform an early cleanup-coverage precheck before rollback capture/native transaction")
        if not (transaction < second_guard < invalidation):
            failures.append("field merge must recheck cleanup coverage under the document lock immediately before destructive invalidator preparation")
        if service.count(guard_token) != 2:
            failures.append("field merge cleanup coverage must have exactly the early precheck and locked pre-invalidation recheck")

    # Keep the explicit native handlers visible in the invalidator. If a handler is removed,
    # the coverage whitelist must not continue advertising that slot as safely erasable.
    require(invalidator, "CoreOwnershipPolicy.RebarHandleKeys", "native invalidator", failures)
    require(invalidator, "EraseCurtainFrames", "native invalidator", failures)
    require(invalidator, "EraseCurtainPanels", "native invalidator", failures)
    require(invalidator, "EraseGridAnnotations", "native invalidator", failures)
    require(invalidator, "GeneratedGeometryService.PrepareReplacement", "native invalidator", failures)

    if failures:
        print("QS3D Interchange FieldMerge native cleanup coverage preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: FieldMerge rejects unsupported generated ownership slots before native mutation.")
    print("PASS: cleanup coverage is rechecked under the document lock immediately before native invalidation.")
    print("PASS: physical-opening owner aliases must identify the same generated host Solid3d handle.")
    print("PASS: known native cleanup handlers remain present for solid/rebar/curtain/grid ownership slots.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
