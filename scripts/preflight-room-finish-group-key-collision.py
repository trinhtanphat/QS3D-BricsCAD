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
    'const string separator = "\\u001f";',
    'new ProjectFamily("family" + separator + "material", "Finish A", ElementCategory.WallFinish)',
    'firstFamily.Properties["Material"] = "paint";',
    'new ProjectFamily("family", "Finish B", ElementCategory.WallFinish)',
    'secondFamily.Properties["Material"] = "material" + separator + "paint";',
    'var collidingUnderOldKey = Finish("finish-3", secondFamily.Id, 7d, "B1");',
    'Equal(2, rows.Count, "distinct room-finish grouping tuples remain distinct")',
    'Equal(2, firstGroup.Count, "identical tuple still groups")',
    'Equal(5d, firstGroup.PrimaryQuantity, "identical tuple primary quantity accumulates")',
    'Equal(1, secondGroup.Count, "old delimiter collision no longer merges")',
    'Equal(7d, secondGroup.PrimaryQuantity, "second group primary quantity remains independent")',
    'Equal("finish-3", secondGroup.ElementIds.Single(), "second group element provenance remains independent")',
    'Equal("B1", secondGroup.SourceHandles.Single(), "second group source provenance remains independent")',
    'Equal("room-1", secondGroup.RoomIds.Single(), "second group room provenance remains independent")',
):
    if token not in smoke:
        errors.append("RoomFinishSchedule collision smoke missing token: " + token)

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

print("PASS: Room Finish grouping uses collision-free length-prefixed identity, preserves case-insensitive grouping, and regression coverage separates accepted U+001F-bearing tuples with independent quantities/provenance.")
