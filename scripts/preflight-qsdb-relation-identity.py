#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbRelationIdentityCanonicalSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB relation identity contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'ValidateOptionalCanonicalValue(project.ActiveZoneId, "active zone id");',
        'ValidateOptionalCanonicalValue(project.ActiveFloorId, "active floor id");',
        'ValidateOptionalCanonicalValue(element.FamilyId, "element " + element.Id + " family id");',
        'ValidateOptionalCanonicalValue(element.FloorId, "element " + element.Id + " floor id");',
        'ValidateOptionalCanonicalValue(element.ZoneId, "element " + element.Id + " zone id");',
        'if (value == null || value.Length == 0) return;',
        'if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))',
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing canonical relation token: " + token)
    if 'target[key] = RawValue(item, "value");' not in text:
        errors.append("QSDB relation hardening must preserve the free-text RawValue roundtrip contract.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedProjectRelations();",
        "RejectsPaddedElementRelations();",
        "AllowsEmptyOptionalRelations();",
        'project.ActiveFloorId = " F1 "',
        'element.FamilyId = " FAM "',
        "Throws<InvalidDataException>",
    ):
        if token not in text:
            errors.append("QsdbRelationIdentityCanonicalSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB optional semantic relation ids remain empty-or-canonical and cannot be silently trimmed during Save/Load.")
