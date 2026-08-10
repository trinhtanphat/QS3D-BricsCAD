#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREVIEW = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportPreview.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportPreviewSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-IMPORT-PREVIEW.md"
errors = []

for path in (PREVIEW, VALIDATOR, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing interchange import-preview contract file: " + str(path.relative_to(ROOT)))

if PREVIEW.is_file():
    text = PREVIEW.read_text(encoding="utf-8")
    required = (
        "public const int MaxDetailedItems = 10000",
        "var validation = ProjectInterchangeJsonValidator.Validate(json);",
        "if (!validation.IsValid)",
        "var manifest = ParseValidatedManifest(json);",
        "InterchangeIdentityDisposition.New",
        "InterchangeIdentityDisposition.ExistingNeedsPolicy",
        "InterchangeIdentityDisposition.ExistingIncompatible",
        "InterchangeDrawingFingerprintRelation.Unknown",
        "InterchangeDrawingFingerprintRelation.Match",
        "InterchangeDrawingFingerprintRelation.Different",
        "Import preview refuses ambiguous target identity",
        "total > items.Count",
        "Items = (items ?? Enumerable.Empty<InterchangeImportPreviewItem>()).ToList().AsReadOnly()",
        "Enum.TryParse<ElementCategory>((raw ?? string.Empty).Trim(), false, out var category)",
        "Enum.IsDefined(typeof(ElementCategory), category)",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectInterchangeImportPreview.cs missing preview/fail-closed token: " + token)
    if text.find("var validation = ProjectInterchangeJsonValidator.Validate(json);") > text.find("var manifest = ParseValidatedManifest(json);"):
        errors.append("import preview must validate before parsing its collision manifest")
    for token in (
        "targetProject.Zones.Add(", "targetProject.Floors.Add(", "targetProject.Families.Add(", "targetProject.Elements.Add(",
        "targetProject.Name =", "targetProject.DrawingFingerprint =", "targetProject.ActiveZoneId =", "targetProject.ActiveFloorId =",
        "targetProject.Touch(", "ProjectStateSnapshot.Restore(", "GeneratedSolidHandle", "sourceHandles",
    ):
        if token in text:
            errors.append("read-only import preview contains forbidden mutation/ownership authority token: " + token)

if VALIDATOR.is_file():
    text = VALIDATOR.read_text(encoding="utf-8")
    for token in (
        "ProjectInterchangeJsonExporter.FormatName",
        "ProjectInterchangeJsonExporter.FormatVersion",
        "sourceRefScope must be exactly 'drawing-local'",
        "new UTF8Encoding(false, true)",
        '"COLLECTION_MISSING"',
    ):
        if token not in text:
            errors.append("interchange validator lost an import-preview prerequisite: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NewIdentitiesArePreviewedWithoutMutation",
        "ExistingSameCategoryRequiresPolicy",
        "CategoryMismatchIsIncompatible",
        "InvalidSnapshotStopsBeforeCollisionPlanning",
        "FingerprintRelationIsDescriptive",
        "AmbiguousTargetIdsFailClosed",
    ):
        if token not in text:
            errors.append("ProjectInterchangeImportPreviewSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "ProjectInterchangeImportPreviewSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("interchange import-preview smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "read-only collision/provenance preview",
        "not import permission",
        "sourceRefScope = drawing-local",
        "does not reconstruct any of them",
        "Do **not** add `QS3DINTERCHANGEIMPORT`",
        "REMOTE_DONE as read-only import planning only",
    ):
        if token not in text:
            errors.append("INTERCHANGE-IMPORT-PREVIEW.md missing safety/import boundary: " + token)

print("QS3D interchange import-preview preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Semantic Snapshot v1 collision preview is validation-first, bounded, immutable-output and target-read-only; merge/import, source-handle rebinding and native ownership reconstruction remain explicitly unimplemented.")
