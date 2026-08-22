#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs"
errors = []

for path in (STORE, VALIDATOR):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if not errors:
    store = STORE.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")

    root = store.find("var root = LoadDocument(path).Root")
    validate = store.find("RevisionSnapshotXmlSchemaValidator.Validate(root);", root)
    snapshot = store.find("var snapshot = new RevisionSnapshot", root)
    if min(root, validate, snapshot) < 0 or not root < validate < snapshot:
        errors.append("RevisionSnapshotStore.Load must validate XML shape before reading snapshot fields")

    required = [
        'ValidateElement(root, "qs3dRevision", new[] { "id", "createdUtc" }, new[] { "elements" })',
        'RequireAtMostOne(root, "elements")',
        'ValidateElement(element, "element", new[] { "id", "category", "familyId", "floorId", "zoneId" }, new[] { "properties", "quantities", "sourceHandles", "dependencies" })',
        'RequireAtMostOne(element, "properties")',
        'RequireAtMostOne(element, "quantities")',
        'RequireAtMostOne(element, "sourceHandles")',
        'RequireAtMostOne(element, "dependencies")',
        'ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>())',
        'ValidateElement(quantity, "q", new[] { "name", "value" }, Array.Empty<string>())',
        'ValidateElement(handle, "h", new[] { "value" }, Array.Empty<string>())',
        'ValidateElement(dependency, "d", new[] { "value" }, Array.Empty<string>())',
        'element.Name != expected',
        'attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name)',
        'child.Name.Namespace != XNamespace.None || !children.Contains(child.Name)',
        '!string.IsNullOrWhiteSpace(text.Value)',
        'parent.Elements(name).Skip(1).Any()',
    ]
    for token in required:
        if token not in validator:
            errors.append("revision schema validator missing contract token: " + token)

    if "root.Name.LocalName" in validator:
        errors.append("revision schema validator must not accept roots by LocalName only")

print("QS3D revision snapshot XML schema preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: revision XML load fails closed on foreign namespaces, unknown nodes/attributes/content, dependency shape, and duplicate singleton containers.")
