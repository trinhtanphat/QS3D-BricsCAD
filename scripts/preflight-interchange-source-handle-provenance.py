#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenanceStore.cs"
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeAppendProvenanceImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeAppendProvenanceImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-SOURCE-HANDLE-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing interchange provenance file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


store = read(STORE)
importer = read(IMPORTER)
test = read(TEST)
doc = read(DOC)

for token in (
    'MetadataKey = "Interchange.SourceHandleProvenance.v1"',
    "ProjectInterchangeSourceHandleProvenanceRecord",
    "SourceDrawingFingerprint",
    "SourceElementId",
    "TargetElementId",
    "SourceHandles",
    "DtdProcessing.Prohibit",
    "XmlResolver = null",
    "MaxPayloadChars = 1024 * 1024",
    "MaxRecords = 50000",
    "MaxTotalHandles = 100000",
    "sourceToTargetElementIds",
    "target element does not exist",
):
    if token not in store:
        errors.append("source-handle provenance store missing contract token: " + token)

for token in (
    'ImportMode = "AppendOnlyPreserveSourceHandleProvenance"',
    "ProjectInterchangeAppendOnlyImporter.Import",
    "EnsureImportedElementsDoNotOwnSourceCad",
    "ProjectInterchangeSourceHandleProvenanceStore.Append",
    "ProjectStateSnapshot.Capture",
    "snapshot.Restore(target)",
    "SourceHandles.Count != 0",
    "DrawingFingerprint",
    "ImportInterchangeAppendSourceHandleProvenance",
):
    if token not in importer:
        errors.append("append provenance importer missing contract token: " + token)

for token in (
    "PlanIsReadOnlyAndAccountsForHandles",
    "ImportPreservesHandlesOnlyInLedger",
    "MissingSourceFingerprintFailsBeforeMutation",
    "CorruptExistingLedgerRollsBackSemanticAppend",
    "portableTarget",
    "SourceHandles.Count",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("append provenance smoke missing regression token: " + token)

for token in (
    "provenance only",
    "never becomes target CAD ownership",
    "source drawing fingerprint",
    "ProjectElement.SourceHandles",
    "portable semantic snapshot",
    "append-only",
):
    if token not in doc:
        errors.append("source-handle provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: source handles can be preserved in a bounded drawing-provenance ledger without becoming target element ownership or portable snapshot authority.")
