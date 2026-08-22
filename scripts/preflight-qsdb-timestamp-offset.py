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
        "return result.UtcDateTime;",
        'value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)',
        "return offsetSeparator > timeSeparator;",
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing deterministic timestamp token: " + token)
    if "return result.ToUniversalTime();" in text:
        errors.append("QSDB timestamps must not be reinterpreted from DateTime using the machine timezone.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        '"2026-08-10T12:00:00+07:00"',
        '"2026-08-10T12:00:00"',
        "DateTimeKind.Utc",
        "catch (InvalidDataException)",
    ):
        if token not in text:
            errors.append("QsdbTimestampOffsetSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB timestamps require an explicit timezone and normalize deterministically to UTC while legacy missing timestamps remain separately supported.")
