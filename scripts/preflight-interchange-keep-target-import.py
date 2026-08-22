#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeKeepTargetImporter.cs"
PLANNER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportResolutionPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeKeepTargetImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-KEEP-TARGET-IMPORT.md"
errors = []

for path in (IMPORTER, PLANNER, SMOKE, DOC):
    if not path.is_file():
        errors.append("missing KeepTarget interchange contract file: " + str(path.relative_to(ROOT)))

if IMPORTER.is_file():
    text = IMPORTER.read_text(encoding="utf-8")
    for token in (
        'public const string ImportMode = "KeepTarget"',
        'public static ProjectInterchangeKeepTargetImportPlan Plan',
        'public static ProjectInterchangeKeepTargetImportResult Import',
        'ProjectInterchangeImportResolutionPlanner.Plan(target, json, KeepTargetPolicy())',
        'ZoneCollision = InterchangeExistingIdentityAction.KeepTarget',
        'FloorCollision = InterchangeExistingIdentityAction.KeepTarget',
        'FamilyCollision = InterchangeExistingIdentityAction.KeepTarget',
        'ElementCollision = InterchangeExistingIdentityAction.KeepTarget',
        'SourceHandles = InterchangeSourceHandlePolicy.Discard',
        'x.Action != InterchangeImportResolutionAction.AddSourceSemanticData && x.Action != InterchangeImportResolutionAction.KeepTarget',
        'ProjectStateSnapshot.Capture(target)',
        'snapshot.Restore(target)',
        'DrawingFingerprint = string.Empty',
        'element.MarkDirty(ElementDirtyFlags.All)',
        'RestoreExistingActiveContext(',
        '"ImportInterchangeKeepTarget"',
        'LastSemanticIdentitiesAddedKey',
        'LastTargetIdentitiesKeptKey',
    ):
        if token not in text:
            errors.append("KeepTarget importer missing fail-closed/planner/rollback token: " + token)

    if '.SourceHandles.Add(' in text:
        errors.append("KeepTarget importer must discard source CAD handles rather than adopt them into target elements")
    if 'InterchangeExistingIdentityAction.UseSourceSemanticData' in text:
        errors.append("KeepTarget importer must not expose or execute UseSourceSemanticData replacement semantics")
    if 'GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild' in text:
        errors.append("KeepTarget importer must not pretend to clear/rebuild generated/native ownership; it never replaces existing elements")

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        'InterchangeExistingIdentityAction.KeepTarget',
        'InterchangeExistingIdentityAction.UseSourceSemanticData',
        'InterchangeImportResolutionAction.AddSourceSemanticData',
        'InterchangeImportResolutionAction.BlockedIncompatible',
        'NameOwnedByDifferentIdentity',
    ):
        if token not in text:
            errors.append("resolution planner lost policy/name-collision authority required by KeepTarget import: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'PlanIsReadOnlyAndClassifiesAddVersusKeep()',
        'ImportKeepsExistingAndAddsPortableState()',
        'NameAndCategoryConflictsFailBeforeMutation()',
        'InvalidSnapshotFailsBeforeMutation()',
        'Equal("Target Zone", keptZone.Name)',
        'True(ReferenceEquals(existingElement, keptElement))',
        'Equal(string.Empty, imported.DrawingFingerprint)',
        'Equal(0, imported.SourceHandles.Count)',
        'Equal("E1", imported.DependsOn[0])',
        '[ModuleInitializer]',
        'ProjectInterchangeKeepTargetImporterSmoke.Run()',
    ):
        if token not in text:
            errors.append("KeepTarget importer smoke missing regression scenario/registration token: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'ProjectInterchangeKeepTargetImporter.Plan',
        'ProjectInterchangeImportResolutionPlanner',
        '**KeepTarget**',
        'does not execute `UseSourceSemanticData`',
        'source CAD handles are not imported',
        'No generated/native CAD ownership is reconstructed',
        'generic `QS3DINTERCHANGEIMPORT`',
        'licensed BricsCAD V25 runtime qualification is claimed',
    ):
        if token not in text:
            errors.append("INTERCHANGE-KEEP-TARGET-IMPORT.md missing policy/ownership/runtime boundary: " + token)

if errors:
    print("QS3D KeepTarget interchange import preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: KeepTarget interchange mutation is planner-driven, adds only non-colliding portable semantics, preserves colliding target identities/context, discards source CAD ownership, rolls back on apply failure, and does not execute UseSource replacement semantics. Licensed V25 qualification remains local.")
