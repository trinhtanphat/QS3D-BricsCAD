#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticScheduleCatalog.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + ": missing " + token)


def main():
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "var scheduleNodes = MaterializeScheduleNodesBounded(root);", "load must bound schedule nodes before detailed validation")
    require(text, "ValidateSchema(root, scheduleNodes);", "load must validate the bounded schedule-node snapshot")
    require(text, "var definitions = scheduleNodes.Select(ReadDefinition).ToList();", "load must materialize definitions from the bounded node snapshot")
    materialize = text.index("var scheduleNodes = MaterializeScheduleNodesBounded(root);")
    validate = text.index("ValidateSchema(root, scheduleNodes);")
    definitions = text.index("var definitions = scheduleNodes.Select(ReadDefinition).ToList();")
    if not materialize < validate < definitions:
        raise AssertionError("load must enforce capacity before detailed schema and definition parsing")
    require(text, "root.Name.NamespaceName.Length != 0", "root namespaces must fail closed")
    require(text, "private static void ValidateSchema(XElement root, IReadOnlyList<XElement> schedules)", "schema must validate the bounded schedule-node snapshot")
    require(text, "foreach (var schedule in schedules)", "schema must traverse only the bounded schedule-node snapshot")
    require(text, 'ValidateElement(root, "semanticSchedules", new[] { "version" }, new[] { "schedule" });', "root schema allowlist")
    require(text, 'ValidateElement(schedule, "schedule", new[] { "id", "name", "title", "floorId", "zoneId" }, new[] { "categories", "include", "exclude", "columns" });', "schedule schema allowlist")

    for child in ("categories", "include", "exclude", "columns"):
        require(text, 'var ' + child + ' = RequireExactlyOneChild(schedule, "' + child + '");', "exactly-one singleton container guard for " + child)

    require(text, "if (children.Length != 1)", "exactly-one container cardinality check")

    require(text, 'ValidateElement(categories, "categories", Array.Empty<string>(), new[] { "category" });', "categories schema allowlist")
    require(text, 'ValidateElement(category, "category", new[] { "value" }, Array.Empty<string>());', "category schema allowlist")
    require(text, 'ValidateElement(include, "include", Array.Empty<string>(), new[] { "id" });', "include schema allowlist")
    require(text, 'ValidateElement(exclude, "exclude", Array.Empty<string>(), new[] { "id" });', "exclude schema allowlist")
    require(text, 'ValidateElement(id, "id", new[] { "value" }, Array.Empty<string>());', "id schema allowlist")
    require(text, 'ValidateElement(columns, "columns", Array.Empty<string>(), new[] { "column" });', "columns schema allowlist")
    require(text, 'ValidateElement(column, "column", new[] { "header", "template" }, Array.Empty<string>());', "column schema allowlist")

    require(text, "attribute.Name.NamespaceName.Length != 0", "namespaced attributes must fail closed")
    require(text, "!allowedAttributes.Contains(attribute.Name.LocalName)", "unknown attributes must fail closed")
    require(text, "child.Name.NamespaceName.Length != 0", "namespaced children must fail closed")
    require(text, "!allowedChildren.Contains(child.Name.LocalName)", "unknown children must fail closed")
    require(text, "string.IsNullOrWhiteSpace(text.Value)", "only insignificant text is tolerated")
    require(text, "contains unsupported XML content", "comments, processing instructions, and semantic text must fail closed")

    print("PASS: semantic schedule catalog XML is strict and fail-closed against lossy forward/foreign schema.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
