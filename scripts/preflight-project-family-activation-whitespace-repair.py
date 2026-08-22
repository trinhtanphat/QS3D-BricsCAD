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
        'private bool Remove(string key, bool touchMutation)',
        'if (touchMutation) TouchProject();',
        'return _items.Remove(key);',
        'private void TouchProject()',
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
        'Equal(beforeVersion + 1L, project.ChangeVersion);',
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
    lookup = body.find('if (!project.Metadata.TryGetValue("ActiveFamilyId", out var current)) return;')
    preserve = body.find('if (!string.IsNullOrWhiteSpace(current) && project.FindFamily(current.Trim()) != null) return;')
    remove = body.find('project.Metadata.Remove("ActiveFamilyId");')
    if min(lookup, preserve, remove) < 0 or not lookup < preserve < remove:
        print("ERROR: repair must reject missing metadata, preserve valid padded identity, then remove stale/whitespace metadata.")
        return 1

    private_remove = metadata.find("private bool Remove(string key, bool touchMutation)")
    touch = metadata.find("if (touchMutation) TouchProject();", private_remove)
    storage_remove = metadata.find("return _items.Remove(key);", private_remove)
    if min(private_remove, touch, storage_remove) < 0 or not private_remove < touch < storage_remove:
        print("ERROR: public metadata Remove must retain exact-once project revision ownership before storage mutation.")
        return 1

    print("PASS: ClearIfMissing repairs whitespace/missing ActiveFamilyId through revision-owning metadata Remove, preserves valid padded identities, and remains module-smoke guarded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
