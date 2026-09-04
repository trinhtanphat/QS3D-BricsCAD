#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectSchemaMigrator.cs"


def fail(message: str) -> None:
    print("FAIL: " + message)
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.find("private static void MigrateV3ToV4(XElement root)")
end = text.find("private static void ValidatePrimaryIdentityCanonicality", start)
if start < 0 or end < 0:
    fail("cannot isolate v3-to-v4 migration")

block = text[start:end]
metadata_loop = "foreach (var item in metadata.Elements(\"p\"))"
loop_pos = block.find(metadata_loop)
canonical = 'RequireCanonicalAttribute(item, "name", "Project metadata key");'
canonical_pos = block.find(canonical)
reserved = "StartsWith(ProjectMeasurementWorkItemMappingCodec.Prefix, StringComparison.OrdinalIgnoreCase)"
reserved_pos = block.find(reserved)

if loop_pos < 0:
    fail("v3-to-v4 migration must inspect every persisted metadata entry before migration")
if canonical_pos < 0:
    fail("v3-to-v4 migration must reject non-canonical persisted metadata names")
if reserved_pos < 0:
    fail("v3-to-v4 migration must preserve the reserved measurement/work-item namespace guard")
if not (loop_pos < canonical_pos < reserved_pos):
    fail("metadata canonicality must be admitted before reserved-prefix semantics are evaluated")

if "callerRoot.ReplaceWith(new XElement(root));" not in text:
    fail("migration must retain atomic caller-root replacement after validation")

print("PASS: schema migration rejects padded metadata identities before reserved-namespace admission")
