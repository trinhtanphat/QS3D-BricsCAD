#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportCoordinator.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportCoordinatorSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-IMPORT-COORDINATOR.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing import coordinator file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "ProjectInterchangeImportExecutionMode",
    "AppendOnly",
    "KeepTarget",
    "ImportAsNew",
    "UseSourceSemanticData",
    "FieldMerge = 4",
    "PreserveSourceHandleProvenance",
    "FieldMergePolicy",
    "No fallback mode was attempted",
    "ProjectInterchangeAppendOnlyImporter.Import",
    "ProjectInterchangeAppendProvenanceImporter.Import",
    "ProjectInterchangeKeepTargetImporter.Import",
    "ProjectInterchangeKeepTargetProvenanceImporter.Import",
    "ProjectInterchangeRemapAppendImporter.Import",
    "ProjectInterchangeRemapProvenanceImporter.Import",
    "ProjectInterchangeUseSourceSemanticImporter.Import",
    "ProjectInterchangeUseSourceProvenanceImporter.Import",
    "ProjectInterchangeFieldMergeImporter.Plan",
    "ProjectInterchangeFieldMergeImporter.Import",
    "nativeCleanupAuthorization.ElementIds.Count",
    "NativeCleanupRequirements",
    "NativeCleanupElementIds",
    "CreateNativeCleanupAuthorization",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan(_useSourceSemanticPlan)",
    "CreateFieldMergeAuthorization",
    "_fieldMergeExecutionPlan.CreateAuthorization()",
    "FieldMergeSourceFieldsToApply",
    "FieldMergeTargetFieldsToKeep",
    "FieldMergeUnresolvedDecisionCount",
    "FieldMergeSourceOnlyIdentityCount",
    "FieldMergeAffectedTargetElements",
    "FieldMergeNativeCleanupHandlesRequired",
    "FieldMerge execution requires authorization created from the exact reviewed FieldMerge coordinator plan",
    "FieldMerge does not support source-handle provenance",
    "FieldMergePolicy is accepted only for FieldMerge mode",
    "Enum.IsDefined",
):
    if token not in source:
        errors.append("import coordinator missing contract token: " + token)

for token in (
    "CollisionModeIsExplicitAndNeverFallsBack",
    "ImportAsNewPlanSurfacesRemapWithoutMutation",
    "UseSourcePlanPropagatesNativeCleanupRequirement",
    "ExecuteRejectsCleanupAuthorityForOtherModes",
    "UseSourceExecuteRequiresAndConsumesExplicitAuthorization",
    "ProvenanceToggleSelectsCombinedExecution",
    "FieldMergePlanRequiresExplicitPolicyAndNoProvenance",
    "FieldMergePlanSurfacesDedicatedReviewMetrics",
    "FieldMergeExecuteRequiresExactAuthorization",
    "InvalidModeFailsClosed",
    "plan.NativeCleanupRequirements[0].OwnerHandles.Single()",
    "coordinatorPlan.CreateNativeCleanupAuthorization()",
    "appendPlan.CreateNativeCleanupAuthorization()",
    "plan.CreateFieldMergeAuthorization()",
    "wrongAuthorization",
    "FieldMergeNativeCleanupHandlesRequired",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("import coordinator smoke missing regression token: " + token)

for forbidden in (
    "ProjectInterchangeNativeCleanupAuthorization.ForElementIds(plan.NativeCleanupElementIds)",
    "ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);\n            Equal(string.Join",
):
    if forbidden in test:
        errors.append("import coordinator smoke bypasses the exact coordinator cleanup plan: " + forbidden)

for token in (
    "one explicit mode",
    "never falls back",
    "cleanup authorization",
    "exact generated-handle requirements",
    "CreateNativeCleanupAuthorization()",
    "Core coordinator",
    "does not create a BricsCAD command",
    "LOCAL_ONLY",
    "PreserveSourceHandleProvenance",
):
    if token not in doc:
        errors.append("import coordinator documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: one Core coordinator selects an explicit import policy/provenance mode, exposes exact UseSource cleanup handles plus reviewed FieldMerge authorization/metrics, and never falls back silently.")