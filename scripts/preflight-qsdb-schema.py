#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MIGRATOR = ROOT / "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbProjectSchemaRegressionSmoke.cs"
SAVE_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs"
TIMESTAMP_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs"
ATOMICITY_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectSchemaMigrationAtomicitySmoke.cs"
errors = []

for path in (MIGRATOR, VALIDATOR, SMOKE, SAVE_SMOKE, TIMESTAMP_SMOKE, ATOMICITY_SMOKE):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    migrator = MIGRATOR.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    save_smoke = SAVE_SMOKE.read_text(encoding="utf-8")
    timestamp_smoke = TIMESTAMP_SMOKE.read_text(encoding="utf-8")
    atomicity_smoke = ATOMICITY_SMOKE.read_text(encoding="utf-8")

    working_copy = migrator.find("var workingDocument = new XDocument(document);")
    migration = migrator.find("while (schema < ProjectState.CurrentSchemaVersion)", working_copy)
    persistence = migrator.find("ValidateCurrentPersistenceState(workingRoot);", migration)
    shape = migrator.find("QsdbProjectXmlSchemaValidator.ValidateCurrent(workingRoot);", persistence)
    publish = migrator.find("root.ReplaceWith(new XElement(workingRoot));", shape)
    returned = migrator.find("return document;", publish)
    if min(working_copy, migration, persistence, shape, publish, returned) < 0 or not working_copy < migration < persistence < shape < publish < returned:
        errors.append("ProjectSchemaMigrator must migrate legacy schemas on a detached document, validate required current persistence state and strict XML shape, then publish the validated root in place")

    migrate_v2 = migrator.find("private static void MigrateV2ToV3")
    legacy_backfill = migrator.find('if (root.Attribute("changeVersion") == null) root.SetAttributeValue("changeVersion", "0");', migrate_v2)
    persistence_method = migrator.find("private static void ValidateCurrentPersistenceState", legacy_backfill)
    if min(migrate_v2, legacy_backfill, persistence_method) < 0 or not migrate_v2 < legacy_backfill < persistence_method:
        errors.append("ProjectSchemaMigrator must synthesize changeVersion only while migrating legacy schema 2 to schema 3, before strict current-state validation")

    if 'RequirePersistenceValue(root, "changeVersion", "Project root")' not in migrator:
        errors.append("QSDB current persistence validation must reject missing or blank schema-3 changeVersion")

    required_validator_tokens = [
        '"schema", "projectId", "name", "updatedUtc", "changeVersion"',
        '"drawingPath", "drawingFingerprint", "activeZoneId", "activeFloorId"',
        '"metadata", "zones", "floors", "families", "rules", "elements", "audit"',
        'new[] { "id", "name", "category" }',
        'new[] { "id", "category", "output", "expression", "version" }',
        '"dirty", "updatedUtc"',
        'new[] { "handles", "dependencies", "properties", "quantities" }',
        'RequireAtMostOne(element, "handles")',
        'RequireAtMostOne(element, "dependencies")',
        'RequireAtMostOne(element, "properties")',
        'RequireAtMostOne(element, "quantities")',
        'attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name)',
        'child.Name.Namespace != XNamespace.None || !children.Contains(child.Name)',
        '!allowText && !string.IsNullOrWhiteSpace(text.Value)',
        'element.Name.Namespace != XNamespace.None',
        'RootSections,\n                false,\n                false);',
        'parent.Elements(XName.Get(childName)).Skip(1).Any()',
    ]
    for token in required_validator_tokens:
        if token not in validator:
            errors.append("QSDB schema validator missing contract token: " + token)

    required_smoke_tokens = [
        "LegacyV1StillMigrates();",
        "RejectsNonCanonicalRootName();",
        "RejectsForeignNamespace();",
        "RejectsUnknownRootAttribute();",
        "RejectsUnknownChild();",
        "RejectsDuplicateNestedContainer();",
        "RejectsNamespacedAttribute();",
        "RejectsMixedTextContent();",
        "catch (InvalidDataException)",
    ]
    for token in required_smoke_tokens:
        if token not in smoke:
            errors.append("QSDB schema regression smoke missing contract token: " + token)

    atomicity_tokens = [
        "FailedLegacyMigrationDoesNotMutateInput();",
        "SuccessfulLegacyMigrationStillPublishesInPlace();",
        "ReferenceEquals(document, returned)",
        'document.Root?.Attribute("schema")?.Value == "1"',
        'document.Root?.Attribute("updatedUtc") == null',
    ]
    for token in atomicity_tokens:
        if token not in atomicity_smoke:
            errors.append("QSDB schema migration atomicity smoke missing contract token: " + token)

    strict_persistence_tokens = [
        (save_smoke, "MissingCurrentChangeVersionIsRejected();", "save smoke must reject schema-3 files whose required changeVersion was removed"),
        (save_smoke, 'Attribute("changeVersion")?.Remove();', "save smoke must remove changeVersion from a real current QSDB before asserting rejection"),
        (save_smoke, "Throws<InvalidDataException>", "save smoke must fail closed on missing current changeVersion"),
        (timestamp_smoke, "RejectsMissingCurrentChangeVersion();", "timestamp smoke must reject missing schema-3 changeVersion"),
        (timestamp_smoke, "RejectsBlankCurrentChangeVersion();", "timestamp smoke must reject blank schema-3 changeVersion"),
        (timestamp_smoke, "LegacyV1MissingTimestampsStillMigrates();", "timestamp smoke must preserve explicit legacy-schema migration coverage"),
    ]
    for source, token, message in strict_persistence_tokens:
        if token not in source:
            errors.append(message)

print("QS3D QSDB XML schema preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: current schema-3 QSDB requires the canonical qs3d root and explicit persistence state; legacy migration is failure-atomic on a detached copy, publishes only after strict validation, and synthesizes required changeVersion only on the legacy migration path.")
