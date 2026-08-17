#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkCanonicalizationSmoke.cs"
RELATIONSHIP_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkCanonicalRelationshipSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SERVICE, SMOKE, RELATIONSHIP_SMOKE, REG):
    if not path.is_file():
        errors.append("missing host-link canonicalization file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'var previousHostRaw = hasPreviousHost ? previous ?? string.Empty : string.Empty;',
        'ValidateCanonicalPersistedHostId(previousHostRaw, opening.Id);',
        'ValidateCanonicalPersistedHostId(hostIdRaw, opening.Id);',
        'if (!string.Equals(rawHostId, rawHostId.Trim(), StringComparison.Ordinal))',
        "RemoveDependencies(opening, previousHost);",
        "RemoveDependencies(opening, wall.Id);",
        "opening.DependsOn.Add(wall.Id);",
        "var dependencyRemoved = RemoveDependencies(opening, hostId) > 0;",
        "DependencyMatches(opening.DependsOn[i], hostId)",
        "var candidateRaw = candidate ?? string.Empty;",
        "var expectedRaw = expected ?? string.Empty;",
        "var candidateTrimmed = candidateRaw.Trim();",
        "var expectedTrimmed = expectedRaw.Trim();",
        "if (!string.Equals(candidateTrimmed, expectedTrimmed, StringComparison.OrdinalIgnoreCase)) return false;",
        "if (!string.Equals(candidateRaw, candidateTrimmed, StringComparison.Ordinal))",
        "return string.Equals(candidateRaw, expectedRaw, StringComparison.OrdinalIgnoreCase);",
    ):
        if token not in text:
            errors.append("HostLinkService.cs missing fail-closed relationship token: " + token)

    for forbidden in (
        "(candidate ?? string.Empty).Trim()",
        "(expected ?? string.Empty).Trim()",
    ):
        if forbidden in text:
            errors.append("HostLinkService.cs still silently canonicalizes relationship identity: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RelinkRejectsLegacyDependencyVariants();",
        "RehostRejectsLegacyPreviousHostVariants();",
        "UnlinkRejectsLegacyHostVariants();",
        "CanonicalRelinkIsSideEffectFree();",
        "BlankHostMetadataUnlinkFailsBeforeMutation();",
        "AuditedHostMutationsAdvanceRevisionOnce();",
    ):
        if token not in text:
            errors.append("HostLinkCanonicalizationSmoke.cs missing fail-closed regression token: " + token)

if RELATIONSHIP_SMOKE.is_file():
    text = RELATIONSHIP_SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedPersistedHostOnUnlinkWithoutMutation();",
        "RejectsPaddedPersistedHostOnRelinkWithoutMutation();",
        "RejectsPaddedMatchingDependencyWithoutMutation();",
        "PreservesCanonicalRelationshipBehavior();",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("HostLinkCanonicalRelationshipSmoke.cs missing regression token: " + token)

if REG.is_file() and "HostLinkCanonicalizationSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("host-link canonicalization smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] host relationship IDs are statically guarded for fail-closed persisted canonicality, canonical no-op/link/unlink behavior and mutation atomicity")
