#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingTokenBoundSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

required_source = [
    "internal const int MaximumTokenLength = 1024;",
    "if (value.Length > MaximumTokenLength)",
    '"Mapping identifier must contain at most " + MaximumTokenLength + " UTF-16 code units."',
    "MappingId = MeasurementWorkItemMappingContract.RequireToken(mappingId, nameof(mappingId));",
    "MeasurementItemId = MeasurementWorkItemMappingContract.RequireToken(measurementItemId, nameof(measurementItemId));",
    "ClassificationId = MeasurementWorkItemMappingContract.RequireToken(classificationId, nameof(classificationId));",
    "WorkItemId = MeasurementWorkItemMappingContract.RequireToken(workItemId, nameof(workItemId));",
    "var canonicalMeasurementItemId = MeasurementWorkItemMappingContract.RequireToken(measurementItemId, nameof(measurementItemId));",
]
for token in required_source:
    if token not in source:
        errors.append("missing mapping token-bound source contract: " + token)

require_start = source.find("internal static string RequireToken")
length_check = source.find("if (value.Length > MaximumTokenLength)", require_start)
trim_check = source.find("var trimmed = value.Trim();", require_start)
xml_check = source.find("XmlConvert.VerifyXmlChars(value);", require_start)
if min(require_start, length_check, trim_check, xml_check) < 0:
    errors.append("could not resolve RequireToken admission ordering")
elif not (require_start < length_check < trim_check < xml_check):
    errors.append("mapping token resource bound must execute before trimming/control/XML work")

required_smoke = [
    "ExactBoundaryRemainsAcceptedAcrossConstructorSurfaces();",
    "EveryConstructorIdentityRejectsBoundaryPlusOne();",
    "ResolveRejectsBoundaryPlusOneBeforeLookup();",
    "LengthBoundPrecedesXmlValidation();",
    "ExistingCanonicalityRulesRemainIntact();",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic mapping token-bound smoke coverage: " + token)

if "MeasurementWorkItemMappingTokenBoundSmoke.Run();" not in registration:
    errors.append("mapping token-bound smoke is not registered")

print("QS3D MeasurementWorkItemMapping token resource-bound preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: mapping identities are capped at 1024 UTF-16 code units across constructor and Resolve admission before downstream canonicality/XML/catalog work.")
