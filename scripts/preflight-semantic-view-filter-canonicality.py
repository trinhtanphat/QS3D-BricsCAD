#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticViewPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewFilterCanonicalitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewFilterCanonicalitySmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    start = source.find("public static SemanticViewPlan Build(ProjectState project, SemanticViewDefinition definition)")
    end = source.find("public static IReadOnlyList<SemanticViewPlan> BuildCatalog", start)
    if start < 0 or end < 0:
        print("ERROR: SemanticViewPlanner.Build method boundary not found.")
        return 1
    build = source[start:end]

    required = [
        'EnsureUniqueReference(project.Floors, x => x.Id, floorId, "floor")',
        'EnsureUniqueReference(project.Zones, x => x.Id, zoneId, "zone")',
        'string.Equals((x.FloorId ?? string.Empty).Trim(), floorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals((x.ZoneId ?? string.Empty).Trim(), zoneId, StringComparison.OrdinalIgnoreCase)',
    ]
    for token in required:
        if token not in build:
            print("ERROR: missing semantic view canonical filter contract: " + token)
            return 1

    legacy = [
        'string.Equals(x.FloorId, floorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(x.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase)',
    ]
    for token in legacy:
        if token in build:
            print("ERROR: raw semantic view relation comparison returned: " + token)
            return 1

    smoke_tokens = [
        "PaddedCaseVariedRelationsStillMatchCanonicalFilters();",
        'FloorId = "  f-01  "',
        'ZoneId = "  z-01  "',
        '"F-01"',
        '"Z-01"',
        "SemanticViewPlanner.Build(project, definition);",
        "Equal(1, plan.ElementIds.Count);",
        'Equal("E-01", plan.ElementIds[0]);',
        "Equal(beforeVersion, project.ChangeVersion);",
        "Equal(beforeFloorId, element.FloorId);",
        "Equal(beforeZoneId, element.ZoneId);",
    ]
    for token in smoke_tokens:
        if token not in smoke:
            print("ERROR: missing semantic view canonical filter smoke token: " + token)
            return 1

    if "[ModuleInitializer]" not in registration or "SemanticViewFilterCanonicalitySmoke.Run();" not in registration:
        print("ERROR: semantic view filter canonicality smoke is not module-registered.")
        return 1

    print("PASS: SemanticViewPlanner.Build matches Floor/Zone relations by trimmed case-insensitive semantic identity without mutating raw relations or project state.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
