#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbCanonicalPersistenceSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (STORE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing QSDB canonical persistence contract file: " + str(path.relative_to(ROOT)))

if STORE.is_file():
    text = STORE.read_text(encoding="utf-8")
    for token in (
        "ValidateCanonicalStringList(element.SourceHandles",
        "ValidateCanonicalStringList(element.DependsOn",
        "ValidateCanonicalKey(quantity.Key",
        "foreach (var key in values.Keys) ValidateCanonicalKey",
        "must not contain leading/trailing whitespace",
        "project.AuditEvents.Any(x => x == null)",
        "ValidateUtcTimestamp(project.UpdatedUtc",
        "ValidateUtcTimestamp(element.UpdatedUtc",
        "ValidateUtcTimestamp(audit.Utc",
        "value.Kind != DateTimeKind.Utc",
        "Enum.IsDefined(typeof(ElementCategory), x.Category)",
        "Invalid \" + label + \" category:",
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing canonical persistence token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PaddedMapKeyFailsBeforePersistence",
        "PaddedQuantityNameFailsBeforePersistence",
        "NonCanonicalHandleAndDependencyFailBeforePersistence",
        "NullAuditEventFailsClosed",
        "NonUtcTimestampFailsBeforePersistence",
        "DateTimeKind.Unspecified",
        "DateTimeKind.Local",
        "UndefinedCategoryFailsClosed",
        '(ElementCategory)999',
        'category=\\"999\\"',
        "if (File.Exists(path)) throw new Exception",
    ):
        if token not in text:
            errors.append("QsdbCanonicalPersistenceSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "QsdbCanonicalPersistenceSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("QSDB canonical persistence smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB Save rejects non-canonical identity keys/handles/dependencies and non-UTC persisted timestamps before serialization can silently normalize them.")
