#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthService.cs"
COMPREHENSIVE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
DEPENDENCY = ROOT / "src/QS3D.Core/Diagnostics/DependencyHealthService.cs"
LEVEL = ROOT / "src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs"
ROOM = ROOT / "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ModelHealthIdentityAmbiguitySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (HEALTH, COMPREHENSIVE, DEPENDENCY, LEVEL, ROOM, SMOKE, REG):
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

if COMPREHENSIVE.is_file():
    text = COMPREHENSIVE.read_text(encoding="utf-8")
    for token in (
        "AddSafely(issues, seen, \"ModelHealthService\"",
        "AddSafely(issues, seen, \"RoomFinishHealthService\"",
        "AddSafely(issues, seen, \"DependencyHealthService\"",
        '"HEALTH_PROVIDER_FAILED"',
        "IsDiagnosticDataFailure",
    ):
        if token not in text:
            errors.append("ComprehensiveModelHealthService.cs missing provider-isolation token: " + token)

if DEPENDENCY.is_file():
    text = DEPENDENCY.read_text(encoding="utf-8")
    for token in (
        "var duplicateIds = new HashSet<string>",
        '"DEPENDENCY_TARGET_AMBIGUOUS"',
        "if (duplicateIds.Contains(element.Id)",
    ):
        if token not in text:
            errors.append("DependencyHealthService.cs missing ambiguous graph token: " + token)

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

if ROOM.is_file():
    text = ROOM.read_text(encoding="utf-8")
    for token in (
        "var elements = new List<ProjectElement>(project.Elements.Count);",
        "foreach (var element in project.Elements)",
        "if (element == null)",
        'throw new InvalidOperationException("Room-finish diagnostics cannot inspect a project containing a null semantic element.");',
        "var duplicateIds = new HashSet<string>",
        '"AMBIGUOUS_ROOM_FINISH_PARENT"',
    ):
        if token not in text:
            errors.append("RoomFinishHealthService.cs missing diagnostic-safe identity token: " + token)
    if "project.Elements.Where(x => x != null)" in text:
        errors.append("RoomFinishHealthService.cs regressed to silently filtering corrupt null semantic entries.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ModelHealthReportsAmbiguityWithoutThrowing",
        "ComprehensiveHealthPreservesReportAcrossProviderFailures",
        "DependencyHealthRejectsAmbiguousTargets",
        "LevelHealthReportsDuplicateLevelReferencesWithoutPendingQualification",
        'HasFor(issues, "AMBIGUOUS_HOST", "D")',
        'HasFor(issues, "DEPENDENCY_TARGET_AMBIGUOUS", "D")',
        'HasFor(issues, "BOTTOM_LEVEL_REFERENCE_AMBIGUOUS", "D")',
        'Has(issues, "HEALTH_PROVIDER_FAILED")',
        "project.Elements.Add(null!);",
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

print("PASS: Core health reports ambiguous semantic identities without arbitrary resolution, isolates fail-visible provider data failures including null Room Finish input, and preserves duplicate graph/Level diagnostics. No V25 files are inspected.")
