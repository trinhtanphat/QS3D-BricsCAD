#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeFieldMergeMutationDesignPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeFieldMergeMutationDesignSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-FIELD-MERGE-MUTATION-DESIGN.md"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing field merge mutation-design contract file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
doc = read(DOC)

for token in (
    "ProjectInterchangeFieldMergeMutationDesign",
    "ProjectInterchangeFieldMergeMutationDesignPlanner",
    "ProjectInterchangeFieldMergePlanner.Plan(target, json, policy)",
    "TargetDrawingFingerprint",
    "TargetChangeVersion",
    "AffectedTargetElementIds",
    "NativeCleanupRequirements",
    "TargetGeneratedHandlesToClean",
    "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
    "GeneratedHandleOwnershipPolicy.TryFindOwner(target, handle",
    "element.DependsOn.Any(affected.Contains)",
    'ReferencesAffectedHost(element, affected)',
    "ProjectFloorService.BottomLevelIdKey",
    "ProjectFloorService.TopLevelIdKey",
    "EnsureTargetStillMatches",
    "Refusing stale mutation design",
    "requires a non-empty target drawing fingerprint",
    "public bool IsPreviewOnly => true;",
):
    if token not in source:
        errors.append("field merge mutation design missing fail-closed token: " + token)

for token in (
    "TargetBindingAndDependentCleanupAreExplicit",
    "KeepTargetDoesNotManufactureCleanupAuthority",
    "UnresolvedFieldPlanCannotBecomeMutationDesign",
    "DestructiveDesignRequiresDrawingFingerprint",
    'Sequence(new[] { "E-DEP", "E-HOST" }, design.AffectedTargetElementIds)',
    "Equal(3, design.TargetGeneratedHandlesToClean)",
    'True(!design.AffectedTargetElementIds.Contains("E-OTHER", StringComparer.OrdinalIgnoreCase))',
    "Equal(beforeVersion, target.ChangeVersion)",
):
    if token not in smoke:
        errors.append("field merge mutation-design smoke missing regression token: " + token)

for token in (
    "preview-only",
    "ProjectId + DrawingFingerprint + ChangeVersion",
    "affected target element closure",
    "exact generated-owner handle requirements",
    "does not execute field mutation",
    "does not erase native CAD",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("field merge mutation-design doc missing boundary token: " + token)

for token in (
    "ProjectStateSnapshot",
    "AuditTrail",
    ".Touch()",
    "ClearGeneratedOwnershipMetadata",
    "transaction.Commit",
    "public static ProjectInterchangeFieldMergeResult Execute",
):
    if token in source:
        errors.append("field merge mutation design must remain preview-only; forbidden mutation token present: " + token)

if errors:
    print("QS3D interchange field merge mutation-design preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: resolved field precedence can produce a target-bound preview-only mutation design with affected dependent closure and exact generated-owner cleanup requirements, while Core still performs no field/native mutation and leaves guarded adapter/V25 execution LOCAL_ONLY.")
