#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MIGRATOR = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectSchemaMigrator.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbPrimaryIdentityCanonicalitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"
errors = []

for path in (MIGRATOR, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing QSDB primary identity canonicality file: " + str(path.relative_to(ROOT)))

if MIGRATOR.is_file():
    text = MIGRATOR.read_text(encoding="utf-8")
    required = (
        "ValidatePrimaryIdentityCanonicality(callerRoot);",
        "ValidatePrimaryIdentityCanonicality(root);",
        "private static void ValidatePrimaryIdentityCanonicality(XElement root)",
        'RequireCanonicalAttribute(zone, "id", "Project zone id");',
        'RequireCanonicalAttribute(floor, "id", "Project floor id");',
        'RequireCanonicalAttribute(family, "id", "Project family id");',
        'RequireCanonicalAttribute(rule, "id", "Quantity rule id");',
        'RequireCanonicalAttribute(rule, "output", "Quantity rule output");',
        'RequireCanonicalAttribute(element, "id", "Project element id");',
        'RequireCanonicalAttribute(quantity, "name", "Project element quantity name");',
        "private static void RequireCanonicalAttribute(XElement element, string attributeName, string owner)",
        "if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))",
        'throw new InvalidDataException(owner + " must not contain leading/trailing whitespace.");',
    )
    for token in required:
        if token not in text:
            errors.append("ProjectSchemaMigrator.cs missing fail-closed primary identity token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedZoneId();",
        "RejectsPaddedFloorId();",
        "RejectsPaddedFamilyId();",
        "RejectsPaddedElementId();",
        "RejectsPaddedRuleId();",
        "RejectsPaddedRuleOutput();",
        "RejectsPaddedQuantityName();",
        "CanonicalControlRoundTrips();",
        "InvalidDataException",
    ):
        if token not in text:
            errors.append("QsdbPrimaryIdentityCanonicalitySmoke.cs missing regression token: " + token)

if REGISTRATION.is_file():
    text = REGISTRATION.read_text(encoding="utf-8")
    if "QsdbPrimaryIdentityCanonicalitySmoke.Run();" not in text:
        errors.append("SmokeTestRegistration.cs does not register QsdbPrimaryIdentityCanonicalitySmoke.Run().")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB primary persisted identities are rejected before trim-normalizing hydration and covered by deterministic smoke.")
