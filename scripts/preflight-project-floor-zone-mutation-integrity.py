#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FLOOR = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
ZONE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
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


def require_order(text, tokens, label, missing):
    positions = [text.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        return
    if positions != sorted(positions):
        missing.append(label + ": contract ordering changed")


def main():
    paths = (FLOOR, ZONE, PROJECT_STATE, SMOKE, REGISTRATION)
    missing = []
    for path in paths:
        if not path.is_file():
            missing.append("missing contract file: " + str(path.relative_to(ROOT)))
    if missing:
        for item in missing:
            print("ERROR:", item)
        return 1

    floor = FLOOR.read_text(encoding="utf-8")
    zone = ZONE.read_text(encoding="utf-8")
    project_state = PROJECT_STATE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    # Floor intentionally retains its historical trim/repair compatibility contract.
    require(floor, [
        'string.Equals((project.ActiveFloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase)',
        'targets.Where(x => !string.Equals((x.FloorId ?? string.Empty).Trim(), floor.Id, StringComparison.OrdinalIgnoreCase))',
        'throw new InvalidOperationException("Floor mutation target collection contains a null element.");',
    ], "floor", missing)

    # Zone semantic identities are strict: validate before comparison/classification/mutation.
    require(zone, [
        'var canonicalId = RequiredIdentity(id, nameof(id), 64);',
        'var activeZoneId = OptionalIdentity(project.ActiveZoneId, "Project ActiveZoneId", 64);',
        '.Where(x => !string.Equals(OptionalIdentity(x.ZoneId, "Element ZoneId", 64), zone.Id, StringComparison.OrdinalIgnoreCase))',
        'var elementId = RequiredIdentity(element.Id, "Zone assignment target element id", 128);',
        'throw new InvalidOperationException("Zone assignment target collection contains a null element.");',
    ], "zone", missing)
    for forbidden in (
        '(project.ActiveZoneId ?? string.Empty).Trim()',
        '(x.ZoneId ?? string.Empty).Trim()',
        'project.FindZone(normalized)',
    ):
        if forbidden in zone:
            missing.append("zone strict identity contract regressed: " + forbidden)

    require(project_state, [
        'set => SetActiveContextId(ref _activeFloorId, value);',
        'set => SetActiveContextId(ref _activeZoneId, value);',
        'private void SetActiveContextId(ref string field, string? value)',
        'var normalizedValue = (value ?? string.Empty).Trim();',
        'if (normalizedValue.Any(char.IsControl))',
        'SetPersistedScalar(ref field, PersistedTextXml.Verify(normalizedValue, nameof(value), "Active context id"));',
        'var nextChangeVersion = checked(ChangeVersion + 1L);',
    ], "project state", missing)

    require(smoke, [
        'FloorActiveAliasIsCanonicalRepair();',
        'ZonePaddedActiveIdFailsAtomically();',
        'FloorAssignmentCanonicalIdentityIsNoOp();',
        'ZoneAssignmentCanonicalIdentityIsNoOp();',
        'FloorNullTargetFailsAtomically();',
        'ZoneNullTargetFailsAtomically();',
        'ThrowsArgument(() => ProjectZoneService.SetActive(project, " Z-01 "));',
        'Equal(beforeVersion, project.ChangeVersion);',
        'Equal(beforeUpdatedUtc, element.UpdatedUtc);',
        'new ProjectElement[] { element, null! }',
    ], "smoke", missing)
    if 'ZoneActiveAliasIsCanonicalRepair();' in smoke:
        missing.append("smoke still requires obsolete Zone trim-and-repair behavior")

    require(registration, [
        '[ModuleInitializer]',
        'ProjectFloorZoneMutationIntegritySmoke.Run();',
    ], "registration", missing)

    floor_create = method_slice(
        floor,
        'public static FloorDefinition Create(ProjectState project, string id, string name, double elevationM)',
        'public static FloorDefinition Update(ProjectState project, string id, string name, double elevationM)')
    require(floor_create, [
        'var activate = string.IsNullOrWhiteSpace(project.ActiveFloorId);',
        'if (activate) project.ActiveFloorId = floor.Id;',
        'else project.Touch();',
        'project.Floors.Add(floor);',
    ], "floor create", missing)
    require_order(floor_create, [
        'var activate = string.IsNullOrWhiteSpace(project.ActiveFloorId);',
        'if (activate) project.ActiveFloorId = floor.Id;',
        'else project.Touch();',
        'project.Floors.Add(floor);',
    ], "floor create", missing)

    zone_create = method_slice(
        zone,
        'public static ZoneDefinition Create(ProjectState project, string id, string name)',
        'public static ZoneDefinition Update(ProjectState project, string id, string name)')
    require(zone_create, [
        'var activeZoneId = OptionalIdentity(project.ActiveZoneId, "Project ActiveZoneId", 64);',
        'var activate = activeZoneId.Length == 0;',
        'if (activate) project.ActiveZoneId = zone.Id;',
        'else project.Touch();',
        'project.Zones.Add(zone);',
    ], "zone create", missing)
    require_order(zone_create, [
        'var activeZoneId = OptionalIdentity(project.ActiveZoneId, "Project ActiveZoneId", 64);',
        'var activate = activeZoneId.Length == 0;',
        'if (activate) project.ActiveZoneId = zone.Id;',
        'else project.Touch();',
        'project.Zones.Add(zone);',
    ], "zone create", missing)

    floor_assign = method_slice(
        floor,
        'public static int Assign(ProjectState project, string floorId',
        'public static int AssignBottomLevel')
    floor_resolver = method_slice(
        floor,
        'private static IReadOnlyList<ProjectElement> ResolveOwnedElements',
        'private static IReadOnlyList<ProjectElement> ResolveProjectElements')
    zone_assign = method_slice(
        zone,
        'public static int Assign(ProjectState project, string zoneId',
        'public static bool Delete')

    floor_resolve_call = floor_assign.find('var targets = ResolveOwnedElements(project, elements);')
    floor_touch = floor_assign.find('project.Touch();')
    floor_null_guard = floor_resolver.find('if (element == null)')
    floor_ownership_guard = floor_resolver.find('if (!projectElements.TryGetValue(element.Id')
    if floor_resolve_call < 0 or floor_touch < 0 or floor_resolve_call > floor_touch:
        missing.append("Floor target resolution must complete before mutation")
    if floor_null_guard < 0 or floor_ownership_guard < 0 or floor_null_guard > floor_ownership_guard:
        missing.append("Floor null-target guard must run before ownership dereference")

    zone_null_guard = zone_assign.find('if (element == null)')
    zone_identity_guard = zone_assign.find('var elementId = RequiredIdentity(element.Id, "Zone assignment target element id", 128);')
    zone_touch = zone_assign.find('project.Touch();')
    if zone_null_guard < 0 or zone_identity_guard < 0 or zone_touch < 0:
        missing.append("Zone null/identity/mutation ordering tokens are missing")
    elif not (zone_null_guard < zone_identity_guard < zone_touch):
        missing.append("Zone null and canonical-identity validation must complete before mutation")

    if missing:
        print("ERROR: Floor/Zone mutation-integrity contract is incomplete:")
        for item in missing:
            print(" -", item)
        return 1

    print("PASS: Floor compatibility repair remains intact; Zone semantic identities fail closed before lookup/mutation; canonical no-op and null-target atomicity contracts remain covered.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
