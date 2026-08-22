#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomOwnershipUtcSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing auto-room hardening contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'if (utcNow.Kind != DateTimeKind.Utc)',
        'room.Properties["BoundaryStaleUtc"] = utcNow.ToString("O");',
        'var ownedRoom = project.FindElement(room.Id)',
        'if (!ReferenceEquals(ownedRoom, room))',
        'var ownedFamily = project.FindFamily(family.Id)',
        'if (!ReferenceEquals(ownedFamily, family))',
    ):
        if token not in text:
            errors.append("AutoRoomLifecycle.cs missing fail-closed token: " + token)
    if 'utcNow.ToUniversalTime().ToString("O")' in text:
        errors.append("AutoRoomLifecycle must not reinterpret utcNow according to machine timezone.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsSpoofedRoomBeforeMutation();",
        "RejectsSpoofedFamilyBeforeMutation();",
        "RejectsNonUtcStaleTimestampBeforeMutation();",
        "DateTimeKind.Unspecified",
    ):
        if token not in text:
            errors.append("AutoRoomOwnershipUtcSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: AutoRoom Core lifecycle rejects foreign room/family instances and machine-dependent non-UTC stale timestamps before mutation.")
