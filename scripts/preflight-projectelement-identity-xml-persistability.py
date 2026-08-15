#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilityRegistration.cs"
errors = []

for path in (SOURCE, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing ProjectElement identity XML persistability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'return RequireXmlText(normalized, nameof(value), "Element relation id");',
        'return RequireXmlText(rawValue.Trim(), nameof(value), "Element drawing fingerprint");',
        'return RequireXmlText(normalized, nameof(id), "Element id");',
        'key = RequireXmlText(key, nameof(name), "Property name");',
        'key = RequireXmlText(key, nameof(name), "Quantity name");',
    )
    for token in required:
        if token not in text:
            errors.append("ProjectElement lost XML persistability contract: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "RejectsXmlInvalidIdentityAndRelationsBeforeMutation",
        "SupplementaryUnicodeRoundTripsThroughQsdb",
        "new string(new[] { '\\uD800' })",
        "new string(new[] { '\\uDC00' })",
        "beforeDirty",
        "beforeUpdatedUtc",
        "store.SaveNew(project, path);",
        "store.Load(path);",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectElement identity XML smoke missing regression contract: " + token)

if REGISTRATION.is_file():
    text = REGISTRATION.read_text(encoding="utf-8")
    for token in ("[ModuleInitializer]", "ProjectElementIdentityXmlPersistabilitySmoke.Run()"):
        if token not in text:
            errors.append("ProjectElement identity XML smoke registration missing token: " + token)

if errors:
    print("QS3D ProjectElement identity XML persistability preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: ProjectElement Id/relation/fingerprint text is XML-preflighted before acceptance while existing key guards and mutation semantics remain covered.")
