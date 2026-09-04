#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectSchemaMigrator.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("private static void ValidatePrimaryIdentityCanonicality(XElement root)")
end = text.index("private static void RequireCanonicalAttribute(", start)
block = text[start:end]

required = (
    'RequireCanonicalOptionalAttribute(root, "activeZoneId", "Project active zone id")',
    'RequireCanonicalOptionalAttribute(root, "activeFloorId", "Project active floor id")',
    'RequireCanonicalOptionalAttribute(element, "familyId", "Project element family id")',
    'RequireCanonicalOptionalAttribute(element, "floorId", "Project element floor id")',
    'RequireCanonicalOptionalAttribute(element, "zoneId", "Project element zone id")',
    'RequireCanonicalElementValues(element.Element("handles"), "h", "Project element source handle")',
    'RequireCanonicalElementValues(element.Element("dependencies"), "d", "Project element dependency id")',
)
for marker in required:
    if marker not in block:
        fail(f"schema admission is missing persisted reference canonicality fence: {marker}")

optional_start = text.find("private static void RequireCanonicalOptionalAttribute(")
if optional_start < 0:
    fail("schema admission is missing optional-reference canonicality helper")
optional_end = text.find("private static", optional_start + len("private static void RequireCanonicalOptionalAttribute("))
optional = text[optional_start:optional_end if optional_end >= 0 else len(text)]
for marker in ('attribute?.Value', 'value.Length == 0', 'value.Trim()', 'StringComparison.Ordinal', 'InvalidDataException'):
    if marker not in optional:
        fail(f"optional-reference helper is missing fail-closed marker: {marker}")

values_start = text.find("private static void RequireCanonicalElementValues(")
if values_start < 0:
    fail("schema admission is missing reference-list canonicality helper")
values_end = text.find("private static", values_start + len("private static void RequireCanonicalElementValues("))
values = text[values_start:values_end if values_end >= 0 else len(text)]
for marker in ('Elements(itemName)', 'string.IsNullOrWhiteSpace(value)', 'value.Trim()', 'StringComparison.Ordinal', 'InvalidDataException'):
    if marker not in values:
        fail(f"reference-list helper is missing fail-closed marker: {marker}")

# The loader still trims for backwards implementation compatibility. Admission must
# reject malformed persisted identities before those normalization sites execute.
store = (ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs").read_text(encoding="utf-8")
for legacy_normalizer in (
    'ActiveZoneId = Value(root, "activeZoneId")',
    'ActiveFloorId = Value(root, "activeFloorId")',
    'Value(item, "familyId")',
    'Value(item, "floorId")',
    'Value(item, "zoneId")',
    'element.SourceHandles.Add(handle.Value.Trim())',
    'element.DependsOn.Add(dep.Value.Trim())',
):
    if legacy_normalizer not in store:
        fail(f"regression premise changed; re-audit loader normalization site: {legacy_normalizer}")

print("PASS: project schema admission rejects non-canonical persisted reference identities before hydration")
