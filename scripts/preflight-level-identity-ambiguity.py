#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLACEMENT = ROOT / "src/QS3D.Core/Domain/ElementVerticalPlacementService.cs"
STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/LevelReferenceSmoke.cs"
AMBIGUITY_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ModelHealthIdentityAmbiguitySmoke.cs"
errors = []

for path in (PLACEMENT, STATE, HEALTH, SMOKE, AMBIGUITY_SMOKE):
    if not path.is_file():
        errors.append("missing Core Level identity file: " + str(path.relative_to(ROOT)))

if PLACEMENT.is_file():
    text = PLACEMENT.read_text(encoding="utf-8")
    uses_project_unique_lookup = "return project.FindFloor(floorId)" in text
    captured_generation_tokens = (
        "CaptureFloorGeneration(project)",
        "FindCapturedFloor(",
        "new Dictionary<string, double>(count, StringComparer.OrdinalIgnoreCase)",
        "if (floors.ContainsKey(floor.Id))",
        "project.ChangeVersion",
        "project.Floors.Count",
    )
    uses_captured_unique_lookup = all(token in text for token in captured_generation_tokens)
    if not uses_project_unique_lookup and not uses_captured_unique_lookup:
        errors.append(
            "ElementVerticalPlacementService must resolve Floor/Level identity through either ProjectState.FindFloor or a fenced case-insensitive captured floor generation."
        )
    if "project.Floors.FirstOrDefault" in text:
        errors.append("ElementVerticalPlacementService still uses first-match Floor lookup and can hide duplicate IDs.")

if STATE.is_file():
    text = STATE.read_text(encoding="utf-8")
    for token in (
        'FindUnique(Floors, NormalizeLookupId(id), x => x.Id, "floor")',
        "if (match != null) throw new InvalidOperationException",
    ):
        if token not in text:
            errors.append("ProjectState.cs missing unique Floor lookup token: " + token)

if HEALTH.is_file():
    text = HEALTH.read_text(encoding="utf-8")
    for token in (
        "var duplicateFloorIds = new HashSet<string>",
        '"DUPLICATE_LEVEL_ID"',
        '"BOTTOM_LEVEL_REFERENCE_AMBIGUOUS"',
        '"TOP_LEVEL_REFERENCE_AMBIGUOUS"',
    ):
        if token not in text:
            errors.append("LevelReferenceHealthService.cs missing duplicate Level diagnostic token: " + token)

if SMOKE.is_file() and "DuplicateLevelIdsFailClosedDuringPlacement" not in SMOKE.read_text(encoding="utf-8"):
    errors.append("LevelReferenceSmoke.cs is missing duplicate Floor/Level ambiguity regression.")

if AMBIGUITY_SMOKE.is_file():
    text = AMBIGUITY_SMOKE.read_text(encoding="utf-8")
    for token in (
        "LevelHealthReportsDuplicateLevelReferencesWithoutPendingQualification",
        '"BOTTOM_LEVEL_REFERENCE_AMBIGUOUS"',
        '"TOP_LEVEL_REFERENCE_AMBIGUOUS"',
    ):
        if token not in text:
            errors.append("ModelHealthIdentityAmbiguitySmoke.cs missing Level ambiguity regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core vertical placement and Level health reject duplicate Floor identity consistently; this source-only gate does not inspect V25 runtime/native files.")
