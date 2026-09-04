#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectSchemaMigrator.cs"


def fail(message: str) -> None:
    print("FAIL: " + message)
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
validation_start = text.find("private static void ValidatePrimaryIdentityCanonicality(XElement root)")
validation_end = text.find("private static void RequireCanonicalAttribute", validation_start)
if validation_start < 0 or validation_end < 0:
    fail("cannot isolate persisted identity canonicality validation")

validation = text[validation_start:validation_end]
metadata_loop = 'foreach (var item in root.Element("metadata")?.Elements("p") ?? Enumerable.Empty<XElement>())'
metadata_pos = validation.find(metadata_loop)
key_pos = validation.find('RequireCanonicalAttribute(item, "name", "Project metadata key");', metadata_pos)
active_pos = validation.find('string.Equals(item.Attribute("name")?.Value, "ActiveFamilyId", StringComparison.OrdinalIgnoreCase)', metadata_pos)
value_pos = validation.find('RequireCanonicalAttribute(item, "value", "Active family id");', active_pos)
if metadata_pos < 0 or key_pos < 0:
    fail("project metadata keys must remain canonical before identity-bearing value validation")
if active_pos < 0 or value_pos < 0:
    fail("persisted ActiveFamilyId metadata must be rejected when its identity value is non-canonical")
if not (metadata_pos < key_pos < active_pos < value_pos):
    fail("ActiveFamilyId value canonicality must run after metadata-key admission and before hydration")

if "ValidatePrimaryIdentityCanonicality(callerRoot);" not in text:
    fail("current-schema documents must run persisted identity canonicality validation")
if "ValidatePrimaryIdentityCanonicality(root);" not in text:
    fail("migrated documents must run persisted identity canonicality validation before publication")
if "callerRoot.ReplaceWith(new XElement(root));" not in text:
    fail("migration must retain atomic caller-root replacement after validation")

active_slice = validation[active_pos:value_pos + len('RequireCanonicalAttribute(item, "value", "Active family id");')]
if ".Trim()" in active_slice:
    fail("ActiveFamilyId admission must fail closed instead of normalizing malformed identity")

print("PASS: QSDB schema admission rejects non-canonical persisted ActiveFamilyId identity for current and migrated documents")
