#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROJECT_FAMILY_SERVICE = ROOT / "src/QS3D.Core/Domain/ProjectFamilyService.cs"
BULK_EDIT_SERVICE = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing family relation assignment integrity file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


project_family_service = read(PROJECT_FAMILY_SERVICE)
bulk_edit_service = read(BULK_EDIT_SERVICE)
smoke = read(SMOKE)

for token in (
    "var canonicalFamilyId = RequireCanonicalFamilyId(familyId);",
    "var target = FindRequired(project, canonicalFamilyId);",
    "var previousFamilyId = RequireCanonicalExistingFamilyId(element);",
    "!string.Equals(value, value.Trim(), StringComparison.Ordinal)",
    "previous = project.FindFamily(previousFamilyId) ??",
    "references missing family id:",
    "Repair the relation before reassignment.",
):
    if token not in project_family_service:
        errors.append("ProjectFamilyService dangling/canonical-relation guard missing token: " + token)

if "var previousFamilyId = (element.FamilyId ?? string.Empty).Trim();" in project_family_service:
    errors.append("ProjectFamilyService.Assign must not silently trim a persisted previous Family identity.")

target_canonical = project_family_service.find("var canonicalFamilyId = RequireCanonicalFamilyId(familyId);")
target_lookup = project_family_service.find("var target = FindRequired(project, canonicalFamilyId);", target_canonical if target_canonical >= 0 else 0)
target_enumeration = project_family_service.find("var owned = ResolveOwnedElements(project, elements, target);", target_lookup if target_lookup >= 0 else 0)
if min(target_canonical, target_lookup, target_enumeration) < 0 or not target_canonical < target_lookup < target_enumeration:
    errors.append("ProjectFamilyService.Assign must reject a non-canonical target Family identity before target lookup or enumeration.")

canonical = project_family_service.find("var previousFamilyId = RequireCanonicalExistingFamilyId(element);")
lookup = project_family_service.find("previous = project.FindFamily(previousFamilyId) ??", canonical if canonical >= 0 else 0)
mutation = project_family_service.find("element.FamilyId = target.Id;")
if min(canonical, lookup, mutation) < 0 or not canonical < lookup < mutation:
    errors.append("ProjectFamilyService must reject a non-canonical previous Family identity and resolve dangling relations before the first FamilyId mutation.")

for token in (
    "var previousFamilyId = RequireCanonicalExistingFamilyId(element);",
    "!string.Equals(value, value.Trim(), StringComparison.Ordinal)",
    "previousFamily = project.FindFamily(previousFamilyId) ??",
    "references missing family id:",
    "Repair the relation before bulk reassignment.",
):
    if token not in bulk_edit_service:
        errors.append("BulkEditService dangling/canonical-relation guard missing token: " + token)

bulk_canonical = bulk_edit_service.find("var previousFamilyId = RequireCanonicalExistingFamilyId(element);")
bulk_lookup = bulk_edit_service.find("previousFamily = project.FindFamily(previousFamilyId) ??", bulk_canonical if bulk_canonical >= 0 else 0)
bulk_mutation = bulk_edit_service.find("element.FamilyId = family.Id;")
if min(bulk_canonical, bulk_lookup, bulk_mutation) < 0 or not bulk_canonical < bulk_lookup < bulk_mutation:
    errors.append("BulkEditService must reject a non-canonical previous Family identity and resolve dangling relations before the first FamilyId mutation.")

for token in (
    "DanglingPreviousFamilyBlocksWholeAssignmentBatch",
    "DanglingPreviousFamilyBlocksBulkEditBatch",
    "CanonicalCaseInsensitiveTargetAssignmentIsNoOp",
    "PaddedTargetFamilyIdFailsClosedBeforeEnumeration",
    "PaddedPersistedFamilyIdFailsClosedBeforeMutation",
    "CreateDanglingPreviousFamilyProject",
    "overwrote a dangling family reference instead of failing closed.",
    "Rejected padded persisted Family identity touched project persistence state.",
):
    if token not in smoke:
        errors.append("Family relation assignment smoke missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: project-aware family reassignment rejects non-canonical target/source family identities and dangling relations before lookup-enumeration or mutation boundaries.")
