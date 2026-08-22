#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthService.cs"
LEVEL = ROOT / "src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ModelHealthIdentityAmbiguitySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (HEALTH, LEVEL, SMOKE, REG):
    if not path.is_file():
        errors.append("missing diagnostic identity contract file: " + str(path.relative_to(ROOT)))

if HEALTH.is_file():
    text = HEALTH.read_text(encoding="utf-8")
    for token in (
        "private sealed class DiagnosticIdentityIndex",
        "DuplicateElementIds",
        "DuplicateFamilyIds",
        "DuplicateFloorIds",
        "DuplicateZoneIds",
        '"DUPLICATE_FAMILY_ID"',
        '"DUPLICATE_FLOOR_ID"',
        '"DUPLICATE_ZONE_ID"',
        '"AMBIGUOUS_FAMILY"',
        '"AMBIGUOUS_FLOOR"',
        '"AMBIGUOUS_ZONE"',
        '"AMBIGUOUS_HOST"',
        '"AMBIGUOUS_DEPENDENCY"',
        "ValidateHost(identity, element, issues)",
        "ValidateDependencies(identity, element, issues)",
        "HasMaterial(identity, element)",
    ):
        if token not in text:
            errors.append("ModelHealthService.cs missing diagnostic-safe token: " + token)
    for forbidden in (
        "project.FindElement(hostId",
        "project.FindElement(dependencyId",
        "project.FindFamily(element.FamilyId)",
    ):
        if forbidden in text:
            errors.append("ModelHealthService.cs uses throwing unique lookup while diagnosing ambiguous state: " + forbidden)

if LEVEL.is_file():
    text = LEVEL.read_text(encoding="utf-8")
    for token in (
        "duplicateFloorIds",
        '"DUPLICATE_LEVEL_ID"',
        '"BOTTOM_LEVEL_REFERENCE_AMBIGUOUS"',
        '"TOP_LEVEL_REFERENCE_AMBIGUOUS"',
    ):
        if token not in text:
            errors.append("LevelReferenceHealthService.cs missing ambiguity token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ComprehensiveHealthReportsAmbiguityWithoutThrowing",
        "LevelHealthReportsDuplicateLevelReferencesWithoutPendingQualification",
        'HasFor(issues, "AMBIGUOUS_HOST", "D")',
        'HasFor(issues, "AMBIGUOUS_DEPENDENCY", "D")',
        'HasFor(issues, "BOTTOM_LEVEL_REFERENCE_AMBIGUOUS", "D")',
    ):
        if token not in text:
            errors.append("ModelHealthIdentityAmbiguitySmoke.cs missing regression token: " + token)

if REG.is_file() and "ModelHealthIdentityAmbiguitySmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Model health identity ambiguity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core health uses diagnostic indexes for ambiguous identities, reports duplicate Family/Floor/Zone/Element references, and does not depend on throwing business lookups. No V25 files are inspected.")
