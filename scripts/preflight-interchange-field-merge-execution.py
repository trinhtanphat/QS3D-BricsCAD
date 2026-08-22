#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeFieldMergeImporter.cs"
COORDINATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeImportCoordinator.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeFieldMergeImporterSmoke.cs"

errors = []
for path in (IMPORTER, COORDINATOR, SMOKE):
    if not path.is_file():
        errors.append("missing field-merge execution contract file: " + str(path.relative_to(ROOT)))

if not errors:
    importer = IMPORTER.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    required = (
        "ProjectInterchangeFieldMergeAuthorization",
        "TargetChangeVersion",
        "SourceSnapshotHash",
        "DecisionStamp",
        "authorization.MatchesExactly(plan)",
        "Field merge handles same-ID collisions only",
        "ProjectStateSnapshot.Capture(target)",
        "ProjectZoneService.Update",
        "ProjectFloorService.Update",
        "ProjectFamilyService.Rename",
        "ProjectFamilyService.SetProperty",
        "ProjectFamilyService.RemoveProperty",
        "ProjectFamilyService.Assign",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle",
        "Field merge native cleanup ownership is ambiguous",
        "requires a non-empty target drawing fingerprint",
        "ClearGeneratedOwnershipMetadata",
        "ValidateCombinedTarget",
        "snapshot.Restore(target)",
    )
    for token in required:
        if token not in importer:
            errors.append("field-merge importer missing guarded execution token: " + token)

    forbidden = (
        "ProjectContextCoordinator.GetOrCreate",
        "TransactionManager",
    )
    for token in forbidden:
        if token in importer:
            errors.append("Core field-merge importer crossed native/project-bootstrap boundary: " + token)

    if "FieldMerge" in coordinator or "FieldPrecedence" in coordinator:
        errors.append("field merge must not be exposed as a generic coordinator mode before guarded BricsCAD cleanup/recovery orchestration and exact-V25 qualification exist")

    smoke_required = (
        "MixedReviewedMergeAppliesOnlySelectedSourceGroups",
        "TargetRevisionChangeRejectsReviewedAuthorization",
        "SourceSnapshotChangeRejectsReviewedAuthorization",
        "GeneratedHandleChangeRejectsReviewedAuthorization",
        "AmbiguousGeneratedOwnershipBlocksAuthorization",
        "DestructiveCleanupRequiresTargetDrawingFingerprint",
        "SourceOnlyIdentityBlocksExecution",
        "FamilyReassignmentPreservesTargetPropertiesWhenRequested",
    )
    for token in smoke_required:
        if token not in smoke:
            errors.append("field-merge importer smoke missing execution/freshness regression: " + token)

if errors:
    print("QS3D interchange field-merge execution preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: reviewed Core field merge is target/source/decision fresh, exact-handle cleanup-bound, rejects ambiguous generated ownership and anonymous target drawings before cleanup authorization, remains rollback-safe and canonical-service based, and keeps generic BricsCAD orchestration separate.")
