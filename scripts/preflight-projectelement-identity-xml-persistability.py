#!/usr/bin/env python3
from pathlib import Path
import re
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
        'return RequireXmlText(name.Trim(), nameof(name), "Property name");',
        'key = RequireXmlText(key, nameof(name), "Quantity name");',
    )
    for token in required:
        if token not in text:
            errors.append("ProjectElement lost XML persistability contract: " + token)

    property_name_guard = re.search(
        r"private static string RequirePropertyName\(string name\)(?P<body>.*?)\n        private static string NormalizeOptionalRelationId",
        text,
        re.DOTALL,
    )
    if not property_name_guard:
        errors.append("missing ProjectElement.RequirePropertyName body")
    else:
        body = property_name_guard.group("body")
        for token in (
            'if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Property name is required.", nameof(name));',
            'if (name.Any(char.IsControl)) throw new ArgumentException("Property name cannot contain control characters.", nameof(name));',
            'return RequireXmlText(name.Trim(), nameof(name), "Property name");',
        ):
            if token not in body:
                errors.append("RequirePropertyName lost property-name validation contract: " + token)

    for method_name in ("SetProperty", "AddProperty", "RemoveProperty"):
        method = re.search(
            rf"(?:public|internal) (?:void|bool) {method_name}\(.*?\)(?P<body>.*?)\n        (?:public|internal|private) ",
            text,
            re.DOTALL,
        )
        if not method or "var key = RequirePropertyName(name);" not in method.group("body"):
            errors.append(method_name + " must validate property names through RequirePropertyName before mutation")

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

print("PASS: ProjectElement Id/relation/fingerprint text remains XML-preflighted and every semantic property mutation validates names through the shared XML-safe RequirePropertyName boundary.")