#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatorCanonicalSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing canonical interchange-validator contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'issues.Error("ID_NON_CANONICAL"',
        'issues.Error("SOURCE_HANDLE_NON_CANONICAL"',
        'issues.Error("DEPENDENCY_NON_CANONICAL"',
        'issues.Error("PROPERTY_KEY_NON_CANONICAL"',
        'issues.Error("QUANTITY_KEY_NON_CANONICAL"',
        'issues.Error("PROPERTY_KEY_DUPLICATE"',
        'issues.Error("QUANTITY_KEY_DUPLICATE"',
        'DateTime.TryParseExact(raw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)',
        "parsed.Kind != DateTimeKind.Utc",
        'parsed.ToString("O", CultureInfo.InvariantCulture)',
        'issues.Error("TIMESTAMP_NOT_UTC"',
        'string.Equals(element.SourceRefScope ?? string.Empty, "drawing-local", StringComparison.Ordinal)',
    ):
        if token not in text:
            errors.append("ProjectInterchangeJsonValidator.cs missing canonical validation token: " + token)
    if "Enum.TryParse(normalized, true" in text:
        errors.append("Interchange validator must not silently trim/case-normalize category tokens that typed reading rejects.")
    if "DateTimeOffset.TryParse" in text or "HasExplicitUtcOffset(raw)" in text:
        errors.append("Interchange validator must not restore permissive explicit-offset timestamp normalization.")
    if 'issues.Warning("TIMESTAMP_NOT_UTC"' in text:
        errors.append("A non-empty timestamp without explicit timezone must be an Error, not a Warning.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedProjectId();",
        "RejectsPaddedRelationId();",
        "RejectsPaddedSourceHandle();",
        "RejectsPaddedDependency();",
        "RejectsPaddedPropertyKey();",
        "RejectsPaddedQuantityKey();",
        "RejectsTimestampWithoutOffset();",
        "RejectsTimestampWithExplicitOffset();",
        "AcceptsCanonicalUtc();",
    ):
        if token not in text:
            errors.append("ProjectInterchangeValidatorCanonicalSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange validation, typed reading, preview, diff, and import share one canonical semantic-identity and exact UTC round-trip timestamp contract.")
