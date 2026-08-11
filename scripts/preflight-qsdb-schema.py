#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MIGRATOR = ROOT / "src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbProjectSchemaRegressionSmoke.cs"
SAVE_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs"
TIMESTAMP_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs"
errors = []

for path in (MIGRATOR, VALIDATOR, SMOKE, SAVE_SMOKE, TIMESTAMP_SMOKE):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    migrator = MIGRATOR.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    save_smoke = SAVE_SMOKE.read_text(encoding="utf-8")
    timestamp_smoke = TIMESTAMP_SMOKE.read_text(encoding="utf-8")

    migration = migrator.find("while (schema < ProjectState.CurrentSchemaVersion)")
    backfill = migrator.find('if (root.Attribute("changeVersion") == null) root.SetAttributeValue("changeVersion", "0");', migration)
    persistence = migrator.find("ValidateCurrentPersistenceState(root);")
    shape = migrator.find("QsdbProjectXmlSchemaValidator.ValidateCurrent(root);")
    returned = migrator.find("return document;", shape)
    if min(migration, backfill, persistence, shape, returned) < 0 or not migration < backfill < persistence < shape < returned:
        errors.append("ProjectSchemaMigrator must migrate, backfill same-schema legacy changeVersion, validate persistence, then validate strict XML shape")

    if 'RequirePersistenceValue(root, "changeVersion", "Project root")' not in migrator:
        errors.append("QSDB current persistence validation must still reject blank changeVersion after missing-value compatibility backfill")

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
        'string.Equals(element.Name.LocalName, expectedName, StringComparison.OrdinalIgnoreCase)',
        'parent.Elements(XName.Get(childName)).Skip(1).Any()',
    ]
    for token in required_validator_tokens:
        if token not in validator:
            errors.append("QSDB schema validator missing contract token: " + token)

    required_smoke_tokens = [
        "LegacyV1StillMigrates();",
        "RootNameCasingRemainsCompatible();",
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

    compatibility_tokens = [
        (save_smoke, "LegacyFileDefaultsChangeVersion();", "save smoke must cover schema-3 files written before changeVersion existed"),
        (save_smoke, 'Attribute("changeVersion")?.Remove();', "save smoke must remove changeVersion from a real current QSDB before reload"),
        (timestamp_smoke, "RejectsBlankCurrentChangeVersion();", "timestamp smoke must distinguish blank corruption from a missing legacy field"),
    ]
    for source, token, message in compatibility_tokens:
        if token not in source:
            errors.append(message)

print("QS3D QSDB XML schema preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: migrated QSDB XML fails closed on forward-unknown shape while same-schema legacy changeVersion compatibility remains covered.")
