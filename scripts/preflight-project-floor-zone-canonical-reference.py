#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOOR = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
ZONE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFloorZoneCanonicalReferenceSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFloorZoneCanonicalReferenceSmokeRegistration.cs"


def require(text, tokens, label, missing):
    missing.extend(label + ": " + token for token in tokens if token not in text)


def main():
    floor = FLOOR.read_text(encoding="utf-8")
    zone = ZONE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    missing = []
    require(floor, [
        'string.Equals((project.ActiveFloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase)',
        'var normalizedFloorId = floorId.Trim();',
        'string.Equals((element.FloorId ?? string.Empty).Trim(), normalizedFloorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(Property(element, BottomLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(Property(element, TopLevelIdKey), normalizedFloorId, StringComparison.OrdinalIgnoreCase)',
    ], "floor", missing)
    require(zone, [
        'var activeZoneId = OptionalIdentity(project.ActiveZoneId, "Project ActiveZoneId", 64);',
        '.Where(x => ReferencesZone(x, zone.Id))',
        'ResolveProjectElements(project).Count(x => ReferencesZone(x, zone.Id))',
        'private static bool ReferencesZone(ProjectElement element, string zoneId)',
        'OptionalIdentity(element.ZoneId, "Element ZoneId", 64)',
        'var canonicalId = RequiredIdentity(id, nameof(id), 64);',
        'var elementId = RequiredIdentity(element.Id, "Project semantic element id", 128);',
        'private static string RequiredIdentity(string value, string parameterName, int maxLength)',
        'private static string OptionalIdentity(string value, string parameterName, int maxLength)',
    ], "zone", missing)
    require(smoke, [
        'FloorReferenceIdentityIsCanonical();',
        'PaddedActiveFloorBlocksDelete();',
        'ZoneReferenceIdentityFailsClosed();',
        'PaddedActiveZoneFailsClosed();',
        'FloorId = "  f-01  "',
        'ProjectFloorService.ReferenceCount(project, " F-01 ")',
        'SetRawZoneId(element, "  z-01  ");',
        'ThrowsArgument(() => ProjectZoneService.ReferenceCount(project, zone.Id));',
        'ThrowsArgument(() => ProjectZoneService.Update(project, zone.Id, "Zone 01 renamed"));',
        'SetRawActiveZoneId(project, "  zONE-a  ");',
        'ThrowsArgument(() => ProjectZoneService.Delete(project, zone.Id));',
    ], "smoke", missing)
    require(registration, [
        '[ModuleInitializer]',
        'ProjectFloorZoneCanonicalReferenceSmoke.Run();',
    ], "registration", missing)

    if missing:
        print("ERROR: Floor/Zone canonical-reference contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = [
        ('floor', floor, 'string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)'),
        ('floor', floor, 'string.Equals(element.FloorId, floorId, StringComparison.OrdinalIgnoreCase)'),
        ('zone', zone, 'string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)'),
        ('zone', zone, '.Count(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))'),
        ('zone', zone, '.Where(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))'),
        ('zone', zone, 'string.Equals((element.ZoneId ?? string.Empty).Trim(), zoneId, StringComparison.OrdinalIgnoreCase)'),
        ('zone', zone, 'string.Equals((project.ActiveZoneId ?? string.Empty).Trim(), zone.Id, StringComparison.OrdinalIgnoreCase)'),
    ]
    for label, text, token in unsafe:
        if token in text:
            print("ERROR: raw/trim-alias " + label + " reference comparison returned: " + token)
            return 1

    floor_delete_start = floor.find('public static bool Delete(ProjectState project, string floorId)')
    floor_delete = floor[floor_delete_start:floor.find('public static int ReferenceCount', floor_delete_start)]
    zone_delete_start = zone.find('public static bool Delete(ProjectState project, string zoneId)')
    zone_delete = zone[zone_delete_start:zone.find('public static int ReferenceCount', zone_delete_start)]
    if floor_delete.find('.Trim()') < 0 or floor_delete.find('.Trim()') > floor_delete.find('project.Touch();'):
        print("ERROR: active Floor canonical guard must run before mutation.")
        return 1

    zone_active_validation = zone_delete.find('OptionalIdentity(project.ActiveZoneId, "Project ActiveZoneId", 64)')
    if zone_active_validation < 0 or zone_active_validation > zone_delete.find('project.Touch();'):
        print("ERROR: active Zone canonical validation must run before mutation.")
        return 1

    resolve_start = zone.find('private static IReadOnlyList<ProjectElement> ResolveProjectElements(ProjectState project)')
    resolve_end = zone.find('private static void EnsureUniqueName', resolve_start)
    resolve = zone[resolve_start:resolve_end]
    if resolve.find('RequiredIdentity(element.Id, "Project semantic element id", 128)') < 0:
        print("ERROR: project semantic element ids must be validated before Zone indexing.")
        return 1
    zone_reference_validation = resolve.find('OptionalIdentity(element.ZoneId, "Element ZoneId", 64)')
    if zone_reference_validation < 0 or zone_reference_validation > resolve.find('resolved.Add(element);'):
        print("ERROR: stored Zone references must be validated before project element materialization completes.")
        return 1

    print("PASS: Floor retains canonical trimmed-reference compatibility while Zone semantic identities/references fail closed before lookup or mutation with focused smoke coverage.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
