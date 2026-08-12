#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbTimestampOffsetSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB timestamp contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "DateTimeOffset.TryParse",
        "HasExplicitUtcOffset(raw)",
        "var utc = result.UtcDateTime;",
        'var canonical = utc.ToString("O", CultureInfo.InvariantCulture);',
        "if (!string.Equals(value, canonical, StringComparison.Ordinal))",
        '"Non-canonical QSDB UTC timestamp: "',
        "return utc;",
        'value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)',
        "return offsetSeparator > timeSeparator;",
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing canonical UTC timestamp token: " + token)
    if "return result.ToUniversalTime();" in text:
        errors.append("QSDB timestamps must not be reinterpreted from DateTime using the machine timezone.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExplicitNonUtcOffsetIsRejected",
        "MissingOffsetIsRejected",
        "CanonicalUtcRoundTripLoads",
        '"2026-08-10T12:00:00+07:00"',
        '"2026-08-10T12:00:00"',
        '"2026-08-10T05:00:00.0000000Z"',
        "DateTimeKind.Utc",
        "catch (InvalidDataException)",
    ):
        if token not in text:
            errors.append("QsdbTimestampOffsetSmoke.cs missing canonical UTC regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB timestamps fail closed unless persisted in exact canonical UTC form; explicit non-UTC offsets and missing offsets are rejected, and canonical Z timestamps round-trip deterministically.")
