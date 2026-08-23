#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
METADATA = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMetadataDictionary.cs"
AUTOROOM = ROOT / "src" / "QS3D.Core" / "Domain" / "AutoRoomLifecycle.cs"

metadata = METADATA.read_text(encoding="utf-8")
autoroom = AUTOROOM.read_text(encoding="utf-8")

helper = "internal void EnsureCanApplyOwned(IEnumerable<string> removeKeys, IEnumerable<string> setKeys)"
if helper not in metadata:
    raise SystemExit("FAIL: canonical metadata batch-capacity preflight helper is missing")
helper_start = metadata.index(helper)
helper_end = metadata.index("internal void AddOwned", helper_start)
helper_body = metadata[helper_start:helper_end]
for token in (
    "new HashSet<string>(_items.Keys, StringComparer.OrdinalIgnoreCase)",
    "finalKeys.Remove(key);",
    "if (finalKeys.Contains(key)) continue;",
    "if (finalKeys.Count >= MaximumEntries) throw MetadataCountError();",
    "finalKeys.Add(key);",
):
    if token not in helper_body:
        raise SystemExit(f"FAIL: metadata batch-capacity helper missing invariant: {token}")

sync_start = autoroom.index("public static int SyncFamilyDefaults")
sync_end = autoroom.index("public static bool IsExcludedFromQuantity", sync_start)
sync = autoroom[sync_start:sync_end]
preflight = "metadata.EnsureCanApplyOwned(metadataRemoves, metadataSets.Keys);"
touch = "project.Touch();"
if preflight not in sync:
    raise SystemExit("FAIL: AutoRoom family-default sync does not preflight metadata capacity")
if touch not in sync:
    raise SystemExit("FAIL: AutoRoom family-default sync project mutation boundary missing")
if sync.index(preflight) >= sync.index(touch):
    raise SystemExit("FAIL: AutoRoom metadata capacity must be preflighted before project.Touch()")

for mutation in (
    "foreach (var key in roomRemoves) room.Properties.Remove(key);",
    "foreach (var property in roomSets) room.Properties[property.Key] = property.Value;",
    "foreach (var key in metadataRemoves) metadata.RemoveOwned(key);",
    "foreach (var property in metadataSets) metadata.SetOwned(property.Key, property.Value);",
    "if (familyChanged) room.FamilyId = family.Id;",
):
    if mutation not in sync:
        raise SystemExit(f"FAIL: expected AutoRoom mutation contract missing: {mutation}")
    if sync.index(preflight) >= sync.index(mutation):
        raise SystemExit(f"FAIL: metadata capacity preflight must precede mutation: {mutation}")

if "private const int MaxProjectMetadata" in autoroom or "10000" in sync:
    raise SystemExit("FAIL: AutoRoom duplicated canonical project-metadata capacity instead of using metadata store authority")

print("PASS: AutoRoom family-default sync preflights net metadata capacity before mutation")
print("PASS: canonical metadata store accounts for removals, replacements and net-new set keys")
print("PASS: AutoRoom does not duplicate the project-metadata capacity constant")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
