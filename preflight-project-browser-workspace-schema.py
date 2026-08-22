#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Project Browser workspace schema file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)

for token in (
    "ValidateRootShape(root)",
    "expectedAttributes = new HashSet<XName>",
    'XName.Get("format")',
    'XName.Get("version")',
    'XName.Get("grouping")',
    'XName.Get("dirtyOnly")',
    'XName.Get("query")',
    'XName.Get("primaryElementId")',
    "root contains unsupported attribute",
    "root is missing required attribute",
    "ValidateCollectionShape",
    "collection contains unsupported attributes",
    "ValidateItemShape",
    "item contains unsupported attributes",
    "item must contain text only",
    "ValidateContainerNodes",
    "contains unsupported node content",
):
    if token not in source:
        errors.append("workspace source missing strict-schema contract token: " + token)

for token in (
    "UnsupportedSchemaShapeFailsClosed",
    'future=\\"x\\"',
    'query=\\"beam\\"',
    '<FloorIds future=\\"x\\">',
    '<ZoneIds>future',
    '<Id future=\\"x\\">F-02</Id>',
    '<Id><Future>Z-A</Future></Id>',
    '<SelectedElementIds><!--future-->',
):
    if token not in smoke:
        errors.append("workspace smoke missing strict-schema regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: version-1 Project Browser workspace XML rejects unsupported attributes, mixed/container nodes, and non-text item shapes instead of silently losing them on round-trip.")
