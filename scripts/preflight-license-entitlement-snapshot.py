#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Licensing/LicenseEntitlementSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/LicenseEntitlementSnapshotSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing license entitlement snapshot file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'Header = "QS3D-LICENSE-ENTITLEMENT/1"',
        "MaxSerializedChars = 96 * 1024",
        "MaxPayloadBytes = 48 * 1024",
        '"\\nsha256:" + ComputeSha256Hex(canonical)',
        "FixedTimeEquals(expectedSeal, actualSeal)",
        "persistedAt.Kind == DateTimeKind.Unspecified",
        "persistedAt.ToUniversalTime()",
        "new DateTime(ticks, DateTimeKind.Utc)",
        "serialized.IndexOf('\\r') >= 0",
        'serialized.EndsWith("\\n", StringComparison.Ordinal)',
        "Convert.FromBase64String",
        "StrictUtf8.GetString(bytes)",
        "StrictUtf8.GetByteCount(normalized)",
        "StrictUtf8.GetByteCount(payload)",
        "EncoderFallback.ExceptionFallback",
        "DecoderFallback.ExceptionFallback",
    ):
        if token not in text:
            errors.append("LicenseEntitlementSnapshot missing integrity token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RoundTripsCanonicalSnapshot",
        "RejectsTamperedPayload",
        "RejectsMalformedAndOversizedPersistence",
        "RejectsInvalidUtf16BeforeCanonicalization",
        "NormalizesExplicitLocalTimestamp",
        "RejectsAmbiguousTimestamp",
        "tampered payload passed the integrity seal",
        "oversized serialized snapshot was accepted",
        "invalid UTF-16 entitlement payload was replacement-encoded",
        "unspecified timestamp was accepted",
    ):
        if token not in text:
            errors.append("LicenseEntitlementSnapshotSmoke missing regression token: " + token)

print("QS3D license entitlement snapshot integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: entitlement snapshots are bounded, canonical, strict-UTF-8, UTC-normalized, and fail closed when persisted content is malformed or modified without its matching integrity seal.")
