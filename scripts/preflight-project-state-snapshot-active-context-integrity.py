#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotActiveContextIntegritySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
QSDB_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbActiveContextReferentialIntegritySmoke.cs"


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        raise AssertionError(f"missing {label}: {token}")
    return pos


def main() -> int:
    snapshot = SNAPSHOT.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")
    qsdb_smoke = QSDB_SMOKE.read_text(encoding="utf-8")

    validate = require(snapshot, "private static void ValidateCollectionEntries(ProjectState source)", "snapshot validation method")
    zone_unique = require(snapshot, 'RequireUniqueIds(source.Zones, x => x.Id, "zone");', "zone uniqueness validation")
    floor_unique = require(snapshot, 'RequireUniqueIds(source.Floors, x => x.Id, "floor");', "floor uniqueness validation")
    active = require(snapshot, "RequireResolvedActiveContext(source);", "active-context referential validation")
    family_validation = require(snapshot, "foreach (var family in source.Families)", "later family validation")
    if not (validate < zone_unique < floor_unique < active < family_validation):
        raise AssertionError("active-context references must be checked after catalog uniqueness and before later snapshot materialization validation")

    helper = require(snapshot, "private static void RequireResolvedActiveContext(ProjectState source)", "active-context helper")
    helper_slice = snapshot[helper:snapshot.find("private static void RequireCanonicalFamilyProperties", helper)]
    for token in (
        "source.ActiveZoneId.Length != 0",
        "source.FindZone(source.ActiveZoneId) == null",
        "source.ActiveFloorId.Length != 0",
        "source.FindFloor(source.ActiveFloorId) == null",
    ):
        require(helper_slice, token, "canonical ProjectState lookup usage")
    for forbidden in ("ActiveZoneId = string.Empty", "ActiveFloorId = string.Empty", "Zones.FirstOrDefault", "Floors.FirstOrDefault"):
        if forbidden in helper_slice:
            raise AssertionError("snapshot active-context validation must fail closed and reuse canonical lookup semantics: " + forbidden)

    for token in (
        "RejectsDanglingZoneContextWithoutMutation();",
        "RejectsDanglingFloorContextWithoutMutation();",
        "PreservesResolvedContextIdentities();",
        "PreservesEmptyContextIdentities();",
        "ProjectStateSnapshot.Capture(project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        'Equal(beforeVersion, project.ChangeVersion',
        'Equal(beforeUpdatedUtc, project.UpdatedUtc',
        'Equal("zone-a", copy.ActiveZoneId',
        'Equal("floor-a", copy.ActiveFloorId',
    ):
        require(smoke, token, "deterministic snapshot active-context smoke coverage")

    require(registration, "ProjectStateSnapshotActiveContextIntegritySmoke.Run();", "registered active-context smoke")
    require(qsdb_smoke, "RejectsOrphanActiveFloorId();", "QSDB orphan active-floor parity")
    require(qsdb_smoke, "RejectsOrphanActiveZoneId();", "QSDB orphan active-zone parity")
    require(qsdb_smoke, "AcceptsResolvedActiveContextIds();", "QSDB resolved active-context parity")

    print("PASS: project state snapshot active context integrity preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
