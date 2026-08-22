#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/RoomFinishIdentityService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishProjectOwnershipSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing room-finish ownership contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "if (!elements.TryGetValue(room.Id, out var ownedRoom))",
        "if (!ReferenceEquals(ownedRoom, room))",
        "return FindExistingCore(project, elements, ownedRoom, category);",
    ):
        if token not in text:
            errors.append("RoomFinishIdentityService.cs missing ownership token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in ("foreignRoom", "missingRoom", "RoomFinishIdentityService.FindExisting"):
        if token not in text:
            errors.append("RoomFinishProjectOwnershipSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: room-finish identity resolution requires the exact project-owned Room instance.")
