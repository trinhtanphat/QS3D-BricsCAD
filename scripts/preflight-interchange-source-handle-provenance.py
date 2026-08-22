#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROVENANCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs"
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeAppendProvenanceImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeAppendProvenanceImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-APPEND-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing append provenance file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


provenance = read(PROVENANCE)
importer = read(IMPORTER)
test = read(TEST)
doc = read(DOC)

for token in (
    'MetadataPrefix = "Interchange.Provenance.Source."',
    'PolicyName = "PreserveAsProvenanceOnly"',
    "ProjectElement.SourceHandles",
    "ProjectStateSnapshot.Capture",
    "ReadSourceHandles",
    "No imported handle was assigned to target DWG ownership",
    "catch (Exception operationError)",
    "catch (Exception rollbackError)",
    'new AggregateException(operationError, rollbackError)',
    "Interchange source-handle provenance storage failed and project rollback also failed.",
):
    if token not in provenance:
        errors.append("canonical provenance implementation missing contract token: " + token)

if "catch\n            {\n                rollback.Restore(target);\n                throw;\n            }" in provenance:
    errors.append("canonical provenance store still allows rollback failure to mask the original storage error")

for token in (
    'ImportMode = "AppendOnlyPreserveSourceHandleProvenance"',
    "ProjectInterchangeAppendOnlyImporter.Plan",
    "ProjectInterchangeSourceHandleProvenance.Plan",
    "ProjectInterchangeAppendOnlyImporter.Import",
    "ProjectInterchangeSourceHandleProvenance.Store",
    "EnsureProvenanceCanBeScoped",
    "EnsureImportedElementsDoNotOwnSourceCad",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(target)",
    "SourceHandles.Count != 0",
    "ImportInterchangeAppendWithSourceHandleProvenance",
):
    if token not in importer:
        errors.append("combined append provenance importer missing contract token: " + token)

for token in (
    "PlanIsReadOnlyAndAccountsForHandles",
    "ImportPreservesHandlesOnlyAsCanonicalProvenance",
    "MissingSourceFingerprintFailsBeforeMutation",
    "EmptyHandleSetDoesNotRequireFingerprint",
    "ExistingSourceProvenanceIsReplacedByCombinedImport",
    "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles",
    "portableTarget",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("combined append provenance smoke missing regression token: " + token)

for token in (
    "existing canonical operations",
    "never become target CAD ownership",
    "source drawing fingerprint",
    "ProjectElement.SourceHandles",
    "portable semantic re-export",
    "all-new append",
    "does not add a second raw-handle ledger",
):
    if token not in doc:
        errors.append("combined append provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: append-only semantic import preserves source handles only as provenance, and provenance rollback failures cannot mask the original storage failure.")
