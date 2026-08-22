#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "GridNamingService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridNamingBoundedEnumerationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridNamingBoundedEnumerationSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    start = source.find("public static IReadOnlyList<GridLabelAssignment> Renumber(")
    end = source.find("public static string FormatLabel(", start)
    if start < 0 or end < 0:
        print("ERROR: GridNamingService.Renumber method boundary not found.")
        return 1
    renumber = source[start:end]

    required = [
        "var targetEnumerationVersion = project.ChangeVersion;",
        "var projectElementsAtStart = project.Elements.ToList();",
        "var ids = new List<string>();",
        "foreach (var value in orderedGridElementIds)",
        "if (ids.Count == MaxGridBatch)",
        'throw new InvalidOperationException("A Grid renumber batch supports at most " + MaxGridBatch + " elements.");',
        'ids.Add(Required(value, "orderedGridElementIds[" + ids.Count + "]", 128));',
        "if (project.ChangeVersion != targetEnumerationVersion)",
        "var originalTargets = ResolveOriginalTargets(projectElementsAtStart, ids);",
        "var projectElements = ResolveProjectElements(project);",
    ]
    for token in required:
        if token not in renumber:
            print("ERROR: missing bounded/fresh Grid naming contract: " + token)
            return 1

    legacy = [
        ".Select((value, index) => Required(",
        "orderedGridElementIds.ToList()",
        "if (ids.Count > MaxGridBatch)",
    ]
    for token in legacy:
        if token in renumber:
            print("ERROR: legacy post-materialization Grid naming capacity path returned: " + token)
            return 1

    snapshot = renumber.find("var projectElementsAtStart = project.Elements.ToList();")
    loop = renumber.find("foreach (var value in orderedGridElementIds)")
    cap = renumber.find("if (ids.Count == MaxGridBatch)", loop)
    add = renumber.find("ids.Add(Required(value", loop)
    freshness = renumber.find("if (project.ChangeVersion != targetEnumerationVersion)", add)
    original = renumber.find("var originalTargets = ResolveOriginalTargets(projectElementsAtStart, ids);", freshness)
    resolve = renumber.find("var projectElements = ResolveProjectElements(project);", original)
    if min(snapshot, loop, cap, add, freshness, original, resolve) < 0 or not (
        snapshot < loop < cap < add < freshness < original < resolve
    ):
        print("ERROR: Grid naming must snapshot project identity, bound caller enumeration before normalization/add, verify freshness, then resolve current project state.")
        return 1

    smoke_tokens = [
        "OversizeLazyInputStopsAtFirstItemBeyondCapacity();",
        "GridNamingService.Renumber(project, source.Values());",
        'Equal("A Grid renumber batch supports at most 2000 elements.", ex.Message);',
        "Equal(2001, source.YieldCount);",
        "if (YieldCount > 2001)",
        "Grid renumber enumerated beyond the first item over capacity.",
        "Equal(beforeVersion, project.ChangeVersion);",
    ]
    for token in smoke_tokens:
        if token not in smoke:
            print("ERROR: missing Grid naming bounded-enumeration smoke token: " + token)
            return 1

    if "[ModuleInitializer]" not in registration or "GridNamingBoundedEnumerationSmoke.Run();" not in registration:
        print("ERROR: Grid naming bounded-enumeration smoke is not module-registered.")
        return 1

    print("PASS: GridNamingService.Renumber snapshots project identity while bounding lazy target enumeration at the first item beyond the 2,000-item capacity, before current-state resolution or mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
