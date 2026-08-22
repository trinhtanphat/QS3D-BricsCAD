#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbFreeTextRoundtripSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB free-text contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'target[key] = RawValue(item, "value");',
        'private static string RawValue(XElement element, string attribute) => element.Attribute(attribute)?.Value ?? string.Empty;',
        'Action = RawValue(item, "action")',
        'Detail = RawValue(item, "detail")',
        'Actor = RawValue(item, "actor")',
        'CorrelationId = RawValue(item, "correlationId")',
        'new ProjectElement(Required(item, "id"), category, Value(item, "familyId")',
        'private static string Value(XElement element, string attribute) => element.Attribute(attribute)?.Value?.Trim() ?? string.Empty;',
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing split structural/free-text reader token: " + token)
    if 'target[key] = Value(item, "value");' in text:
        errors.append("QSDB map values must not be trimmed during load.")

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

print("PASS: QSDB keeps structural attributes canonical while preserving free-text map and audit payload values byte-for-byte through Save/Load.")
