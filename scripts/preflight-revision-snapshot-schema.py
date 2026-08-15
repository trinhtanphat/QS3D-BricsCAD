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
        'var document = root.Document;',
        'foreach (var node in document.Nodes())',
        'ReferenceEquals(node, root)',
        'Unsupported QS3D revision document-level XML content.',
        'ValidateElement(root, "qs3dRevision", new[] { "id", "createdUtc", "schemaVersion", "projectId" }, new[] { "elements" })',
        'RequireExactlyOne(root, "elements")',
        'parent.Elements(name).Take(2).Count() != 1',
        'ValidateElement(element, "element", new[] { "id", "category", "familyId", "floorId", "zoneId" }, new[] { "properties", "quantities", "sourceHandles", "dependencies" })',
        'RequireExactlyOne(element, "properties")',
        'RequireExactlyOne(element, "quantities")',
        'RequireExactlyOne(element, "sourceHandles")',
        'RequireExactlyOne(element, "dependencies")',
        'ValidateElement(property, "p", new[] { "name", "value" }, Array.Empty<string>())',
        'ValidateElement(quantity, "q", new[] { "name", "value" }, Array.Empty<string>())',
        'ValidateElement(handle, "h", new[] { "value" }, Array.Empty<string>())',
        'ValidateElement(dependency, "d", new[] { "value" }, Array.Empty<string>())',
        'element.Name != expected',
        'attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !attributes.Contains(attribute.Name)',
        'child.Name.Namespace != XNamespace.None || !children.Contains(child.Name)',
        'if (node is XCData)',
        'Unsupported QS3D revision CDATA content in ',
        '!string.IsNullOrWhiteSpace(text.Value)',
    ]
    for token in required:
        if token not in validator:
            errors.append("revision schema validator missing contract token: " + token)

    document_guard = validator.find("var document = root.Document;")
    root_shape = validator.find('ValidateElement(root, "qs3dRevision"')
    if min(document_guard, root_shape) < 0 or not document_guard < root_shape:
        errors.append("revision document-level XML content must be rejected before root schema parsing")

    cdata = validator.find("if (node is XCData)")
    text = validator.find("if (node is XText text)")
    if min(cdata, text) < 0 or not cdata < text:
        errors.append("revision schema validator must reject XCData before the general XText branch")

    forbidden = [
        'RequireAtMostOne(root, "elements")',
        'RequireAtMostOne(element, "properties")',
        'RequireAtMostOne(element, "quantities")',
        'RequireAtMostOne(element, "sourceHandles")',
        'RequireAtMostOne(element, "dependencies")',
    ]
    for token in forbidden:
        if token in validator:
            errors.append("revision canonical container must be required, not optional: " + token)

    if "root.Name.LocalName" in validator:
        errors.append("revision schema validator must not accept roots by LocalName only")

print("QS3D revision snapshot XML schema preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: revision XML load accepts the v2 schemaVersion/projectId root identity contract while rejecting document siblings, CDATA, foreign namespaces and unknown XML content.")
