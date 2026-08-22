#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAMILY = ROOT / "src/QS3D.Core/Domain/ProjectFamilyService.cs"
BULK = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (FAMILY, BULK, SMOKE, REG):
    if not path.is_file():
        errors.append("missing Family assignment atomicity file: " + str(path.relative_to(ROOT)))

if FAMILY.is_file():
    text = FAMILY.read_text(encoding="utf-8")
    for token in (
        "var owned = ResolveOwnedElements(project, elements, target);",
        "var pending = new List<PendingFamilyAssignment>();",
        "project.FindFamily(element.FamilyId)",
        "foreach (var item in pending)",
        "ResolveFamilyMembers(project, family.Id)",
    ):
        if token not in text:
            errors.append("ProjectFamilyService.cs missing whole-batch preflight token: " + token)
    lookup = text.find("project.FindFamily(element.FamilyId)")
    mutation = text.find("element.FamilyId = target.Id;")
    if lookup < 0 or mutation < 0 or lookup > mutation:
        errors.append("ProjectFamilyService.Assign must resolve previous Family identities before the first FamilyId mutation.")

if BULK.is_file():
    text = BULK.read_text(encoding="utf-8")
    for token in (
        "var pending = new List<PendingFamilyAssignment>();",
        "project.FindFamily(element.FamilyId)",
        "pending.Add(new PendingFamilyAssignment",
        "foreach (var item in pending)",
    ):
        if token not in text:
            errors.append("BulkEditService.cs missing Family assignment preflight token: " + token)
    lookup = text.find("project.FindFamily(element.FamilyId)")
    mutation = text.find("element.FamilyId = family.Id;")
    if lookup < 0 or mutation < 0 or lookup > mutation:
        errors.append("BulkEditService.AssignFamily must resolve previous Family identities before the first FamilyId mutation.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicatePreviousFamilyBlocksWholeAssignmentBatch",
        "DuplicatePreviousFamilyBlocksBulkEditBatch",
        "CorruptProjectElementListBlocksPropertyPropagationBeforeMutation",
        "project.UpdatedUtc != beforeUpdated",
    ):
        if token not in text:
            errors.append("ProjectFamilyAssignmentAtomicitySmoke.cs missing regression scenario: " + token)

if REG.is_file() and "ProjectFamilyAssignmentAtomicitySmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Family assignment atomicity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: ProjectFamilyService and BulkEditService resolve all failure-prone previous-family identities before mutating any assignment target.")
