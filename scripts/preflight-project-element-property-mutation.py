#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ELEMENT = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectElement.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = ELEMENT.read_text(encoding="utf-8")

if "Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);" in source:
    fail("ProjectElement.Properties must not expose the raw mutable backing dictionary")

required = (
    "private readonly Dictionary<string, string> _properties",
    "Properties = new ProjectElementPropertyDictionary(this, _properties);",
    "private sealed class ProjectElementPropertyDictionary : IDictionary<string, string>",
    "_owner.SetProperty(key, value)",
    "_owner.RemoveProperty(key)",
    "_owner.ClearProperties()",
)
for token in required:
    if token not in source:
        fail(f"ProjectElement property mutations must route through semantic tracking: {token}")

set_start = source.index("public void SetProperty")
set_end = source.index("internal bool RemoveProperty", set_start)
set_property = source[set_start:set_end]
if "_properties.TryGetValue" not in set_property or "_properties[key] = normalized;" not in set_property:
    fail("SetProperty must write the backing store directly so the facade cannot recurse")
if "Properties[key] = normalized;" in set_property:
    fail("SetProperty must not recurse through the public property facade")

remove_start = source.index("internal bool RemoveProperty")
remove_end = source.index("public void SetQuantity", remove_start)
remove_property = source[remove_start:remove_end]
if "_properties.Remove(key)" not in remove_property:
    fail("RemoveProperty must mutate the backing store before applying semantic dirty tracking")
if "Properties.Remove(key)" in remove_property:
    fail("RemoveProperty must not recurse through the public property facade")

clear_start = source.index("private void ClearProperties")
clear_end = source.index("private sealed class ProjectElementPropertyDictionary", clear_start)
clear_properties = source[clear_start:clear_end]
if "if (_properties.Count == 0) return;" not in clear_properties:
    fail("clearing an already-empty property map must remain a true no-op")
if "ElementGeometryPolicy.AffectsGeneratedGeometry" not in clear_properties or "ElementGeometryPolicy.AffectsGeneratedOutput" not in clear_properties:
    fail("ClearProperties must preserve key-sensitive generated geometry/output invalidation semantics")
if "_properties.Clear();" not in clear_properties or "MarkDirtyCore(" not in clear_properties:
    fail("ClearProperties must clear once and apply one coherent semantic dirty transition")

print("PASS: ProjectElement public property-map mutations preserve validation, dirty tracking, generated-output invalidation, and no-op semantics")
