#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticScheduleCatalog.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleFilterCanonicalitySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticScheduleFilterCanonicalitySmokeRegistration.cs"


def require_any(build, label, tokens):
    if any(token in build for token in tokens):
        return True
    print("ERROR: missing semantic schedule canonical filter contract: " + label)
    return False


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    start = source.find("public static SemanticDocumentationTable Build(ProjectState project, SemanticScheduleDefinition definition)")
    end = source.find("private static void ValidateCatalog", start)
    if start < 0 or end < 0:
        print("ERROR: SemanticScheduleCatalog.Build method boundary not found.")
        return 1
    build = source[start:end]

    required = [
        'project.FindFloor(normalized.FloorId)',
        'project.FindZone(normalized.ZoneId)',
        'SemanticDocumentationTableBuilder.Build(project, normalized.Title, ids, normalized.Columns, allowEmpty: true)',
    ]
    for token in required:
        if token not in build:
            print("ERROR: missing semantic schedule canonical filter contract: " + token)
            return 1

    if not require_any(
        build,
        "trimmed case-insensitive FloorId comparison",
        [
            'string.Equals((x.FloorId ?? string.Empty).Trim(), normalized.FloorId, StringComparison.OrdinalIgnoreCase)',
            'string.Equals((element.FloorId ?? string.Empty).Trim(), normalized.FloorId, StringComparison.OrdinalIgnoreCase)',
        ]):
        return 1
    if not require_any(
        build,
        "trimmed case-insensitive ZoneId comparison",
        [
            'string.Equals((x.ZoneId ?? string.Empty).Trim(), normalized.ZoneId, StringComparison.OrdinalIgnoreCase)',
            'string.Equals((element.ZoneId ?? string.Empty).Trim(), normalized.ZoneId, StringComparison.OrdinalIgnoreCase)',
        ]):
        return 1

    legacy = [
        'string.Equals(x.FloorId, normalized.FloorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(x.ZoneId, normalized.ZoneId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(element.FloorId, normalized.FloorId, StringComparison.OrdinalIgnoreCase)',
        'string.Equals(element.ZoneId, normalized.ZoneId, StringComparison.OrdinalIgnoreCase)',
    ]
    for token in legacy:
        if token in build:
            print("ERROR: raw semantic schedule relation comparison returned: " + token)
            return 1

    smoke_tokens = [
        "PaddedCaseVariedRelationsStillMatchCanonicalFilters();",
        'FloorId = "  f-01  "',
        'ZoneId = "  z-01  "',
        '"F-01"',
        '"Z-01"',
        "SemanticScheduleCatalog.Build(project, definition);",
        "Equal(1, table.Rows.Count);",
        "Equal(beforeVersion, project.ChangeVersion);",
        "Equal(beforeFloorId, element.FloorId);",
        "Equal(beforeZoneId, element.ZoneId);",
    ]
    for token in smoke_tokens:
        if token not in smoke:
            print("ERROR: missing semantic schedule canonical filter smoke token: " + token)
            return 1

    if "[ModuleInitializer]" not in registration or "SemanticScheduleFilterCanonicalitySmoke.Run();" not in registration:
        print("ERROR: semantic schedule filter canonicality smoke is not module-registered.")
        return 1

    print("PASS: SemanticScheduleCatalog.Build matches Floor/Zone relations by trimmed case-insensitive semantic identity without mutating raw relations or project state.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
