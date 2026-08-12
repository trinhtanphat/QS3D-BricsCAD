#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFamilyService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyMemberCanonicalReferenceSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyMemberCanonicalReferenceSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        'private static IReadOnlyList<ProjectElement> ResolveFamilyMembers(ProjectState project, string familyId)',
        'string.Equals((element.FamilyId ?? string.Empty).Trim(), familyId, StringComparison.OrdinalIgnoreCase)',
    ]
    required_smoke = [
        'PaddedRelationCountsAndBlocksDelete();',
        'CaseVariedPaddedRelationCounts();',
        'UnrelatedFamilyRemainsUnreferenced();',
        'FamilyId = "  F-BEAM  "',
        'FamilyId = "  fAM-cOLUMN  "',
        'ProjectFamilyService.ReferenceCount(project, family.Id)',
        'ProjectFamilyService.Delete(project, family.Id)',
    ]
    required_registration = [
        '[ModuleInitializer]',
        'ProjectFamilyMemberCanonicalReferenceSmoke.Run();',
    ]

    missing = ["source: " + token for token in required_source if token not in source]
    missing += ["smoke: " + token for token in required_smoke if token not in smoke]
    missing += ["registration: " + token for token in required_registration if token not in registration]
    if missing:
        print("ERROR: Family member canonical-reference contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = 'string.Equals(element.FamilyId, familyId, StringComparison.OrdinalIgnoreCase)'
    if unsafe in source:
        print("ERROR: raw ProjectElement.FamilyId comparison returned to ResolveFamilyMembers().")
        return 1

    start = source.find("private static IReadOnlyList<ProjectElement> ResolveFamilyMembers")
    end = source.find("private static IReadOnlyList<ProjectElement> ResolveOwnedElements", start)
    if start < 0 or end < 0:
        print("ERROR: cannot isolate ResolveFamilyMembers().")
        return 1
    body = source[start:end]
    if ".Trim()" not in body or "StringComparison.OrdinalIgnoreCase" not in body:
        print("ERROR: ResolveFamilyMembers() must use trimmed case-insensitive Family identity.")
        return 1

    print("PASS: Family member resolution uses canonical relation identity for reference count, propagation and delete safety, with module-registered regression coverage.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
