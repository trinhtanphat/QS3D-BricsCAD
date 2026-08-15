#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFamilyActivationService.cs"
METADATA = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyActivationWhitespaceRepairSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectFamilyActivationWhitespaceRepairSmokeRegistration.cs"


def main():
    source = SOURCE.read_text(encoding="utf-8")
    metadata = METADATA.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        'if (!project.Metadata.TryGetValue("ActiveFamilyId", out var current)) return;',
        'if (!string.IsNullOrWhiteSpace(current) && project.FindFamily(current.Trim()) != null) return;',
        'project.Metadata.Remove("ActiveFamilyId");',
    ]
    required_metadata = [
        'public bool Remove(string key) => Remove(key, true);',
        'if (touchMutation) TouchProject();',
        'project.Touch();',
    ]
    required_smoke = [
        'MissingKeyIsNoOp();',
        'WhitespaceOnlyMetadataIsCleared();',
        'ValidPaddedIdentityIsPreserved();',
        'MissingNonBlankIdentityIsCleared();',
        'project.Metadata["ActiveFamilyId"] = "   \\t  ";',
        'ProjectFamilyActivationService.ClearIfMissing(project);',
        'ProjectFamilyActivationService.GetActive(project)',
    ]
    required_registration = [
        '[ModuleInitializer]',
        'ProjectFamilyActivationWhitespaceRepairSmoke.Run();',
    ]

    missing = ["source: " + token for token in required_source if token not in source]
    missing += ["metadata: " + token for token in required_metadata if token not in metadata]
    missing += ["smoke: " + token for token in required_smoke if token not in smoke]
    missing += ["registration: " + token for token in required_registration if token not in registration]
    if missing:
        print("ERROR: Active Family whitespace repair contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    unsafe = 'if (!project.Metadata.TryGetValue("ActiveFamilyId", out var current) || string.IsNullOrWhiteSpace(current)) return;'
    if unsafe in source:
        print("ERROR: whitespace-only ActiveFamilyId is still treated as a repair no-op.")
        return 1

    start = source.find("public static void ClearIfMissing(ProjectState project)")
    if start < 0:
        print("ERROR: cannot locate ClearIfMissing().")
        return 1
    body = source[start:source.find("    }\n}", start)]
    remove = body.find('project.Metadata.Remove("ActiveFamilyId");')
    if remove < 0:
        print("ERROR: repair must remove stale ActiveFamilyId metadata.")
        return 1
    if "project.Touch();" in body:
        print("ERROR: repair must not directly Touch before public metadata Remove; ProjectMetadataDictionary owns the exact-once revision boundary.")
        return 1

    print("PASS: ClearIfMissing removes whitespace-only/missing ActiveFamilyId metadata, preserves valid padded identities, and delegates exact-once revision advancement to ProjectMetadataDictionary.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
