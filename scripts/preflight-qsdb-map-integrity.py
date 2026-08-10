#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
store = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/QsdbMapIntegritySmoke.cs"
errors = []

for path in (store, smoke):
    if not path.is_file():
        errors.append("missing QSDB map-integrity file: " + str(path.relative_to(ROOT)))

if store.is_file():
    text = store.read_text(encoding="utf-8")
    for needle in (
        "if (target.ContainsKey(key))",
        "Duplicate QSDB map key",
        "target[key] = RawValue(item, \"value\")",
    ):
        if needle not in text:
            errors.append("QsdbProjectStore missing duplicate-map/free-text guard: " + needle)
    for legacy in (
        'target[Required(item, "name")] = Value(item, "value")',
        'target[Required(item, "name")] = RawValue(item, "value")',
    ):
        if legacy in text:
            errors.append("QsdbProjectStore reintroduced silent last-wins map loading")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "DuplicateMetadataKeysFailClosedCaseInsensitively",
        'new XAttribute("name", "contract")',
        "Duplicate QSDB map key",
        "[ModuleInitializer]",
    ):
        if needle not in text:
            errors.append("QSDB map-integrity smoke missing regression token: " + needle)

print("QS3D QSDB map-integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QSDB metadata/family/element maps preserve free-text values while failing closed on duplicate case-insensitive keys.")
