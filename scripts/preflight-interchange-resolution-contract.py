#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLANNER = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeImportResolutionPlanner.cs"
VALIDATOR = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeJsonValidator.cs"
USE_SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "ProjectInterchangeUseSourceSemanticImporter.cs"

errors = []

for path in (PLANNER, VALIDATOR, USE_SOURCE):
    if not path.is_file():
        errors.append("missing source file: " + str(path.relative_to(ROOT)))

if not errors:
    planner = PLANNER.read_text(encoding="utf-8")
    validator = VALIDATOR.read_text(encoding="utf-8")
    use_source = USE_SOURCE.read_text(encoding="utf-8")

    linked_plan_guard = "private const int MaxPlanItems = ProjectInterchangeJsonValidator.MaxCollectionItems;"
    validator_guard = "public const int MaxCollectionItems = 250000;"
    if linked_plan_guard not in planner:
        errors.append("resolution planner must derive MaxPlanItems from ProjectInterchangeJsonValidator.MaxCollectionItems")
    if validator_guard not in validator:
        errors.append("snapshot validator MaxCollectionItems contract changed; review planner capacity coupling explicitly")
    if "private const int MaxPlanItems = 50000;" in planner:
        errors.append("resolution planner reintroduced the stale 50,000 identity cap")

    start = use_source.find("private static void ApplySourceFamilyProperties(")
    end = use_source.find("private static void ApplySourceElementSemanticData(", start)
    if start < 0 or end < 0 or end <= start:
        errors.append("cannot locate ApplySourceFamilyProperties contract block")
    else:
        block = use_source[start:end]
        required = (
            "ProjectFamilyService.RemoveProperty(target, family.Id, key)",
            "ProjectFamilyService.SetProperty(target, family.Id, property.Key, property.Value ?? string.Empty)",
        )
        for token in required:
            if token not in block:
                errors.append("UseSource Family apply must preserve canonical inheritance path: " + token)
        forbidden = (
            "family.Properties.Clear()",
            "family.Properties[property.Key] =",
            "family.Properties[key] =",
        )
        for token in forbidden:
            if token in block:
                errors.append("UseSource Family apply bypasses ProjectFamilyService inheritance semantics: " + token)

    family_loop_start = use_source.find("foreach (var familySnapshot in source.Families")
    family_loop_end = use_source.find("foreach (var elementSnapshot in source.Elements", family_loop_start)
    if family_loop_start < 0 or family_loop_end < 0 or family_loop_end <= family_loop_start:
        errors.append("cannot locate UseSource Family replacement loop")
    else:
        family_loop = use_source[family_loop_start:family_loop_end]
        if "ProjectFamilyService.Rename(target, familySnapshot.Id, familySnapshot.Name)" not in family_loop:
            errors.append("UseSource Family replacement must use ProjectFamilyService.Rename")

if errors:
    print("QS3D interchange resolution contract preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: interchange resolution capacity follows the validated snapshot guard and UseSource Family replacement preserves canonical rename/property inheritance semantics.")
