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
        "DateTimeOffset.TryParse",
        "HasExplicitUtcOffset(value)",
        "return result.UtcDateTime;",
        "Enum.IsDefined(typeof(ElementCategory), category)",
        "ValidateCanonicalCategory(element.Category)",
        "CanonicalRequired(property, \"name\", \"revision property name\")",
        "Duplicate revision property:",
        "property.Attribute(\"value\")?.Value ?? string.Empty",
        "ValidateCanonicalStringList(element.SourceHandles",
        "Duplicate revision source handle:",
        "x.SourceHandles.OrderBy",
    ):
        if token not in text:
            errors.append("RevisionSnapshotStore missing persistence-integrity token: " + token)

    for forbidden in (
        "snapshot.CreatedUtc.ToUniversalTime()",
        "DateTimeStyles.RoundtripKind",
        "return result.ToUniversalTime()",
        "x.SourceHandles.Where(h => !string.IsNullOrWhiteSpace(h))",
        ".Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(h => h",
        "new XAttribute(\"value\", h.Trim())",
    ):
        if forbidden in text:
            errors.append("RevisionSnapshotStore reintroduced machine-dependent or lossy persistence: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExplicitOffsetNormalizesToUtcAndMissingOffsetFailsClosed",
        '"2026-08-10T12:00:00+07:00"',
        '"2026-08-10T12:00:00"',
        "DateTimeKind.Unspecified",
        "DateTimeKind.Local",
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

print("PASS: revision snapshots require deterministic UTC/category/canonical structure, preserve free-text maps, reject lossy handle normalization, and leave an existing file intact on failed save.")
