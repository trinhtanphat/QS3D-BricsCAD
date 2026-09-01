#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsdbPrimaryIdentityCanonicalitySmoke.cs"
errors = []

for path in (STORE, SMOKE):
    if not path.is_file():
        errors.append("missing QSDB primary identity canonicality file: " + str(path.relative_to(ROOT)))

if STORE.is_file():
    text = STORE.read_text(encoding="utf-8")
    required = (
        'new ZoneDefinition(RequiredCanonical(item, "id"), Required(item, "name"))',
        'new FloorDefinition(RequiredCanonical(item, "id"), Required(item, "name"), Double(item.Attribute("elevationM")?.Value))',
        'new ProjectFamily(RequiredCanonical(item, "id"), Required(item, "name"), category)',
        'RequiredCanonical(item, "id"), category, RequiredCanonical(item, "output"), Required(item, "expression"), Required(item, "version")',
        'new ProjectElement(RequiredCanonical(item, "id"), category, Value(item, "familyId"), Value(item, "floorId"), Value(item, "zoneId"))',
        'var quantityName = RequiredCanonical(q, "name");',
        'private static string RequiredCanonical(XElement element, string attribute)',
        'if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))',
        'throw new InvalidDataException("Non-canonical QSDB attribute: " + attribute);',
    )
    for token in required:
        if token not in text:
            errors.append("QsdbProjectStore.cs missing fail-closed primary identity token: " + token)

    forbidden = (
        'new ZoneDefinition(Required(item, "id")',
        'new FloorDefinition(Required(item, "id")',
        'new ProjectFamily(Required(item, "id")',
        'new ProjectElement(Required(item, "id")',
        'var quantityName = Required(q, "name");',
    )
    for token in forbidden:
        if token in text:
            errors.append("QSDB primary identity load must not trim-normalize through Required(...): " + token)

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

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB primary persisted identities are exact-canonical on load and are never silently trim-normalized.")
