#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / "src/QS3D.Core/Export/ProjectInterchangeElementPropertyPolicy.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
READER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
FIELD_MERGE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeFieldMergeImporter.cs"
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
field_merge = read(FIELD_MERGE)
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

# Export portability filtering must remain explicit while executing inside the bounded
# map helper so the first over-limit portable member fails before retain/sort.
for token, label in (
    (
        'AppendStringMap(json, element.Properties, ProjectInterchangeElementPropertyPolicy.IsPortable, 3, "element properties");',
        "ProjectElement portability predicate is not routed through bounded map export",
    ),
    (
        'AppendStringMap(json, family.Properties, IsInterchangeProperty, 2, "family properties");',
        "Family interchange predicate is not routed through bounded map export",
    ),
    ("Func<string, bool> include", "bounded string-map helper no longer accepts an explicit inclusion predicate"),
    ("if (!include(item.Key)) continue;", "bounded string-map helper no longer filters before retention"),
    ("if (items.Count >= MaxInterchangeMapItems)", "bounded string-map helper no longer guards retained portable members"),
):
    if token not in exporter:
        errors.append(label + ": missing " + token)

for stale in (
    "element.Properties.Where(x => ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key))",
    "family.Properties.Where(x => IsInterchangeProperty(x.Key))",
):
    if stale in exporter:
        errors.append("interchange exporter must filter inside the bounded helper rather than eagerly materializing the old LINQ shape: " + stale)

for token in (
    "ElementStringMap(x.Properties, \"element properties\")",
    "ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key)",
    "StringMap(x.Properties, \"Family properties\")",
    "ProjectInterchangeSemanticReferenceValidator.Validate(result)",
):
    if token not in reader:
        errors.append("validated snapshot reader missing portability/integrity token: " + token)

if ".Where(x => !ProjectInterchangeElementPropertyPolicy.IsPortable(x.Key))" not in field_merge:
    errors.append("field merge does not preserve target-local nonportable element properties while applying portable source semantics")

for token in (
    "ExportOmitsElementHandleMetadataButKeepsFamilySemantics",
    "LegacyHandlePropertyIsAcceptedButNotMaterialized",
    "AppendOnlyDoesNotRebindLegacyHandleProperty",
    "KeepTargetDoesNotRebindLegacyHandleProperty",
    "FieldMergeDoesNotReviewOrAdoptLegacyHandleProperty",
    "ProjectInterchangeJsonValidator.Validate(json).IsValid",
    "properties.CadHandle",
    "Equal(\"TARGET-CAD\", element.Properties[\"CadHandle\"])",
    "ModuleInitializer",
):
    if token not in smoke:
        errors.append("portability smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectElement interchange properties are filtered through the bounded export helper and at read boundaries; source handle metadata is never rebound and FieldMerge preserves target-local nonportable metadata outside the reviewed semantic plan.")
