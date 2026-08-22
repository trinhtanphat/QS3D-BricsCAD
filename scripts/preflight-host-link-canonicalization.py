#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkCanonicalizationSmoke.cs"
FOCUSED_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkCanonicalRelationshipSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SERVICE, SMOKE, FOCUSED_SMOKE, REG):
    if not path.is_file():
        errors.append("missing host-link canonicalization file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'var previousHostRaw = hasPreviousHost ? previous ?? string.Empty : string.Empty;',
        'ValidateCanonicalPersistedHostId(previousHostRaw, opening.Id);',
        'var hostIdRaw = value ?? string.Empty;',
        'ValidateCanonicalPersistedHostId(hostIdRaw, opening.Id);',
        'if (!string.Equals(rawHostId, rawHostId.Trim(), StringComparison.Ordinal))',
        "RemoveDependencies(opening, previousHost);",
        "RemoveDependencies(opening, wall.Id);",
        "opening.DependsOn.Add(wall.Id);",
        "var dependencyRemoved = RemoveDependencies(opening, hostId) > 0;",
        "DependencyMatches(opening.DependsOn[i], hostId)",
        "var candidateTrimmed = candidateRaw.Trim();",
        "if (!string.Equals(candidateRaw, candidateTrimmed, StringComparison.Ordinal))",
    ):
        if token not in text:
            errors.append("HostLinkService.cs missing fail-closed host identity token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NonCanonicalRelinkFailsBeforeMutation();",
        "NonCanonicalRehostFailsBeforeMutation();",
        "NonCanonicalUnlinkFailsBeforeMutation();",
        'opening.Properties["HostWallId"] = " wall-a ";',
        "Throws<InvalidOperationException>",
        "CanonicalRelinkIsSideEffectFree();",
    ):
        if token not in text:
            errors.append("HostLinkCanonicalizationSmoke.cs missing fail-closed regression token: " + token)

if FOCUSED_SMOKE.is_file():
    text = FOCUSED_SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedPersistedHostOnUnlinkWithoutMutation();",
        "RejectsPaddedPersistedHostOnRelinkWithoutMutation();",
        "RejectsPaddedMatchingDependencyWithoutMutation();",
        "PreservesCanonicalRelationshipBehavior();",
    ):
        if token not in text:
            errors.append("HostLinkCanonicalRelationshipSmoke.cs missing focused regression token: " + token)

if REG.is_file() and "HostLinkCanonicalizationSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("host-link canonicalization smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] opening host properties/dependencies fail closed on non-canonical relationship IDs while canonical link/unlink behavior remains guarded")
