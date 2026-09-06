#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
FAMILY_SERVICE = ROOT / "src/QS3D.Core/Domain/ProjectFamilyService.cs"
ZONE_SERVICE = ROOT / "src/QS3D.Core/Domain/ProjectZoneService.cs"
FLOOR_SERVICE = ROOT / "src/QS3D.Core/Domain/ProjectFloorService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectCatalogPersistenceFreshnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing catalog persistence freshness file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
family_service = read(FAMILY_SERVICE)
zone_service = read(ZONE_SERVICE)
floor_service = read(FLOOR_SERVICE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "internal event Action? PersistenceMutationRequested;",
    "PersistenceMutationRequested?.Invoke();",
    "private sealed class PersistenceAwarePropertyDictionary : IDictionary<string, string>",
    "private readonly PersistenceAwarePropertyDictionary _properties;",
    "_properties = new PersistenceAwarePropertyDictionary(() => PersistenceMutationRequested?.Invoke());",
    "Properties = _properties;",
    "if (_inner.Count == 0) return;",
    "internal sealed class CatalogOwnershipList<T> : IList<T>",
    "private readonly Action _beforeMutation;",
    "internal CatalogOwnershipList(Action<T> attach, Action<T> detach, Action beforeMutation)",
    "Zones = new CatalogOwnershipList<ZoneDefinition>(AttachZone, DetachZone, Touch);",
    "Floors = new CatalogOwnershipList<FloorDefinition>(AttachFloor, DetachFloor, Touch);",
    "Families = new CatalogOwnershipList<ProjectFamily>(AttachFamily, DetachFamily, Touch);",
    "zone.PersistenceMutationRequested += Touch",
    "floor.PersistenceMutationRequested += Touch",
    "family.PersistenceMutationRequested += Touch",
    "ContainsReference(item)",
    "CountReferences(item) == 1",
    "internal void ApplyPersistedUpdate(string name, bool updateName, double elevationM, bool updateElevation)",
):
    if token not in source:
        errors.append("Project catalog freshness contract missing: " + token)

indexer_start = source.find("public string this[string key]")
indexer_end = source.find("public ICollection<string> Keys", indexer_start)
if indexer_start < 0 or indexer_end < 0:
    errors.append("Project catalog freshness contract missing the persistence-aware family property indexer")
else:
    indexer = source[indexer_start:indexer_end]
    no_op = re.search(
        r"if\s*\(_inner\.TryGetValue\(([^,]+),\s*out var current\)\s*&&\s*"
        r"string\.Equals\(current,\s*([^,]+),\s*StringComparison\.Ordinal\)\)\s*return;",
        indexer,
    )
    write = re.search(r"_inner\[(.+?)\]\s*=\s*(.+?);", indexer)
    callback = indexer.find("_beforeMutation();")
    if no_op is None:
        errors.append("Family property indexer must retain an ordinal same-value no-op before persistence mutation")
    elif write is None:
        errors.append("Family property indexer must retain the persistence-aware dictionary write")
    elif callback < 0:
        errors.append("Family property indexer must retain the persistence mutation callback")
    else:
        lookup_key, compared_value = (part.strip() for part in no_op.groups())
        write_key, write_value = (part.strip() for part in write.groups())
        if lookup_key != write_key or compared_value != write_value:
            errors.append("Family property no-op comparison must use the exact key/value representation that is written")
        if not (no_op.end() <= callback < write.start()):
            errors.append("Family property same-value no-op must precede persistence mutation and the dictionary write")

for token in (
    "OwnedCatalogScalarMutationsAdvanceProjectFreshness",
    "OwnedFamilyPropertyMutationsAdvanceProjectFreshness",
    "NormalizedNoOpsDoNotAdvanceProjectFreshness",
    "OwnershipTracksRemovalReplacementAndSnapshotRestore",
    "DuplicateCatalogReferencesHaveSingleOwnershipSubscription",
    "ServiceRenameAdvancesProjectFreshnessExactlyOnce",
    "ServicePropertyMutationsAdvanceProjectFreshnessExactlyOnce",
    "ServiceDuplicateAdvancesProjectFreshnessExactlyOnce",
    "ServiceZoneUpdateAdvancesProjectFreshnessExactlyOnce",
    "ServiceFloorUpdateAdvancesProjectFreshnessOncePerLogicalUpdate",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "three persisted catalog structural adds must each advance freshness once",
):
    if token not in smoke:
        errors.append("catalog freshness regression missing: " + token)

for forbidden, label in (
    ("project.Touch();\n            family.Name = normalized;", "Family rename must not pre-touch before the owned scalar setter"),
    ("project.Touch();\n            family.Properties[normalizedKey] = normalizedValue;", "Family SetProperty must not pre-touch before the owned property store"),
    ("project.Touch();\n            family.Properties.Remove(normalizedKey);", "Family RemoveProperty must not pre-touch before the owned property store"),
    ("project.Touch();\n            zone.Name = normalizedName;", "Zone update must not pre-touch before the owned scalar setter"),
    ("project.Touch();\n            floor.Name = normalizedName;", "Floor update must not pre-touch before owned scalar mutation"),
):
    text = family_service if "family." in forbidden else zone_service if "zone.Name" in forbidden else floor_service
    if forbidden in text:
        errors.append(label)

for forbidden, label in (
    ("project.Touch();\n            project.Families.Add(family);", "Family create must let structural Add own revision admission"),
    ("project.Touch();\n            project.Families.Add(clone);", "Family duplicate must let structural Add own revision admission"),
    ("project.Touch();\n            return project.Families.Remove(family);", "Family delete must let structural Remove own revision admission"),
):
    if forbidden in family_service:
        errors.append(label)

for token in (
    "var clone = CreateDetached(project, newId, newName, source.Category);",
    "foreach (var pair in properties) clone.Properties[pair.Key] = pair.Value;",
    "project.Families.Add(clone);",
):
    if token not in family_service:
        errors.append("Family duplicate must initialize properties while detached and let structural admission publish the completed clone in one project revision: " + token)

if "floor.ApplyPersistedUpdate(normalizedName, nameChanged, elevationM, elevationChanged);" not in floor_service:
    errors.append("Floor service must batch name/elevation persistence freshness into one logical revision.")

if "ProjectCatalogPersistenceFreshnessSmoke.Run();" not in registration:
    errors.append("catalog freshness smoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: owned Zone/Floor/Family scalar, Family property, and structural catalog changes advance ProjectState persistence freshness exactly once per logical catalog operation while no-op and snapshot semantics stay guarded.")
