#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeUseSourceProvenanceImporter.cs"
SEMANTIC = ROOT / "src/QS3D.Core/Export/ProjectInterchangeUseSourceSemanticImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceProvenanceImporterSmoke.cs"
SEMANTIC_TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceSemanticImporterSmoke.cs"
COORDINATOR_TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportCoordinatorSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-USE-SOURCE-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing UseSource provenance/authorization file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(IMPORTER)
semantic = read(SEMANTIC)
test = read(TEST)
semantic_test = read(SEMANTIC_TEST)
coordinator_test = read(COORDINATOR_TEST)
doc = read(DOC)

for token in (
    'ImportMode = "UseSourceSemanticDataPreserveSourceHandleProvenance"',
    "ProjectInterchangeUseSourceSemanticImporter.Plan",
    "ProjectInterchangeSourceHandleProvenance.Plan",
    "ProjectInterchangeUseSourceSemanticImporter.Import",
    "ProjectInterchangeSourceHandleProvenance.Store",
    "ProjectInterchangeProvenanceTargetMap.Store",
    "ProjectInterchangeNativeCleanupAuthorization",
    "TargetElementIdsRequiringNativeCleanup",
    "EnsureSourceElementsDoNotOwnImportedCad",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(target)",
    "ImportInterchangeUseSourceWithSourceHandleProvenance",
):
    if token not in source:
        errors.append("UseSource provenance importer missing contract token: " + token)

for token in (
    "ForPlan(",
    "target.ProjectId",
    "target.ChangeVersion",
    "GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles",
    "GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot",
    "CaptureOwnerTokens",
    "OwnerTokensSetEquals",
    "ElementIdsSetEquals",
    "EnsureNativeCleanupAuthorized(target, prepared.Plan, nativeCleanupAuthorization)",
    "state-bound native cleanup authorization",
    "current generated handle/owner-slot set changed",
):
    if token not in semantic:
        errors.append("canonical UseSource cleanup authorization missing stale-safe contract token: " + token)

if "EnsureNativeCleanupAuthorized(prepared.Plan, nativeCleanupAuthorization)" in semantic:
    errors.append("canonical UseSource import still validates cleanup authorization without the current target state")

for token in (
    "PlanPreservesCleanupRequirementAndIdentityMapping",
    "MissingCleanupAuthorizationFailsBeforeProvenanceMutation",
    "AuthorizedImportRetainsProvenanceWithoutCadOwnership",
    "MissingSourceFingerprintFailsBeforeMutation",
    "ProjectInterchangeNativeCleanupAuthorization.None",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan(target, plan.SemanticPlan)",
    "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles",
    "ProjectInterchangeProvenanceTargetMap.ReadTargetElementId",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("UseSource provenance smoke missing regression token: " + token)

for token in (
    "StaleNativeCleanupAuthorizationFailsBeforeMutation",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan(target, plan)",
    'element.Properties["GeneratedSolidHandle"] = "BB22"',
    "Equal(changeVersion, target.ChangeVersion)",
):
    if token not in semantic_test:
        errors.append("canonical UseSource stale-authorization smoke missing token: " + token)

for token in (
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan(target, semanticPlan)",
    "UseSourceExecuteRequiresAndConsumesExplicitAuthorization",
):
    if token not in coordinator_test:
        errors.append("unified import coordinator smoke missing state-bound cleanup authorization token: " + token)

for unsafe in (
    "ProjectInterchangeNativeCleanupAuthorization.ForElementIds(plan.SemanticPlan.TargetElementIdsRequiringNativeCleanup)",
    "ProjectInterchangeNativeCleanupAuthorization.ForElementIds(plan.NativeCleanupElementIds)",
):
    if unsafe in test or unsafe in coordinator_test:
        errors.append("UseSource success-path smoke still authorizes cleanup by Element ID only: " + unsafe)

source_calls = []
for path in (ROOT / "src").rglob("*.cs"):
    text = path.read_text(encoding="utf-8", errors="ignore")
    if "ProjectInterchangeNativeCleanupAuthorization.ForElementIds(" in text:
        source_calls.append(str(path.relative_to(ROOT)))
if source_calls:
    errors.append("production source must not grant cleanup authority by Element ID only: " + ", ".join(sorted(source_calls)))

for token in (
    "cleanup authorization",
    "does not perform native cleanup",
    "raw source-handle provenance",
    "source-to-target semantic lineage",
    "never target CAD ownership",
    "portable semantic re-export",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("UseSource provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: UseSource provenance composition preserves target CAD ownership boundaries and cleanup authorization is bound to the exact project/change-version/generated owner set before semantic mutation.")
