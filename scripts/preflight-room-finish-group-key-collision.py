#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/RoomFinishSchedule.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishScheduleGroupKeyCollisionSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishScheduleGroupKeyCollisionRegistration.cs"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing room-finish group-key regression file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
smoke = read(SMOKE)
registration = read(REGISTRATION)

for token in (
    "using System.Text;",
    "new Dictionary<string, RoomFinishScheduleRow>(StringComparer.OrdinalIgnoreCase)",
    "var key = GroupKey(",
    "floorId,",
    "roomKey,",
    "element.Category.ToString(),",
    "familyId,",
    "material,",
    "unitHint);",
    "private static string GroupKey(params string[] tokens)",
    "var key = new StringBuilder();",
    "foreach (var raw in tokens)",
    "var token = raw ?? string.Empty;",
    "token.Length.ToString(CultureInfo.InvariantCulture)",
    ".Append(':')",
    ".Append(token)",
):
    if token not in source:
        errors.append("RoomFinishSchedule collision-free source contract missing token: " + token)

if 'string.Join("\\u001f"' in source:
    errors.append("RoomFinishSchedule must not return to delimiter-only U+001F composite grouping keys.")

build_start = source.find("public static IReadOnlyList<RoomFinishScheduleRow> Build(ProjectState project)")
helper_start = source.find("private static string GroupKey(params string[] tokens)", build_start)
if build_start < 0 or helper_start <= build_start:
    errors.append("cannot isolate RoomFinishSchedule Build/GroupKey ordering")
else:
    build = source[build_start:helper_start]
    key_start = build.find("var key = GroupKey(")
    if key_start < 0:
        errors.append("RoomFinishSchedule Build must construct grouped identity through GroupKey")
    else:
        key_end = build.find("unitHint);", key_start)
        if key_end < 0:
            errors.append("RoomFinishSchedule GroupKey call must include unitHint as the final grouping token")
        else:
            key_call = build[key_start:key_end]
            ordered = [
                "floorId",
                "roomKey",
                "element.Category.ToString()",
                "familyId",
                "material",
            ]
            cursor = -1
            for token in ordered:
                next_pos = key_call.find(token, cursor + 1)
                if next_pos < 0 or next_pos <= cursor:
                    errors.append("RoomFinishSchedule GroupKey tokens are missing or reordered at: " + token)
                    break
                cursor = next_pos

for token in (
    'const string separator = "|";',
    'new FloorDefinition("A" + separator + "B", "Floor AB", 0d)',
    'new FloorDefinition("A", "Floor A", 3d)',
    'LegacyDelimitedKey(separator, "A" + separator + "B", "C", "WallFinish", "wf", "Paint", "m\\u00b2")',
    'LegacyDelimitedKey(separator, "A", "B" + separator + "C", "WallFinish", "wf", "Paint", "m\\u00b2")',
    '"fixture tuples collide under six-token delimiter-only grouping"',
    'private static string LegacyDelimitedKey(string separator, params string[] tokens)',
    'string.Join(separator, tokens)',
    'var firstRoom = Room("C", roomFamily.Id, "A" + separator + "B", "Room C");',
    'var secondRoom = Room("B" + separator + "C", roomFamily.Id, "A", "Room BC");',
    'var first = LinkedFinish("finish-1", finishFamily.Id, "A" + separator + "B", firstRoom.Id, 2d, "A1");',
    'var collidingUnderOldKey = LinkedFinish("finish-2", finishFamily.Id, "A", secondRoom.Id, 7d, "B1");',
    'var identicalUnlinked = UnlinkedFinish("finish-3", finishFamily.Id, "D", 3d, "C1");',
    'var identicalUnlinkedAgain = UnlinkedFinish("finish-4", finishFamily.Id, "D", 4d, "C2");',
    'Equal(3, rows.Count, "old delimiter collision remains split while identical tuples still group")',
    'Equal(1, firstGroup.Count, "first linked finish remains independent")',
    'Equal(2d, firstGroup.PrimaryQuantity, "first linked quantity remains independent")',
    'Equal("C", firstGroup.RoomIds.Single(), "first room provenance remains independent")',
    'Equal(1, secondGroup.Count, "old delimiter collision no longer merges")',
    'Equal(7d, secondGroup.PrimaryQuantity, "second linked quantity remains independent")',
    'Equal("B" + separator + "C", secondGroup.RoomIds.Single(), "separator-bearing room id is preserved")',
    'Equal(2, identicalGroup.Count, "identical unlinked tuple still groups")',
    'Equal(7d, identicalGroup.PrimaryQuantity, "identical tuple quantities still accumulate")',
    'Equal(2, identicalGroup.SourceHandles.Count, "identical tuple source provenance accumulates")',
    'Equal(0, identicalGroup.RoomIds.Count, "unlinked tuple does not invent room provenance")',
):
    if token not in smoke:
        errors.append("RoomFinishSchedule collision smoke missing token: " + token)

if smoke.count('element.Properties["ParentRoomId"] = roomId;') != 1:
    errors.append("collision fixture must link only through the dedicated LinkedFinish path so identical unlinked rows remain valid under RoomFinishIdentityService")

for token in (
    "using System.Runtime.CompilerServices;",
    "internal static class RoomFinishScheduleGroupKeyCollisionRegistration",
    "[ModuleInitializer]",
    "RoomFinishScheduleGroupKeyCollisionSmoke.Run();",
):
    if token not in registration:
        errors.append("RoomFinishSchedule collision smoke registration missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room Finish grouping uses collision-free length-prefixed identity, preserves case-insensitive grouping, and regression coverage proves the historical six-token collision with accepted printable-delimiter floor/room tuples while retaining valid Room finish identity and normal grouping/provenance.")
