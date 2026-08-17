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
        'RequireCanonicalOptionalReference(project.ActiveZoneId, "ActiveZoneId");',
        '.Where(x => ReferencesZone(x, zone.Id))',
        'ResolveProjectElements(project).Count(x => ReferencesZone(x, zone.Id))',
        'private static bool ReferencesZone(ProjectElement element, string zoneId)',
        'RequireCanonicalOptionalReference(element.ZoneId, "Element ZoneId");',
        'private static string RequireCanonicalOptionalReference(string value, string fieldName)',
        'private static string RequiredCanonicalId(string value, string parameterName, int maxLength)',
        'if (!string.Equals(raw, canonical, StringComparison.Ordinal))',
        'if (!string.Equals(raw, normalized, StringComparison.Ordinal))',
    ], "zone", missing)
    require(smoke, [
        'FloorReferenceIdentityIsCanonical();',
        'PaddedActiveFloorBlocksDelete();',
        'ZoneReferenceIdentityIsCanonical();',
        'PaddedActiveZoneBlocksDelete();',
        'FloorId = "  f-01  "',
        'ZoneId = "z-01"',
        'ThrowsArgument(() => ProjectZoneService.ReferenceCount(project, " Z-01 "));',
        'ProjectZoneService.ReferenceCount(project, "z-01")',
        'ProjectFloorService.Update(project, floor.Id, floor.Name, 0.25d);',
        'ProjectZoneService.Update(project, zone.Id, "Zone 01 renamed");',
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
    ]
    for label, text, token in unsafe:
        if token in text:
            print("ERROR: raw " + label + " reference comparison returned: " + token)
            return 1

    floor_delete = floor[floor.find('public static bool Delete(ProjectState project, string floorId)'):floor.find('public static int ReferenceCount', floor.find('public static bool Delete(ProjectState project, string floorId)'))]
    zone_delete = zone[zone.find('public static bool Delete(ProjectState project, string zoneId)'):zone.find('public static int ReferenceCount', zone.find('public static bool Delete(ProjectState project, string zoneId)'))]
    if floor_delete.find('.Trim()') < 0 or floor_delete.find('.Trim()') > floor_delete.find('project.Touch();'):
        print("ERROR: active Floor canonical guard must run before mutation.")
        return 1
    zone_guard = zone_delete.find('RequireCanonicalOptionalReference(project.ActiveZoneId, "ActiveZoneId");')
    zone_touch = zone_delete.find('project.Touch();')
    if zone_guard < 0 or zone_touch < 0 or zone_guard > zone_touch:
        print("ERROR: active Zone canonical validation must run before comparison/mutation.")
        return 1

    print("PASS: Floor retains canonical trimmed-reference behavior while Zone rejects noncanonical semantic references before comparison/mutation, with module-registered smoke coverage.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
