#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeFieldMergePlanner.cs"
COORDINATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeImportCoordinator.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeFieldMergePlannerSmoke.cs"
BATCH_NAME_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectInterchangeFieldMergeBatchNameCollisionSmoke.cs"

errors = []
for path in (PLANNER, COORDINATOR, SMOKE, BATCH_NAME_SMOKE):
    if not path.is_file():
        errors.append("missing field-merge contract file: " + str(path.relative_to(ROOT)))

if not errors:
    planner = PLANNER.read_text(encoding="utf-8")
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    batch_name_smoke = BATCH_NAME_SMOKE.read_text(encoding="utf-8")

    required = (
        "public bool IsPreviewOnly => true;",
        "public bool CanProceedToMutationDesign => !HasBlocks && !HasUnresolvedDecisions;",
        "InterchangeFieldPrecedenceChoice.Unspecified",
        "InterchangeFieldPrecedenceChoice.KeepTarget",
        "InterchangeFieldPrecedenceChoice.UseSource",
        '"familyId"',
        '"floorId"',
        '"zoneId"',
        '"dependencies"',
        '"properties"',
        '"quantities"',
        "requiresGeneratedOutputReset",
        "AddSelectedSourceNameBatchCollisions",
    )
    for token in required:
        if token not in planner:
            errors.append("field-merge planner missing preview/precedence token: " + token)

    forbidden = (
        "ProjectStateSnapshot.Capture",
        "ProjectFamilyService.SetProperty",
        "ProjectFamilyService.RemoveProperty",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles",
        "ProjectInterchangeNativeCleanupAuthorization",
    )
    for token in forbidden:
        if token in planner:
            errors.append("preview-only field-merge planner crossed mutation/native boundary: " + token)

    if "FieldMerge" in coordinator or "FieldPrecedence" in coordinator:
        errors.append("preview-only field merge must not be exposed as an executable coordinator mode before a reviewed executor/native-cleanup contract exists")

    smoke_required = (
        "MixedPrecedenceIsDeterministicAndPreviewOnly",
        "UnspecifiedPrecedenceFailsClosed",
        "CategoryMismatchBlocksFieldMerge",
        "GeneratedSolidHandle",
        "sourceHandles",
        "drawingFingerprint",
    )
    for token in smoke_required:
        if token not in smoke:
            errors.append("field-merge smoke missing boundary regression: " + token)

    batch_required = (
        "SelectedDuplicateSourceNamesBlockSameScope",
        "FamilyDuplicateSourceNamesRemainCategoryScoped",
        "Shared Zone",
        "Shared Floor",
        "Shared Beam",
    )
    for token in batch_required:
        if token not in batch_name_smoke:
            errors.append("field-merge batch-name smoke missing ownership regression: " + token)

if errors:
    print("QS3D interchange field-merge preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: field-level precedence remains deterministic, explicit, fail-closed and preview-only; batch display-name ownership is guarded and native/provenance authority is not merged or exposed as an executable coordinator mode.")
