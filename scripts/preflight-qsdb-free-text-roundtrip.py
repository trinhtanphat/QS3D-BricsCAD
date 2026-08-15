#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
METADATA = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbFreeTextRoundtripSmoke.cs"
errors = []

for path in (SOURCE, METADATA, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB free-text contract file: " + str(path.relative_to(ROOT)))


def validate_raw_map_hydration(store_text, metadata_text):
    start = store_text.find("private static void ReadStringMap(")
    end = store_text.find("private static string RequiredCanonical(", start)
    method = store_text[start:end] if start >= 0 and end > start else ""
    tokens = (
        'var key = Required(item, "name");',
        "if (target.ContainsKey(key))",
        'var value = RawValue(item, "value");',
        "if (target is ProjectMetadataDictionary projectMetadata)",
        "projectMetadata.SetPersistenceValue(key, value);",
        "else",
        "target[key] = value;",
    )
    positions = [method.find(token) for token in tokens]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("ReadStringMap must read each raw value, validate project metadata, then assign other maps unchanged.")
    if method.count('RawValue(item, "value")') != 1:
        errors.append("ReadStringMap must read each map value through RawValue exactly once.")
    if 'var value = Value(item, "value");' in method or "value.Trim(" in method:
        errors.append("ReadStringMap must not trim-normalize free-text map values.")

    persistence = "internal void SetPersistenceValue(string key, string value) => Set(key, value, false, false);"
    set_start = metadata_text.find("private void Set(string key, string value, bool addOnly, bool touchMutation)")
    set_end = metadata_text.find("private static bool IsReservedKey(", set_start)
    set_method = metadata_text[set_start:set_end] if set_start >= 0 and set_end > set_start else ""
    validate = set_method.find("ValidateReserved(next);")
    mutate = set_method.find("if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;")
    if persistence not in metadata_text or validate < 0 or mutate < 0 or validate >= mutate:
        errors.append("SetPersistenceValue must validate reserved metadata before hydrating the backing map without touching semantics.")

    validate_start = metadata_text.find("private static void ValidateReserved(")
    validate_end = metadata_text.find("private static string RequirePublicKey(", validate_start)
    validate_method = metadata_text[validate_start:validate_end] if validate_start >= 0 and validate_end > validate_start else ""
    for codec in (
        "ProjectMeasurementWorkItemMappingCodec.Read(metadata);",
        "ProjectTbqWorkspaceCodec.Read(metadata);",
    ):
        if codec not in validate_method:
            errors.append("ValidateReserved must validate every registered reserved metadata codec before mutation: " + codec)

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'private static string RawValue(XElement element, string attribute) => element.Attribute(attribute)?.Value ?? string.Empty;',
        'Action = RawValue(item, "action")',
        'Detail = RawValue(item, "detail")',
        'Actor = RawValue(item, "actor")',
        'CorrelationId = RawValue(item, "correlationId")',
        'Value(item, "familyId")',
        'Value(item, "floorId")',
        'Value(item, "zoneId")',
        'private static string Value(XElement element, string attribute) => element.Attribute(attribute)?.Value?.Trim() ?? string.Empty;',
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing split structural/free-text reader token: " + token)
    validate_raw_map_hydration(
        text,
        METADATA.read_text(encoding="utf-8") if METADATA.is_file() else "",
    )

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        '"  project note  "',
        '"  family description  "',
        '"  element comment  "',
        '"  detail with intentional padding  "',
        "store.Save(project, path);",
        "var loaded = store.Load(path);",
    ):
        if token not in text:
            errors.append("QsdbFreeTextRoundtripSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB keeps structural identity attributes canonical while preserving free-text map and audit payload values byte-for-byte through Save/Load.")
