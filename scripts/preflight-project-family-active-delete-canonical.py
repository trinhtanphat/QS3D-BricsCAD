#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFamilyService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyActiveDeleteCanonicalSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyActiveDeleteCanonicalSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        'project.Metadata.TryGetValue("ActiveFamilyId", out var active)',
        'string.Equals((active ?? string.Empty).Trim(), family.Id, StringComparison.OrdinalIgnoreCase)',
        'throw new InvalidOperationException("Cannot delete the active Family. Activate another Family first.");',
    ]
    required_smoke = [
        'PaddedActiveIdBlocksDelete();',
        'CaseVariedPaddedActiveIdBlocksDelete();',
        'InactiveFamilyStillDeletes();',
        'project.Metadata["ActiveFamilyId"] = "  F-BEAM  ";',
        'project.Metadata["ActiveFamilyId"] = "  fAM-cOLUMN  ";',
        'ProjectFamilyService.Delete(project, inactive.Id)',
        'ProjectFamilyActivationService.GetActive(project)',
    ]
    required_registration = [
        '[ModuleInitializer]',
        'ProjectFamilyActiveDeleteCanonicalSmoke.Run();',
    ]

    missing = ["source: " + token for token in required_source if token not in source]
    missing += ["smoke: " + token for token in required_smoke if token not in smoke]
    missing += ["registration: " + token for token in required_registration if token not in registration]
    if missing:
        print("ERROR: active Family deletion canonical-id contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = 'string.Equals(active, family.Id, StringComparison.OrdinalIgnoreCase)'
    if unsafe in source:
        print("ERROR: raw ActiveFamilyId comparison returned to ProjectFamilyService.Delete().")
        return 1

    delete_start = source.find("public static bool Delete(ProjectState project, string familyId)")
    reference_start = source.find("public static int ReferenceCount", delete_start)
    if delete_start < 0 or reference_start < 0:
        print("ERROR: cannot isolate ProjectFamilyService.Delete().")
        return 1
    delete_body = source[delete_start:reference_start]
    if ".Trim()" not in delete_body or "project.Touch();" not in delete_body:
        print("ERROR: Delete() must canonicalize active identity before the existing mutation boundary.")
        return 1
    if delete_body.find(".Trim()") > delete_body.find("project.Touch();"):
        print("ERROR: ActiveFamilyId canonical guard must run before project mutation.")
        return 1

    print("PASS: active Family deletion uses the same trimmed, case-insensitive identity as activation reads and remains covered by module-registered smoke tests.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
