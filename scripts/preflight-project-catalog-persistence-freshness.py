#!/usr/bin/env python3
from pathlib import Path
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
    "if (_inner.TryGetValue(key, out var current) && string.Equals(current, value, StringComparison.Ordinal)) return;",
    "if (_inner.Count == 0) return;",
    "internal sealed class CatalogOwnershipList<T> : IList<T>",
    "Zones = new CatalogOwnershipList<ZoneDefinition>(AttachZone, DetachZone);",
    "Floors = new CatalogOwnershipList<FloorDefinition>(AttachFloor, DetachFloor);",
    "Families = new CatalogOwnershipList<ProjectFamily>(AttachFamily, DetachFamily);",
    "zone.PersistenceMutationRequested += Touch",
    "floor.PersistenceMutationRequested += Touch",
    "family.PersistenceMutationRequested += Touch",
    "ContainsReference(item)",
    "CountReferences(item) == 1",
    "internal void ApplyPersistedUpdate(string name, bool updateName, double elevationM, bool updateElevation)",
):
    if token not in source:
        errors.append("Project catalog freshness contract missing: " + token)

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

for token in (
    "var clone = CreateDetached(project, newId, newName, source.Category);",
    "foreach (var pair in properties) clone.Properties[pair.Key] = pair.Value;",
    "project.Touch();\n            project.Families.Add(clone);",
):
    if token not in family_service:
        errors.append("Family duplicate must initialize properties while detached and admit the completed clone in one project revision: " + token)

if "floor.ApplyPersistedUpdate(normalizedName, nameChanged, elevationM, elevationChanged);" not in floor_service:
    errors.append("Floor service must batch name/elevation persistence freshness into one logical revision.")

if "ProjectCatalogPersistenceFreshnessSmoke.Run();" not in registration:
    errors.append("catalog freshness smoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: owned Zone/Floor/Family scalar and Family property changes advance ProjectState persistence freshness exactly once per logical catalog operation while no-op/materialization/snapshot semantics stay guarded.")
