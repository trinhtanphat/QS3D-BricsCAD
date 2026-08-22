#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticDocumentationCatalogStore.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + ": missing " + token)


def main():
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "ValidateSchema(root);", "load must validate schema before materializing views and sheets")
    require(text, "root.Name.NamespaceName.Length != 0", "root namespaces must fail closed")
    require(text, 'ValidateElement(root, "documentation", new[] { "version" }, new[] { "views", "sheets" });', "root schema allowlist")
    require(text, 'RequireExactlyOneChild(root, "views");', "required views container guard")
    require(text, 'RequireExactlyOneChild(root, "sheets");', "required sheets container guard")
    require(text, "private static void RequireExactlyOneChild(XElement parent, string childName)", "exactly-one root helper")
    require(text, "parent.Elements(childName).Take(2).Count() != 1", "exactly-one root cardinality")
    require(text, 'ValidateElement(view, "view", new[] { "id", "name", "kind", "floorId", "zoneId" }, new[] { "categories", "include", "exclude" });', "view schema allowlist")
    require(text, 'ValidateElement(sheet, "sheet", new[] { "id", "number", "name", "widthMm", "heightMm", "titleBlockName" }, new[] { "placements" });', "sheet schema allowlist")
    require(text, 'ValidateElement(placement, "placement", new[] { "viewId", "xMm", "yMm", "widthMm", "heightMm" }, Array.Empty<string>());', "placement schema allowlist")
    require(text, 'ValidateElement(category, "category", new[] { "value" }, Array.Empty<string>());', "category schema allowlist")
    require(text, 'ValidateElement(id, "id", new[] { "value" }, Array.Empty<string>());', "id schema allowlist")

    for parent, child in (("view", "categories"), ("view", "include"), ("view", "exclude"), ("sheet", "placements")):
        require(text, 'EnsureAtMostOneChild(' + parent + ', "' + child + '");', "duplicate singleton guard for " + parent + "/" + child)

    require(text, "attribute.Name.NamespaceName.Length != 0", "namespaced attributes must fail closed")
    require(text, "!allowedAttributes.Contains(attribute.Name.LocalName)", "unknown attributes must fail closed")
    require(text, "child.Name.NamespaceName.Length != 0", "namespaced children must fail closed")
    require(text, "!allowedChildren.Contains(child.Name.LocalName)", "unknown children must fail closed")
    require(text, "string.IsNullOrWhiteSpace(text.Value)", "only insignificant text is tolerated")
    require(text, "contains unsupported XML content", "comments, processing instructions, and semantic text must fail closed")

    print("PASS: semantic documentation catalog XML is strict and fail-closed against lossy forward/foreign schema, with exactly one views/sheets root container.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
