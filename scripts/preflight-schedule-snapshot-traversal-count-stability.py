#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Scheduling/ScheduleSnapshot.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ScheduleSnapshotTraversalCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (root / "docs/FEATURE-RUNBOOKS/schedule-snapshot-traversal-count-stability.md").read_text(encoding="utf-8")

method_start = source.index("private static List<T> Snapshot<T>")
method_end = source.index("private static int? ReadKnownCount<T>", method_start)
snapshot = source[method_start:method_end]

ordered_tokens = [
    "var moved = enumerator.MoveNext();",
    "var moveNextCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, moveNextCount, null, parameterName, collectionName);",
    "if (!moved)",
    "var item = enumerator.Current;",
    "var currentCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, currentCount, null, parameterName, collectionName);",
    "items.Add(item);",
]
position = -1
for token in ordered_tokens:
    next_position = snapshot.find(token, position + 1)
    if next_position < 0:
        raise SystemExit(f"ERROR: missing schedule traversal Count contract token in Snapshot<T>: {token}")
    if next_position <= position:
        raise SystemExit(f"ERROR: schedule traversal Count contract token is out of order: {token}")
    position = next_position

# The Count checks must be immediate semantic barriers around caller-controlled callbacks:
# no Current read may occur before the post-MoveNext rebound, and no item publication may
# occur before the post-Current rebound.
move = snapshot.index("var moved = enumerator.MoveNext();")
move_rebound = snapshot.index("ValidateReboundCount(knownCount, moveNextCount, null", move)
current = snapshot.index("var item = enumerator.Current;", move)
current_rebound = snapshot.index("ValidateReboundCount(knownCount, currentCount, null", current)
add = snapshot.index("items.Add(item);", current)
if "enumerator.Current" in snapshot[move:move_rebound]:
    raise SystemExit("ERROR: Current is read before the post-MoveNext Count rebound")
if "items.Add(item);" in snapshot[current:current_rebound]:
    raise SystemExit("ERROR: schedule item is published before the post-Current Count rebound")
if not (move < move_rebound < current < current_rebound < add):
    raise SystemExit("ERROR: Schedule traversal Count barriers are not ordered before Current/item acceptance")

for token in [
    "MoveNextCountDriftFailsBeforeCurrent",
    "CurrentCountDriftFailsBeforeAcceptance",
    "CurrentReads != 0",
    "StableCountedActivitiesRemainAccepted",
    "StreamingActivitiesRemainAccepted",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit(f"ERROR: missing traversal regression token: {token}")

for token in ["MoveNext", "Current", "before item acceptance", "pure streaming", "10,000"]:
    if token not in runbook:
        raise SystemExit(f"ERROR: missing traversal runbook token: {token}")

print("PASS schedule snapshot traversal Count stability")
