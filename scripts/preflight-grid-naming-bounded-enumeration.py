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
        "var ids = new List<string>();",
        "foreach (var value in orderedGridElementIds)",
        "if (ids.Count == MaxGridBatch)",
        'throw new InvalidOperationException("A Grid renumber batch supports at most " + MaxGridBatch + " elements.");',
        'ids.Add(Required(value, "orderedGridElementIds[" + ids.Count + "]", 128));',
        "var projectElements = ResolveProjectElements(project);",
    ]
    for token in required:
        if token not in renumber:
            print("ERROR: missing bounded Grid naming contract: " + token)
            return 1

    legacy = [
        ".Select((value, index) => Required(",
        ".ToList();",
        "if (ids.Count > MaxGridBatch)",
    ]
    for token in legacy:
        if token in renumber:
            print("ERROR: legacy post-materialization Grid naming capacity path returned: " + token)
            return 1

    loop = renumber.find("foreach (var value in orderedGridElementIds)")
    cap = renumber.find("if (ids.Count == MaxGridBatch)", loop)
    add = renumber.find("ids.Add(Required(value", loop)
    resolve = renumber.find("var projectElements = ResolveProjectElements(project);")
    if min(loop, cap, add, resolve) < 0 or not (loop < cap < add < resolve):
        print("ERROR: Grid naming capacity guard must execute inside enumeration before normalization/add and project resolution.")
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

    print("PASS: GridNamingService.Renumber bounds lazy input enumeration at the first item beyond the 2,000-item capacity, before project resolution or mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
