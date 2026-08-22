#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectBrowserQueryPlannerSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing Project Browser family/category integrity contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("private static void ValidateElementReferences(")
    if start < 0:
        errors.append("cannot isolate ProjectBrowserQueryPlanner.ValidateElementReferences")
    else:
        body = source[start:]
        for token in (
            "if (!families.TryGetValue(familyId, out var family))",
            "if (family.Category != element.Category)",
            "Project browser found family/category mismatch on element",
            "if (floorId.Length > 0 && !floors.ContainsKey(floorId))",
            "if (zoneId.Length > 0 && !zones.ContainsKey(zoneId))",
        ):
            if token not in body:
                errors.append("browser query reference validation missing contract: " + token)
        lookup = body.find("if (!families.TryGetValue(familyId, out var family))")
        category = body.find("if (family.Category != element.Category)")
        floor = body.find("var floorId =")
        if min(lookup, category, floor) < 0 or not (lookup < category < floor):
            errors.append("Family existence/category validation must finish before Floor/Zone reference validation")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "FamilyCategoryMismatchFailsClosed();",
        "private static void FamilyCategoryMismatchFailsClosed()",
        'new ProjectElement("BAD-FAMILY-CATEGORY", ElementCategory.Beam, "FAM-C"',
        "dirtyOnly: true, categories: new[] { ElementCategory.Column }",
        "Filtered browser query must reject Family/category corruption even when the corrupt element would not match the filter.",
    ):
        if token not in smoke:
            errors.append("browser query smoke missing Family/category regression: " + token)

print("QS3D Project Browser family/category integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: filtered Project Browser queries validate Family existence and category integrity for the complete semantic element set before filtering/search results are produced.")
