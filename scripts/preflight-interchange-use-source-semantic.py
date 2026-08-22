#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeUseSourceSemanticImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceSemanticImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-USE-SOURCE-SEMANTIC.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing UseSource semantic import file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    "ProjectInterchangeNativeCleanupRequirement",
    "ProjectInterchangeUseSourceSemanticPlan",
    "NativeCleanupRequirements",
    "ProjectInterchangeNativeCleanupAuthorization",
    "ProjectInterchangeNativeCleanupAuthorization ForPlan",
    "MatchesExactly",
    "ProjectInterchangeUseSourceSemanticResult",
    "InterchangeExistingIdentityAction.UseSourceSemanticData",
    "InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild",
    "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles",
    "BuildAffectedTargetElementIds",
    "ReferencesAffectedHost",
    "EnsureNativeCleanupAuthorized",
    "ProjectStateSnapshot.Capture",
    "snapshot.Restore(target)",
    "ApplySourceElementSemanticData",
    "ClearGeneratedOwnershipMetadata",
    "element.SourceHandles.Clear()",
    "element.DrawingFingerprint = string.Empty",
    "element.MarkDirty(ElementDirtyFlags.All)",
    "ImportInterchangeUseSourceSemantic",
):
    if token not in source:
        errors.append("UseSource semantic importer missing contract token: " + token)

for token in (
    "PlanClassifiesReplacementAndNativeCleanup",
    "ImportRejectsMissingNativeCleanupWithoutMutation",
    "ImportRejectsStaleNativeCleanupHandleAuthorizationWithoutMutation",
    "ImportReplacesInPlaceAndInvalidatesAffectedTargetElements",
    "SemanticOnlyReplacementNeedsNoNativeAuthorization",
    "ConflictsFailBeforeMutation",
    "ReferenceEquals",
    "GeneratedSolidHandle",
    "GeneratedRebarHandles",
    "ProjectInterchangeNativeCleanupAuthorization.None",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan",
    'element.Properties["GeneratedSolidHandle"] = "BB22"',
):
    if token not in test:
        errors.append("UseSource semantic smoke missing regression token: " + token)

for token in (
    "semantic replacement",
    "native cleanup authorization",
    "exact generated-handle set",
    "Core importer does not",
    "source handles",
    "generated ownership",
    "LOCAL_ONLY",
    "generic `QS3DINTERCHANGEIMPORT`",
):
    if token not in doc:
        errors.append("UseSource semantic documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: UseSourceSemanticData is bounded to exact handle-bound native cleanup authorization, semantic replacement, dirty rebuild, and rollback-safe Core mutation.")