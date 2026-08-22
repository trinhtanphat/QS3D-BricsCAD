#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeKeepTargetProvenanceImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeKeepTargetProvenanceImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-KEEP-TARGET-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing KeepTarget provenance file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(IMPORTER)
test = read(TEST)
doc = read(DOC)

for token in (
    'ImportMode = "KeepTargetPreserveSourceHandleProvenance"',
    "ProjectInterchangeKeepTargetImporter.Plan",
    "ProjectInterchangeSourceHandleProvenance.Plan",
    "target.FindElement(sourceElement.Id) != null",
    "ProjectInterchangeKeepTargetImporter.Import",
    "ProjectInterchangeSourceHandleProvenance.Store",
    "ProjectInterchangeProvenanceTargetMap.Store",
    "EnsureMappedTargetsDoNotOwnSourceCad",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(target)",
    "CollidedSourceElementsWithoutTargetLineage",
    "ImportInterchangeKeepTargetWithSourceHandleProvenance",
):
    if token not in source:
        errors.append("KeepTarget provenance importer missing contract token: " + token)

for token in (
    "PlanMapsOnlyActuallyAddedSourceElements",
    "ImportKeepsCollisionAndMapsOnlyAppend",
    "MissingSourceFingerprintFailsBeforeMutation",
    "AllCollisionsProduceNoFalseTargetLineage",
    "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles",
    "ProjectInterchangeProvenanceTargetMap.ReadTargetElementId",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("KeepTarget provenance smoke missing regression token: " + token)

doc_lower = doc.lower()
for token in (
    "does not create false lineage",
    "raw source-handle provenance",
    "only actually appended source Elements",
    "existing target Element",
    "never target CAD ownership",
    "portable semantic re-export",
):
    if token.lower() not in doc_lower:
        errors.append("KeepTarget provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: KeepTarget provenance maps only appended source Elements, preserves collision provenance without false target lineage, and never assigns source CAD ownership.")
