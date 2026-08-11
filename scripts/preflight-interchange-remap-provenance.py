#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MAP = ROOT / "src/QS3D.Core/Export/ProjectInterchangeProvenanceTargetMap.cs"
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeRemapProvenanceImporter.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapProvenanceImporterSmoke.cs"
DOC = ROOT / "docs/INTERCHANGE-REMAP-PROVENANCE.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing remap provenance file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


mapping = read(MAP)
importer = read(IMPORTER)
test = read(TEST)
doc = read(DOC)

for token in (
    'MetadataPrefix = "Interchange.Provenance.TargetMap."',
    "source semantic Element id -> imported target semantic Element id lineage",
    "Mapping contains no CAD ownership handles",
    "ReadTargetElementId",
    "target.FindElement",
    "one-to-one",
    "ProjectStateSnapshot.Capture",
    "catch (Exception operationError)",
    "catch (Exception rollbackError)",
    "new AggregateException(operationError, rollbackError)",
    "Interchange provenance target-map storage failed and project rollback also failed.",
):
    if token not in mapping:
        errors.append("provenance target map missing contract token: " + token)

if "catch\n            {\n                rollback.Restore(target);\n                throw;\n            }" in mapping:
    errors.append("provenance target map still allows rollback failure to mask the original storage error")

for token in (
    'ImportMode = "RemapAppendAsNewPreserveSourceHandleProvenance"',
    "ProjectInterchangeRemapAppendImporter.Plan",
    "ProjectInterchangeSourceHandleProvenance.Plan",
    "semanticPlan.Remap.MapId",
    "ProjectInterchangeRemapAppendImporter.Import",
    "ProjectInterchangeSourceHandleProvenance.Store",
    "ProjectInterchangeProvenanceTargetMap.Store",
    "EnsureMappedTargetsDoNotOwnSourceCad",
    "ProjectStateSnapshot.Capture",
    "rollback.Restore(target)",
    "ImportInterchangeRemapWithSourceHandleProvenance",
):
    if token not in importer:
        errors.append("remap provenance importer missing contract token: " + token)

for token in (
    "PlanBuildsOneToOneSourceTargetLineage",
    "ImportKeepsRawHandlesOutsideMappedTargetOwnership",
    "MissingSourceFingerprintFailsBeforeMutation",
    "TargetMapRejectsMissingTargetElement",
    "ProjectInterchangeProvenanceTargetMap.ReadTargetElementId",
    "ProjectInterchangeSourceHandleProvenance.ReadSourceHandles",
    "portableTarget",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("remap provenance smoke missing regression token: " + token)

for token in (
    "two complementary records",
    "raw source handles",
    "source-to-target semantic lineage",
    "never target CAD ownership",
    "Import As New",
    "portable semantic re-export",
    "LOCAL_ONLY",
):
    if token not in doc:
        errors.append("remap provenance documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Import As New retains canonical raw-handle provenance plus semantic lineage, and target-map rollback failures cannot mask the original storage failure.")
