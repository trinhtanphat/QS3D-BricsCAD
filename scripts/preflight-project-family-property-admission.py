#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyPropertyAdmissionSmoke.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "RequirePropertyKey",
    "RequirePropertyValue",
    "private const int MaxPropertyKeyLength = 120;",
    "private const int MaxPropertyValueLength = 1000;",
    "Family property key must be canonical without surrounding whitespace.",
    "value.Length > MaxPropertyKeyLength",
    "persistedValue.Length > MaxPropertyValueLength",
    "PersistedTextXml.Verify(persistedValue",
    "var canonicalKey = RequirePropertyKey(key);",
    "var persistedValue = RequirePropertyValue(value);",
    "var canonicalKey = RequirePropertyKey(property.Key);",
    "var persistedValue = RequirePropertyValue(property.Value);",
)
for token in required_source:
    if token not in source:
        fail(f"ProjectFamily persistence admission must retain source token: {token}")

indexer_start = source.index("public string this[string key]")
indexer_end = source.index("public ICollection<string> Keys", indexer_start)
indexer = source[indexer_start:indexer_end]
validate_key = indexer.find("RequirePropertyKey(key)")
validate_value = indexer.find("RequirePropertyValue(value)")
callback = indexer.find("_beforeMutation()")
write = indexer.find("_inner[canonicalKey] = persistedValue")
if min(validate_key, validate_value, callback, write) < 0 or not (validate_key < validate_value < callback < write):
    fail("family property indexer must validate/normalize before persistence callback and write")

add_start = source.index("public void Add(string key, string value)", indexer_end)
add_end = source.index("public void Add(KeyValuePair<string, string> item)", add_start)
add = source[add_start:add_end]
add_validate_key = add.find("RequirePropertyKey(key)")
add_duplicate_check = add.find("_inner.ContainsKey(canonicalKey)")
add_validate_value = add.find("RequirePropertyValue(value)")
add_callback = add.find("_beforeMutation()")
add_write = add.find("_inner.Add(canonicalKey, persistedValue)")
if min(add_validate_key, add_duplicate_check, add_validate_value, add_callback, add_write) < 0 or not (
    add_validate_key < add_duplicate_check < add_validate_value < add_callback < add_write
):
    fail("family property Add must validate key, preserve duplicate precedence, then validate value before persistence callback and write")

restore_start = source.index("internal void RestoreSnapshotState")
restore_end = source.index("private static string RequirePropertyKey", restore_start)
restore = source[restore_start:restore_end]
if restore.find("RequirePropertyKey(property.Key)") < 0 or restore.find("RequirePropertyValue(property.Value)") < 0:
    fail("snapshot restore must validate family property key/value state before replacement")
if restore.find("RequirePropertyKey(property.Key)") > restore.find("_properties.ReplaceSnapshotState(nextProperties)"):
    fail("snapshot family-property validation must precede live state replacement")

required_smoke = (
    "RejectsNonPersistablePropertyKeysWithoutMutation",
    "RejectsXmlInvalidPropertyValuesWithoutMutation",
    "RejectsNonPersistableAddWithoutMutation",
    "RejectsOversizedPropertyStateWithoutMutation",
    "NormalizesNullPropertyValueBeforeMutation",
    "PreservesCaseInsensitiveAndDuplicateSemantics",
    "PreservesRemoveAndClearMutationSemantics",
)
for token in required_smoke:
    if token not in smoke:
        fail(f"deterministic smoke must retain {token}")

print("PASS: ProjectFamily properties validate canonical bounded XML-persistable state before mutation while preserving dictionary semantics")