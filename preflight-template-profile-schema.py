#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Templates/TemplateProfileStore.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Templates/TemplateProfileXmlSchemaValidator.cs"
errors = []

for path in (STORE, VALIDATOR):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    store = STORE.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")

    root = store.find("var root = document.Root")
    validate = store.find("TemplateProfileXmlSchemaValidator.Validate(root);", root)
    schema = store.find("var schema = Required(root, \"schema\");", root)
    if min(root, validate, schema) < 0 or not root < validate < schema:
        errors.append("TemplateProfileStore.Load must validate the XML schema before reading schema/profile fields")

    required = [
        'ValidateElement(root, "qs3dTemplate", new[] { "schema", "id", "name" }, new[] { "families", "rules", "layerMappings", "bqColumns" })',
        'RequireAtMostOne(root, "families")',
        'RequireAtMostOne(root, "rules")',
        'RequireAtMostOne(root, "layerMappings")',
        'RequireAtMostOne(root, "bqColumns")',
        'ValidateElement(family, "family", new[] { "id", "name", "category" }, new[] { "properties" })',
        'RequireAtMostOne(family, "properties")',
        'ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>())',
        'ValidateElement(rule, "rule", new[] { "id", "category", "output", "expression", "version" }, Array.Empty<string>())',
        'ValidateElement(map, "map", new[] { "pattern", "category" }, Array.Empty<string>())',
        'ValidateElement(column, "column", new[] { "name" }, Array.Empty<string>())',
        'element.Name != expected',
        'attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name)',
        'child.Name.Namespace != XNamespace.None || !children.Contains(child.Name)',
        '!string.IsNullOrWhiteSpace(text.Value)',
        'parent.Elements(name).Skip(1).Any()',
    ]
    for token in required:
        if token not in validator:
            errors.append("template schema validator missing contract token: " + token)

    if "root.Name.LocalName" in validator:
        errors.append("template schema validator must not accept roots by LocalName only")

print("QS3D template profile XML schema preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: template XML load fails closed on foreign namespaces, unknown nodes/attributes/content, and duplicate singleton containers.")
