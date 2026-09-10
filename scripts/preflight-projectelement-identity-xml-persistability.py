#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
QUANTITY_DICTIONARY = ROOT / "src/QS3D.Core/Domain/ProjectElementQuantityDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilitySmoke.cs"
QUANTITY_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementQuantityMutationSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilityRegistration.cs"
errors = []

for path in (SOURCE, QUANTITY_DICTIONARY, SMOKE, QUANTITY_SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing ProjectElement identity XML persistability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'return RequireXmlText(normalized, nameof(value), "Element relation id");',
        'return RequireXmlText(rawValue.Trim(), nameof(value), "Element drawing fingerprint");',
        'return RequireXmlText(normalized, nameof(id), "Element id");',
        'return RequireXmlText(name.Trim(), nameof(name), "Property name");',
        'return RequireXmlText(name.Trim(), nameof(name), "Quantity name");',
        'var key = RequireQuantityName(name);',
    )
    for token in required:
        if token not in text:
            errors.append("ProjectElement lost XML persistability contract: " + token)

    property_name_guard = re.search(
        r"private static string RequirePropertyName\(string name\)(?P<body>.*?)\n        private static string RequireQuantityName",
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

    quantity_name_guard = re.search(
        r"private static string RequireQuantityName\(string name\)(?P<body>.*?)\n        private static double RequireQuantityValue",
        text,
        re.DOTALL,
    )
    if not quantity_name_guard:
        errors.append("missing ProjectElement.RequireQuantityName body")
    else:
        body = quantity_name_guard.group("body")
        for token in (
            'if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Quantity name is required.", nameof(name));',
            'if (name.Any(char.IsControl)) throw new ArgumentException("Quantity name cannot contain control characters.", nameof(name));',
            'return RequireXmlText(name.Trim(), nameof(name), "Quantity name");',
        ):
            if token not in body:
                errors.append("RequireQuantityName lost quantity-name validation contract: " + token)

    for method_name in ("SetProperty", "AddProperty", "RemoveProperty"):
        method = re.search(
            rf"(?:public|internal) (?:void|bool) {method_name}\(.*?\)(?P<body>.*?)\n        (?:public|internal|private) ",
            text,
            re.DOTALL,
        )
        if not method or "var key = RequirePropertyName(name);" not in method.group("body"):
            errors.append(method_name + " must validate property names through RequirePropertyName before mutation")

    for method_name in ("SetQuantity", "AddQuantity", "RemoveQuantity"):
        method = re.search(
            rf"(?:public|internal) (?:void|bool) {method_name}\(.*?\)(?P<body>.*?)\n        (?:public|internal|private) ",
            text,
            re.DOTALL,
        )
        if not method or "var key = RequireQuantityName(name);" not in method.group("body"):
            errors.append(method_name + " must validate quantity names through RequireQuantityName before mutation")

if QUANTITY_DICTIONARY.is_file():
    text = QUANTITY_DICTIONARY.read_text(encoding="utf-8")
    required = (
        "get => _values[CanonicalReadKey(key)];",
        "ContainsKey(string key) => _values.ContainsKey(CanonicalReadKey(key))",
        "TryGetValue(string key, out double value) => _values.TryGetValue(CanonicalReadKey(key), out value)",
        "new KeyValuePair<string, double>(CanonicalReadKey(item.Key), item.Value)",
        "if (key == null) throw new ArgumentNullException(nameof(key));",
        "return key.Trim();",
    )
    for token in required:
        if token not in text:
            errors.append("ProjectElement quantity facade lost canonical read identity contract: " + token)

    pair_remove = re.search(
        r"public bool Remove\(KeyValuePair<string, double> item\)(?P<body>.*?)\n        public bool TryGetValue",
        text,
        re.DOTALL,
    )
    if not pair_remove:
        errors.append("missing ProjectElement quantity pair-removal body")
    else:
        body = pair_remove.group("body")
        for token in (
            "if (!_values.TryGetValue(candidate, out var existing))",
            "if (!existing.Equals(item.Value)) return false;",
            "return _owner.RemoveQuantity(key);",
        ):
            if token not in body:
                errors.append("ProjectElement quantity pair removal lost persisted-corruption cleanup contract: " + token)
        if "_owner.SetQuantity(" in body:
            errors.append("ProjectElement quantity pair removal must not re-admit persisted values through SetQuantity")

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

if QUANTITY_SMOKE.is_file():
    text = QUANTITY_SMOKE.read_text(encoding="utf-8")
    for token in (
        "ReadApisUseCanonicalIdentity",
        'element.Quantities[" area "]',
        'element.Quantities.ContainsKey(" AREA ")',
        'element.Quantities.TryGetValue(" area ", out var value)',
        'new KeyValuePair<string, double>(" AREA ", 12.5d)',
        "PairRemovalCanDeletePersistedCorruptValue",
        'SeedPersistedQuantity(element, "Area", -1d)',
        'new KeyValuePair<string, double>(" area ", -1d)',
        "Equal(ElementDirtyFlags.None, element.Dirty);",
        "Equal(before, element.UpdatedUtc);",
    ):
        if token not in text:
            errors.append("ProjectElement quantity canonical-read/cleanup smoke missing regression contract: " + token)

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

print("PASS: ProjectElement Id/relation/fingerprint text remains XML-preflighted; semantic quantity mutations and reads share canonical identity, pair removal can clean persisted-corrupt values without re-admission, and reads stay lifecycle-neutral.")
