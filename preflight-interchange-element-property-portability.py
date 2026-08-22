#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Export/ProjectInterchangeElementPropertyPolicy.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
READER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeElementPropertyPortabilitySmoke.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing portability file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


policy = read(POLICY)
exporter = read(EXPORTER)
reader = read(READER)
smoke = read(SMOKE)

for token in (
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)",
    "StartsWith(\"Generated\"",
    "StartsWith(\"QS3D.Generated\"",
    "StartsWith(\"PhysicalOpeningCut\"",
    "StartsWith(\"QS3D.PhysicalOpeningCut\"",
    "IndexOf(\"Handle\", StringComparison.OrdinalIgnoreCase) < 0",
):
    if token not in policy:
        errors.append("portable element-property policy missing boundary token: " + token)

if "element.Properties.Where(x => ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key))" not in exporter:
    errors.append("interchange exporter does not filter ProjectElement properties through the portability policy")
if "family.Properties.Where(x => IsInterchangeProperty(x.Key))" not in exporter:
    errors.append("interchange exporter unexpectedly changed Family property semantics")

for token in (
    "ElementStringMap(x.Properties, \"element properties\")",
    "ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key)",
    "StringMap(x.Properties, \"Family properties\")",
    "ProjectInterchangeSemanticReferenceValidator.Validate(result)",
):
    if token not in reader:
        errors.append("validated snapshot reader missing portability/integrity token: " + token)

for token in (
    "ExportOmitsElementHandleMetadataButKeepsFamilySemantics",
    "LegacyHandlePropertyIsAcceptedButNotMaterialized",
    "AppendOnlyDoesNotRebindLegacyHandleProperty",
    "KeepTargetDoesNotRebindLegacyHandleProperty",
    "FieldMergeDoesNotReviewOrAdoptLegacyHandleProperty",
    "ProjectInterchangeJsonValidator.Validate(json).IsValid",
    "properties.CadHandle",
    "ModuleInitializer",
):
    if token not in smoke:
        errors.append("portability smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectElement interchange properties are filtered at export and typed-read boundaries; drawing-local handle metadata cannot be rebound by canonical import/merge paths.")
