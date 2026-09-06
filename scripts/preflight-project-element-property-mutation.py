#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOMAIN = ROOT / "src" / "QS3D.Core" / "Domain"
ELEMENT = DOMAIN / "ProjectElement.cs"
FACADE = DOMAIN / "ProjectElementPropertyDictionary.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = ELEMENT.read_text(encoding="utf-8")
if not FACADE.exists():
    fail("ProjectElement public property mutations require a dedicated semantic IDictionary facade")
facade = FACADE.read_text(encoding="utf-8")

if "Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);" in source:
    fail("ProjectElement.Properties must not expose the raw mutable backing dictionary")

required_owner = (
    "private readonly Dictionary<string, string> _properties",
    "Properties = new ProjectElementPropertyDictionary(this, _properties);",
    "internal void AddProperty(string name, string value)",
    "internal bool RemoveProperty(string name)",
    "internal void ClearProperties()",
    "private void MarkPropertyChanged(string key)",
)
for token in required_owner:
    if token not in source:
        fail(f"ProjectElement property mutations must have one semantic owner boundary: {token}")

required_facade = (
    "internal sealed class ProjectElementPropertyDictionary : IDictionary<string, string>",
    "_owner.SetProperty(key, value)",
    "_owner.AddProperty(key, value)",
    "_owner.RemoveProperty(key)",
    "_owner.ClearProperties()",
)
for token in required_facade:
    if token not in facade:
        fail(f"public IDictionary mutation must route through ProjectElement semantics: {token}")

set_start = source.index("public void SetProperty")
set_end = source.index("internal void AddProperty", set_start)
set_property = source[set_start:set_end]
if "_properties.TryGetValue" not in set_property or "_properties[key] = normalized;" not in set_property:
    fail("SetProperty must write the backing store directly so the facade cannot recurse")
if "Properties[key] = normalized;" in set_property:
    fail("SetProperty must not recurse through the public property facade")

add_start = source.index("internal void AddProperty")
add_end = source.index("internal bool RemoveProperty", add_start)
add_property = source[add_start:add_end]
if "_properties.Add(key, normalized);" not in add_property or "MarkPropertyChanged(key);" not in add_property:
    fail("AddProperty must preserve dictionary duplicate semantics and then apply semantic dirty tracking")

remove_start = source.index("internal bool RemoveProperty")
remove_end = source.index("internal void ClearProperties", remove_start)
remove_property = source[remove_start:remove_end]
if "_properties.Remove(key)" not in remove_property or "MarkPropertyChanged(key);" not in remove_property:
    fail("RemoveProperty must mutate the backing store before applying semantic dirty tracking")
if "Properties.Remove(key)" in remove_property:
    fail("RemoveProperty must not recurse through the public property facade")

clear_start = source.index("internal void ClearProperties")
clear_end = source.index("public void SetQuantity", clear_start)
clear_properties = source[clear_start:clear_end]
if "if (_properties.Count == 0) return;" not in clear_properties:
    fail("clearing an already-empty property map must remain a true no-op")
if "ElementGeometryPolicy.AffectsGeneratedGeometry" not in clear_properties or "ElementGeometryPolicy.AffectsGeneratedOutput" not in clear_properties:
    fail("ClearProperties must preserve key-sensitive generated geometry/output invalidation semantics")
if "_properties.Clear();" not in clear_properties or "MarkDirtyCore(" not in clear_properties:
    fail("ClearProperties must clear once and apply one coherent semantic dirty transition")

internal_write_tokens = (
    "_properties[stateKey] = StaleValue;",
    "_properties[snapshotKey] = signature;",
    "_properties[GeneratedCurtainPanelStateKey] = StaleValue;",
    "_properties[GeneratedCurtainPanelStaleSnapshotKey] = signature;",
    "_properties[GeneratedGeometryStateKey] = StaleValue;",
    "_properties[GeneratedGeometryStaleReasonKey] = normalizedReason;",
)
for token in internal_write_tokens:
    if token not in source:
        fail(f"generated-state internal bookkeeping must bypass the public semantic facade to avoid recursive dirty transitions: {token}")

print("PASS: ProjectElement public property-map mutations preserve validation, dirty tracking, generated-output invalidation, and no-op semantics")
