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
        "var canonicalFamilyId = RequireCanonicalFamilyId(familyId);",
        "var target = FindRequired(project, canonicalFamilyId);",
        "var owned = ResolveOwnedElements(project, elements, target);",
        "var pending = new List<PendingFamilyAssignment>();",
        "var previousFamilyId = RequireCanonicalExistingFamilyId(element);",
        "!string.Equals(value, value.Trim(), StringComparison.Ordinal)",
        "project.FindFamily(previousFamilyId)",
        "foreach (var item in pending)",
        "ResolveFamilyMembers(project, family.Id)",
    ):
        if token not in text:
            errors.append("ProjectFamilyService.cs missing whole-batch/canonical preflight token: " + token)
    if "var previousFamilyId = (element.FamilyId ?? string.Empty).Trim();" in text:
        errors.append("ProjectFamilyService.Assign must not normalize a persisted previous Family identity by trimming it.")
    target_canonical = text.find("var canonicalFamilyId = RequireCanonicalFamilyId(familyId);")
    target_lookup = text.find("var target = FindRequired(project, canonicalFamilyId);", target_canonical if target_canonical >= 0 else 0)
    enumeration = text.find("var owned = ResolveOwnedElements(project, elements, target);", target_lookup if target_lookup >= 0 else 0)
    if min(target_canonical, target_lookup, enumeration) < 0 or not target_canonical < target_lookup < enumeration:
        errors.append("ProjectFamilyService.Assign must validate canonical target Family identity before lookup and target enumeration.")
    canonical = text.find("var previousFamilyId = RequireCanonicalExistingFamilyId(element);")
    lookup = text.find("project.FindFamily(previousFamilyId)", canonical if canonical >= 0 else 0)
    mutation = text.find("element.FamilyId = target.Id;")
    if min(canonical, lookup, mutation) < 0 or not canonical < lookup < mutation:
        errors.append("ProjectFamilyService.Assign must reject non-canonical and resolve previous Family identities before the first FamilyId mutation.")

if BULK.is_file():
    text = BULK.read_text(encoding="utf-8")
    for token in (
        "var pending = new List<PendingFamilyAssignment>();",
        "var previousFamilyId = RequireCanonicalExistingFamilyId(element);",
        "!string.Equals(value, value.Trim(), StringComparison.Ordinal)",
        "project.FindFamily(previousFamilyId)",
        "pending.Add(new PendingFamilyAssignment",
        "foreach (var item in pending)",
    ):
        if token not in text:
            errors.append("BulkEditService.cs missing canonical Family assignment preflight token: " + token)
    canonical = text.find("var previousFamilyId = RequireCanonicalExistingFamilyId(element);")
    lookup = text.find("project.FindFamily(previousFamilyId)", canonical if canonical >= 0 else 0)
    mutation = text.find("element.FamilyId = family.Id;")
    if min(canonical, lookup, mutation) < 0 or not canonical < lookup < mutation:
        errors.append("BulkEditService.AssignFamily must reject non-canonical and resolve previous Family identities before the first FamilyId mutation.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicatePreviousFamilyBlocksWholeAssignmentBatch",
        "DuplicatePreviousFamilyBlocksBulkEditBatch",
        "CanonicalCaseInsensitiveTargetAssignmentIsNoOp",
        "PaddedTargetFamilyIdFailsClosedBeforeEnumeration",
        "PaddedPersistedFamilyIdFailsClosedBeforeMutation",
        "CorruptProjectElementListBlocksPropertyPropagationBeforeMutation",
        "setup.Project.UpdatedUtc != beforeUpdated",
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

print("PASS: ProjectFamilyService validates canonical target/source Family identities and resolves the whole batch before mutation; BulkEditService preserves the same fail-closed source-identity boundary.")
