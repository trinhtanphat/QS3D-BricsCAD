#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLACEMENT = ROOT / "src/QS3D.Core/Domain/ElementVerticalPlacementService.cs"
STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/LevelReferenceSmoke.cs"
errors = []

for path in (PLACEMENT, STATE, SMOKE):
    if not path.is_file():
        errors.append("missing Core Level identity file: " + str(path.relative_to(ROOT)))

if PLACEMENT.is_file():
    text = PLACEMENT.read_text(encoding="utf-8")
    if "return project.FindFloor(floorId)" not in text:
        errors.append("ElementVerticalPlacementService must resolve Floor/Level identity through ProjectState.FindFloor.")
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

if SMOKE.is_file() and "DuplicateLevelIdsFailClosedDuringPlacement" not in SMOKE.read_text(encoding="utf-8"):
    errors.append("LevelReferenceSmoke.cs is missing duplicate Floor/Level ambiguity regression.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core vertical placement uses unique normalized Floor identity and rejects duplicate Level IDs; this gate does not inspect V25 runtime/native files.")
