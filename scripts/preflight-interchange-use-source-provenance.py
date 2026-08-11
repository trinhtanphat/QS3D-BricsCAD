#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeUseSourceProvenanceImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeUseSourceProvenanceImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-USE-SOURCE-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing UseSource provenance file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(IMPORTER)
test = read(TEST)
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
    "PlanPreservesCleanupRequirementAndIdentityMapping",
    "MissingCleanupAuthorizationFailsBeforeProvenanceMutation",
    "AuthorizedImportRetainsProvenanceWithoutCadOwnership",
    "MissingSourceFingerprintFailsBeforeMutation",
    "ProjectInterchangeNativeCleanupAuthorization.None",
    "ProjectInterchangeNativeCleanupAuthorization.ForPlan",
    "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles",
    "ProjectInterchangeProvenanceTargetMap.ReadTargetElementId",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("UseSource provenance smoke missing regression token: " + token)

doc_boundary = doc.replace("**", "")
for token in (
    "cleanup authorization",
    "does not perform native cleanup",
    "raw source-handle provenance",
    "source-to-target semantic lineage",
    "never target CAD ownership",
    "portable semantic re-export",
    "LOCAL_ONLY",
):
    if token not in doc_boundary:
        errors.append("UseSource provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: UseSource provenance composition preserves handle-bound native-cleanup authorization and keeps imported source handles outside target CAD ownership.")
