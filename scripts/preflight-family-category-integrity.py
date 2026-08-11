#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
FAMILY_DEFINITION = ROOT / "src/QS3D.Core/Domain/FamilyDefinition.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing family category integrity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


project_state = read(PROJECT_STATE)
family_definition = read(FAMILY_DEFINITION)
smoke = read(SMOKE)

for token in (
    "_category = RequireCategory(category);",
    "var next = RequireCategory(value);",
    "Enum.IsDefined(typeof(ElementCategory), value)",
    "Family category must be a defined ElementCategory.",
):
    if token not in project_state:
        errors.append("ProjectFamily category guard missing token: " + token)

for token in (
    "private ElementCategory _category;",
    "Enum.IsDefined(typeof(ElementCategory), value)",
    "Family category must be a defined ElementCategory.",
):
    if token not in family_definition:
        errors.append("FamilyDefinition category guard missing token: " + token)

for token in (
    "UndefinedProjectFamilyCategoryFailsClosed",
    "UndefinedFamilyDefinitionCategoryFailsClosed",
    "Throws<ArgumentOutOfRangeException>",
    "Rejected ProjectFamily category assignment mutated the previous category.",
    "Rejected FamilyDefinition category assignment mutated the previous category.",
):
    if token not in smoke:
        errors.append("Family category smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: project-family category construction and assignment fail closed on undefined ElementCategory values before mutation.")
