#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AutoRoomCanonicalScopeSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AutoRoomCanonicalScopeSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        '.Where(x => SameScopeId(x.FloorId, floorId))',
        '.Where(x => SameScopeId(x.ZoneId, zoneId))',
        '.Where(room => SameScopeId(room.FloorId, floorId))',
        '.Where(room => SameScopeId(room.ZoneId, zoneId))',
        'if (!SameScopeId(room.FloorId, element.FloorId) ||',
        '!SameScopeId(room.ZoneId, element.ZoneId)) return true;',
        'private static bool SameScopeId(string? left, string? right)',
        'string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)',
    ]
    required_smoke = [
        'FindBySignatureUsesCanonicalScope();',
        'StaleSelectionUsesCanonicalScope();',
        'FinishQuantityScopeUsesCanonicalIdentity();',
        'FloorId = "  floor-a  "',
        'ZoneId = " zone-a "',
        'AutoRoomLifecycle.FindBySourceSignature',
        'AutoRoomLifecycle.MarkStaleForSelection',
        'AutoRoomLifecycle.IsExcludedFromQuantity(project, finish)',
    ]
    required_registration = [
        '[ModuleInitializer]',
        'AutoRoomCanonicalScopeSmoke.Run();',
    ]

    missing = ["source: " + token for token in required_source if token not in source]
    missing += ["smoke: " + token for token in required_smoke if token not in smoke]
    missing += ["registration: " + token for token in required_registration if token not in registration]
    if missing:
        print("ERROR: Auto Room canonical-scope contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = [
        'string.Equals(x.FloorId, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(x.ZoneId, zoneId ?? string.Empty, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(room.FloorId, floorId ?? string.Empty, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(room.ZoneId, zoneId ?? string.Empty, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(room.FloorId, element.FloorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(room.ZoneId, element.ZoneId, StringComparison.OrdinalIgnoreCase)',
    ]
    for token in unsafe:
        if token in source:
            print("ERROR: raw Auto Room scope comparison returned:", token)
            return 1

    find_start = source.find("public static ProjectElement? FindBySourceSignature")
    stale_start = source.find("public static IReadOnlyList<ProjectElement> MarkStaleForSelection")
    mark_active_start = source.find("public static void MarkActive", stale_start)
    quantity_start = source.find("public static bool IsExcludedFromQuantity")
    resolve_start = source.find("private static IReadOnlyList<ProjectElement> ResolveProjectElements", quantity_start)
    if min(find_start, stale_start, mark_active_start, quantity_start, resolve_start) < 0:
        print("ERROR: cannot isolate Auto Room scope methods.")
        return 1
    if "SameScopeId" not in source[find_start:stale_start]:
        print("ERROR: FindBySourceSignature must route Floor/Zone scope through SameScopeId.")
        return 1
    if "SameScopeId" not in source[stale_start:mark_active_start]:
        print("ERROR: MarkStaleForSelection must route Floor/Zone scope through SameScopeId.")
        return 1
    if "SameScopeId" not in source[quantity_start:resolve_start]:
        print("ERROR: IsExcludedFromQuantity must route Room/finish scope through SameScopeId.")
        return 1

    print("PASS: Auto Room reuse, stale selection and finish quantity inclusion use canonical trimmed Floor/Zone scope identity with module-registered smoke coverage.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
