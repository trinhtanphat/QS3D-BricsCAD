#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOOR = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
ZONE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFloorZoneMutationIntegritySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFloorZoneMutationIntegritySmokeRegistration.cs"


def require(text, tokens, label, missing):
    missing.extend(label + ": " + token for token in tokens if token not in text)


def method_slice(text, start_token, end_token):
    start = text.find(start_token)
    if start < 0:
        return ""
    end = text.find(end_token, start)
    return text[start:] if end < 0 else text[start:end]


def main():
    floor = FLOOR.read_text(encoding="utf-8")
    zone = ZONE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    require(floor, [
        'string.Equals((project.ActiveFloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase)',
        'targets.Where(x => !string.Equals((x.FloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase))',
        'throw new InvalidOperationException("Floor mutation target collection contains a null element.");',
    ], "floor", missing)
    require(zone, [
        'string.Equals((project.ActiveZoneId ?? string.Empty).Trim(), zone.Id, StringComparison.OrdinalIgnoreCase)',
        '.Where(x => !string.Equals((x.ZoneId ?? string.Empty).Trim(), zone.Id, StringComparison.OrdinalIgnoreCase))',
        'throw new InvalidOperationException("Zone assignment target collection contains a null element.");',
    ], "zone", missing)
    require(smoke, [
        'FloorActiveCanonicalIdentityIsNoOp();',
        'ZoneActiveCanonicalIdentityIsNoOp();',
        'FloorAssignmentCanonicalIdentityIsNoOp();',
        'ZoneAssignmentCanonicalIdentityIsNoOp();',
        'FloorNullTargetFailsAtomically();',
        'ZoneNullTargetFailsAtomically();',
        'Equal(beforeVersion, project.ChangeVersion);',
        'Equal(beforeUpdatedUtc, element.UpdatedUtc);',
        'new ProjectElement[] { element, null! }',
    ], "smoke", missing)
    require(registration, [
        '[ModuleInitializer]',
        'ProjectFloorZoneMutationIntegritySmoke.Run();',
    ], "registration", missing)

    if missing:
        print("ERROR: Floor/Zone mutation-integrity contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    floor_active = method_slice(
        floor,
        'public static void SetActive(ProjectState project, string floorId)',
        'public static int Assign(ProjectState project, string floorId')
    floor_assign = method_slice(
        floor,
        'public static int Assign(ProjectState project, string floorId',
        'public static int AssignBottomLevel')
    zone_active = method_slice(
        zone,
        'public static void SetActive(ProjectState project, string zoneId)',
        'public static int Assign(ProjectState project, string zoneId')
    zone_assign = method_slice(
        zone,
        'public static int Assign(ProjectState project, string zoneId',
        'public static bool Delete')

    unsafe = [
        ("floor SetActive", floor_active, 'string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)'),
        ("floor Assign", floor_assign, 'string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase)'),
        ("zone SetActive", zone_active, 'string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)'),
        ("zone Assign", zone_assign, 'string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)'),
        ("floor target validation", floor, 'if (element == null) continue;'),
        ("zone target validation", zone_assign, 'if (element == null) continue;'),
    ]
    for label, text, token in unsafe:
        if token in text:
            print("ERROR: unsafe " + label + " behavior returned: " + token)
            return 1

    for label, text in (("floor Assign", floor_assign), ("zone Assign", zone_assign)):
        null_guard = text.find('if (element == null)')
        touch = text.find('project.Touch();')
        if null_guard < 0 or touch < 0 or null_guard > touch:
            print("ERROR: " + label + " null-target validation must complete before mutation.")
            return 1

    print("PASS: Floor/Zone activation and assignment use canonical no-op identity, and null-containing object-target batches fail closed before mutation with module-registered Core regression coverage.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
