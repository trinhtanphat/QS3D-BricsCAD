#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RevisionSnapshotStoreIntegritySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (STORE, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing revision-store integrity file: " + str(path.relative_to(ROOT)))

if STORE.is_file():
    text = STORE.read_text(encoding="utf-8")
    for token in (
        "ValidateSnapshot(snapshot);",
        "ValidateUtcTimestamp(snapshot.CreatedUtc",
        'snapshot.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)',
        'DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)',
        "result.Kind != DateTimeKind.Utc",
        '!string.Equals(value, result.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal)',
        'throw new InvalidDataException("Invalid or non-canonical revision timestamp.");',
        "return result;",
        "Enum.IsDefined(typeof(ElementCategory), category)",
        "ValidateCanonicalCategory(element.Category)",
        "CanonicalRequired(property, \"name\", \"revision property name\")",
        "Duplicate revision property:",
        "property.Attribute(\"value\")?.Value ?? string.Empty",
        "ValidateCanonicalStringList(element.SourceHandles",
        "Duplicate revision source handle:",
        ".SourceHandles.OrderBy(h => h, StringComparer.OrdinalIgnoreCase)",
    ):
        if token not in text:
            errors.append("RevisionSnapshotStore missing persistence-integrity token: " + token)

    for forbidden in (
        "snapshot.CreatedUtc.ToUniversalTime()",
        "DateTimeOffset.TryParse",
        "HasExplicitUtcOffset(value)",
        "return result.UtcDateTime",
        "return result.ToUniversalTime()",
        "x.SourceHandles.Where(h => !string.IsNullOrWhiteSpace(h))",
        ".Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(h => h",
        "new XAttribute(\"value\", h.Trim())",
    ):
        if forbidden in text:
            errors.append("RevisionSnapshotStore reintroduced normalization or lossy persistence: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CanonicalUtcLoadsAndNonCanonicalTimestampsFailClosed",
        'RevisionDocument("2026-08-10T05:00:00.0000000Z")',
        'RevisionDocument("2026-08-10T12:00:00.0000000+07:00")',
        'RevisionDocument("2026-08-10T05:00:00.0000000+00:00")',
        'RevisionDocument("2026-08-10T05:00:00.0000000")',
        'RevisionDocument("2026-08-10T05:00:00Z")',
        "DateTimeKind.Utc",
        "DateTimeKind.Unspecified",
        "DateTimeKind.Local",
        "Throws<InvalidDataException>(() => store.Load(offsetPath));",
        "Throws<InvalidDataException>(() => store.Load(zeroOffsetPath));",
        "Throws<InvalidDataException>(() => store.Load(missingOffsetPath));",
        "Throws<InvalidDataException>(() => store.Load(shortUtcPath));",
        "SaveRequiresUtcAndCanonicalDefinedCategory",
        'Snapshot("undefined", "999")',
        'Snapshot("noncanonical", "beam")',
        "FreeTextRoundTripsAndInvalidSavePreservesExistingFile",
        '"  intentional free text  "',
        "before.SequenceEqual(File.ReadAllBytes(path))",
        "MalformedMapsAndSourceHandlesFailClosed",
        'new XAttribute("name", " Note ")',
        'paddedHandle.Elements[0].SourceHandles.Add(" AA ")',
    ):
        if token not in text:
            errors.append("RevisionSnapshotStoreIntegritySmoke missing regression token: " + token)

if REGISTRATION.is_file() and "RevisionSnapshotStoreIntegritySmoke.Run();" not in REGISTRATION.read_text(encoding="utf-8"):
    errors.append("RevisionSnapshotStoreIntegritySmoke is not registered.")

print("QS3D revision-store persistence integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: revision snapshots require exact invariant UTC round-trip timestamps, deterministic category/canonical structure, preserve free-text maps, reject lossy normalization, and leave an existing file intact on failed save.")
