#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectElementCategoryIntegritySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing element category integrity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "private ElementCategory _category;",
    "_category = RequireCategory(category);",
    "var next = RequireCategory(value);",
    "Enum.IsDefined(typeof(ElementCategory), value)",
    "Element category must be a defined ElementCategory.",
):
    if token not in source:
        errors.append("ProjectElement category guard missing token: " + token)

for token in (
    "ConstructorRejectsUndefinedCategory",
    "SetterRejectsUndefinedCategoryWithoutMutation",
    "Throws<ArgumentOutOfRangeException>",
    "Rejected ProjectElement category assignment mutated the previous valid category.",
):
    if token not in smoke:
        errors.append("ProjectElement category smoke missing regression token: " + token)

if "ProjectElementCategoryIntegritySmoke.Run();" not in registration:
    errors.append("ProjectElement category smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectElement construction and assignment fail closed on undefined ElementCategory values before mutation.")
