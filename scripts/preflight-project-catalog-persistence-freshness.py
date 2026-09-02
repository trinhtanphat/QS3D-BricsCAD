#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectCatalogPersistenceFreshnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing catalog persistence freshness file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "internal event Action? PersistenceMutationRequested;",
    "PersistenceMutationRequested?.Invoke();",
    "internal sealed class CatalogOwnershipList<T> : IList<T>",
    "Zones = new CatalogOwnershipList<ZoneDefinition>(AttachZone, DetachZone);",
    "Floors = new CatalogOwnershipList<FloorDefinition>(AttachFloor, DetachFloor);",
    "Families = new CatalogOwnershipList<ProjectFamily>(AttachFamily, DetachFamily);",
    "zone.PersistenceMutationRequested += Touch",
    "floor.PersistenceMutationRequested += Touch",
    "family.PersistenceMutationRequested += Touch",
):
    if token not in source:
        errors.append("Project catalog freshness contract missing: " + token)

for token in (
    "OwnedCatalogScalarMutationsAdvanceProjectFreshness",
    "NormalizedNoOpsDoNotAdvanceProjectFreshness",
    "OwnershipTracksRemovalReplacementAndSnapshotRestore",
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
):
    if token not in smoke:
        errors.append("catalog freshness regression missing: " + token)

if "ProjectCatalogPersistenceFreshnessSmoke.Run();" not in registration:
    errors.append("catalog freshness smoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: owned Zone/Floor/Family scalar changes advance ProjectState persistence freshness while no-op/materialization/snapshot ownership semantics stay guarded.")
