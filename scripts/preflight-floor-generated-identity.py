#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IDENTITY = ROOT / "src/QS3D.Core/Domain/FloorGeneratedIdentityPlanner.cs"
FLOORS = ROOT / "src/QS3D.Core/Domain/ProjectFloorService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/FloorGeneratedIdentitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/FloorGeneratedIdentitySmokeRegistration.cs"
DOC = ROOT / "docs/LEVEL-GENERATED-OWNERSHIP.md"
errors = []

for path in (IDENTITY, FLOORS, SMOKE, REGISTRATION, DOC):
    if not path.is_file():
        errors.append("missing Floor generated identity contract file: " + str(path.relative_to(ROOT)))

if IDENTITY.is_file():
    text = IDENTITY.read_text(encoding="utf-8")
    for token in (
        'public sealed class FloorGeneratedIdentity',
        'public static class FloorGeneratedIdentityPlanner',
        'MaxFloorIdLength = 64',
        'MaxFloorNameLength = 120',
        'OwnerTokenPrefix = "LVO1:"',
        'StateTokenPrefix = "LVS1:"',
        'normalized.ToUpperInvariant()',
        'elevation.ToString("R", CultureInfo.InvariantCulture)',
        'SHA256.Create()',
        'public static string BuildOwnerToken(string floorId)',
    ):
        if token not in text:
            errors.append("FloorGeneratedIdentityPlanner.cs missing deterministic ownership token: " + token)

    for forbidden in (
        'ProjectElement',
        'ElementCategory',
        'GeneratedGeometryService',
        'ObjectId',
        'Handle',
    ):
        if forbidden in text:
            errors.append("Floor generated identity must remain independent from single-element/native ownership: " + forbidden)

if FLOORS.is_file():
    text = FLOORS.read_text(encoding="utf-8")
    for token in (
        'Required(id, nameof(id), 64)',
        'MaxNameLength = 120',
        'FloorDefinition Create(',
        'FloorDefinition Update(',
    ):
        if token not in text:
            errors.append("ProjectFloorService.cs no longer matches Floor identity normalization contract: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'OwnerIsStableAcrossNameAndElevationChanges',
        'CaseAndWhitespaceCanonicalizeOwner',
        'StateIsDeterministic',
        'InvalidLegacyLengthsFailClosed',
        'TokensStayCompact',
    ):
        if token not in text:
            errors.append("FloorGeneratedIdentitySmoke.cs missing scenario: " + token)

if REGISTRATION.is_file():
    text = REGISTRATION.read_text(encoding="utf-8")
    if '[ModuleInitializer]' not in text or 'FloorGeneratedIdentitySmoke.Run()' not in text:
        errors.append("Floor generated identity smoke is not module-registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'FloorGeneratedIdentityPlanner',
        'LVO1:',
        'LVS1:',
        'owner token stays stable',
        'state token changes',
        'FloorDefinition',
        'not a `ProjectElement`',
        'LOCAL_ONLY',
    ):
        if token not in text:
            errors.append("LEVEL-GENERATED-OWNERSHIP.md missing Floor owner boundary: " + token)

print("QS3D Floor generated ownership identity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Floor/Level generated ownership has a compact stable owner token and separate name/elevation state token without pretending FloorDefinition is a ProjectElement; native symbols remain LOCAL_ONLY.")
