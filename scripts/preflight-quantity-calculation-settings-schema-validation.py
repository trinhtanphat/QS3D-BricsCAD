#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/QuantityCalculationSettings.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsSchemaValidationSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsSchemaValidationSmokeRegistration.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing quantity settings schema validation file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "if (SchemaVersion < 0)",
    'throw new InvalidOperationException("Quantity settings schema cannot be negative.");',
    "var normalizedSchemaVersion = SchemaVersion == 0 ? CurrentSchemaVersion : SchemaVersion;",
    "if (normalizedSchemaVersion > CurrentSchemaVersion)",
    "SchemaVersion = normalizedSchemaVersion;",
):
    if token not in source:
        errors.append("QuantityCalculationSettings schema guard missing token: " + token)

if "if (SchemaVersion <= 0) SchemaVersion = CurrentSchemaVersion;" in source:
    errors.append("negative schema versions must not be silently promoted through the legacy zero-schema compatibility path")

negative_pos = source.find("if (SchemaVersion < 0)")
normalized_pos = source.find("var normalizedSchemaVersion = SchemaVersion == 0 ? CurrentSchemaVersion : SchemaVersion;")
future_pos = source.find("if (normalizedSchemaVersion > CurrentSchemaVersion)")
commit_pos = source.find("SchemaVersion = normalizedSchemaVersion;", future_pos)
if min(negative_pos, normalized_pos, future_pos, commit_pos) < 0 or not (negative_pos < normalized_pos < future_pos < commit_pos):
    errors.append("schema validation must reject negatives, normalize zero locally, reject future schema, then commit the normalized schema only after validation")

for token in (
    "ZeroSchemaKeepsLegacyCompatibility",
    "NegativeSchemaFailsClosedWithoutMutation",
    "CurrentSchemaRemainsValid",
    "settings.SchemaVersion = 0;",
    "settings.SchemaVersion = -1;",
    'Equal("Quantity settings schema cannot be negative.", error.Message, "negative schema validation message")',
    'Equal(-1, settings.SchemaVersion, "negative schema remains unchanged after rejection")',
    "new QuantityCalculationRuleSet(settings)",
    'Equal(-1, settings.SchemaVersion, "runtime rejection does not mutate caller schema")',
):
    if token not in smoke:
        errors.append("quantity settings schema smoke missing token: " + token)

for token in (
    "using System.Runtime.CompilerServices;",
    "internal static class QuantityCalculationSettingsSchemaValidationSmokeRegistration",
    "[ModuleInitializer]",
    "QuantityCalculationSettingsSchemaValidationSmoke.Run();",
):
    if token not in registration:
        errors.append("quantity settings schema smoke registration missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Quantity Settings rejects negative/future schema metadata before mutation, normalizes legacy schema-zero locally, and commits the supported schema only after validation.")
