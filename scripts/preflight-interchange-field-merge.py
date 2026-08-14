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
        '"familyId"', '"floorId"', '"zoneId"', '"dependencies"', '"properties"', '"quantities"',
        "requiresGeneratedOutputReset",
        "AddSelectedSourceNameBatchCollisions",
    )
    for token in required:
        if token not in planner:
            errors.append("field-merge planner missing preview/precedence token: " + token)

    for token in ("ProjectStateSnapshot.Capture", "ProjectFamilyService.SetProperty", "ProjectFamilyService.RemoveProperty", "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles", "ProjectInterchangeNativeCleanupAuthorization"):
        if token in planner:
            errors.append("preview-only field-merge planner crossed mutation/native boundary: " + token)

    coordinator_required = (
        "FieldMerge = 4",
        "public ProjectInterchangeFieldMergePolicy? FieldMergePolicy { get; set; }",
        "ProjectInterchangeFieldMergeExecutionPlan? _fieldMergeExecutionPlan",
        "public ProjectInterchangeFieldMergeAuthorization CreateFieldMergeAuthorization()",
        "return _fieldMergeExecutionPlan.CreateAuthorization();",
        "case ProjectInterchangeImportExecutionMode.FieldMerge:",
        "return PlanFieldMerge(target, json, request.FieldMergePolicy);",
        "FieldMerge execution requires authorization created from the exact reviewed FieldMerge coordinator plan.",
    )
    for token in coordinator_required:
        if token not in coordinator:
            errors.append("reviewed FieldMerge coordinator exposure missing token: " + token)
    if "ProjectInterchangeImportExecutionMode.FieldMerge" in coordinator and "request.PreserveSourceHandleProvenance" in coordinator:
        field_case = coordinator.find("if (request.Mode == ProjectInterchangeImportExecutionMode.FieldMerge)")
        if field_case >= 0:
            field_block = coordinator[field_case:coordinator.find("var plan = Plan", field_case)]
            if "nativeCleanupAuthorization.ElementIds.Count != 0" not in field_block:
                errors.append("FieldMerge coordinator must reject unrelated UseSource native cleanup authority")

    for token in ("MixedPrecedenceIsDeterministicAndPreviewOnly", "UnspecifiedPrecedenceFailsClosed", "CategoryMismatchBlocksFieldMerge", "GeneratedSolidHandle", "sourceHandles", "drawingFingerprint"):
        if token not in smoke:
            errors.append("field-merge smoke missing boundary regression: " + token)
    for token in ("SelectedDuplicateSourceNamesBlockSameScope", "FamilyDuplicateSourceNamesRemainCategoryScoped", "Shared Zone", "Shared Floor", "Shared Beam"):
        if token not in batch_name_smoke:
            errors.append("field-merge batch-name smoke missing ownership regression: " + token)

if errors:
    print("QS3D interchange field-merge preflight")
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: field-level precedence remains deterministic, explicit and fail-closed; planning stays preview-only, while reviewed coordinator exposure requires the exact FieldMerge execution plan/authorization and rejects unrelated native cleanup authority.")
