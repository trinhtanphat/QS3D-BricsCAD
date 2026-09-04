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
canonical = 'RequireCanonicalAttribute(item, "name", "Project metadata key");'
canonical_pos = validation.find(canonical, metadata_pos)
if metadata_pos < 0 or canonical_pos < 0 or canonical_pos < metadata_pos:
    fail("current-schema and migrated documents must reject padded project metadata keys before hydration")

migration_start = text.find("private static void MigrateV3ToV4(XElement root)")
migration_end = text.find("private static void ValidatePrimaryIdentityCanonicality", migration_start)
if migration_start < 0 or migration_end < 0:
    fail("cannot isolate v3-to-v4 migration")
migration = text[migration_start:migration_end]
if "StartsWith(ProjectMeasurementWorkItemMappingCodec.Prefix, StringComparison.OrdinalIgnoreCase)" not in migration:
    fail("v3-to-v4 migration must preserve the reserved measurement/work-item namespace guard")

if "ValidatePrimaryIdentityCanonicality(callerRoot);" not in text:
    fail("current-schema documents must run persisted identity canonicality validation")
if "ValidatePrimaryIdentityCanonicality(root);" not in text:
    fail("migrated documents must run persisted identity canonicality validation before publication")
if "callerRoot.ReplaceWith(new XElement(root));" not in text:
    fail("migration must retain atomic caller-root replacement after validation")

print("PASS: QSDB schema admission rejects padded project metadata keys for current and migrated documents")
