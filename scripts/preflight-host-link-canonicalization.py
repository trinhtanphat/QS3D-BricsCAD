#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkCanonicalizationSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SERVICE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing host-link canonicalization file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        '(previous ?? string.Empty).Trim()',
        '(value ?? string.Empty).Trim()',
        "RemoveDependencies(opening, previousHost);",
        "RemoveDependencies(opening, wall.Id);",
        "opening.DependsOn.Add(wall.Id);",
        "var dependencyRemoved = RemoveDependencies(opening, hostId) > 0;",
        "DependencyMatches(opening.DependsOn[i], hostId)",
        "(candidate ?? string.Empty).Trim()",
        "(expected ?? string.Empty).Trim()",
    ):
        if token not in text:
            errors.append("HostLinkService.cs missing canonical dependency token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RelinkCollapsesLegacyDependencyVariants();",
        "RehostRemovesLegacyPreviousHostVariants();",
        "UnlinkRemovesLegacyHostVariants();",
        'opening.DependsOn.Add(" WALL-A ");',
        "Equal(1, opening.DependsOn.Count);",
    ):
        if token not in text:
            errors.append("HostLinkCanonicalizationSmoke.cs missing regression token: " + token)

if REG.is_file() and "HostLinkCanonicalizationSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("host-link canonicalization smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] opening host properties/dependencies are statically guarded for trimmed canonical IDs, duplicate collapse, re-host cleanup and unlink cleanup")
