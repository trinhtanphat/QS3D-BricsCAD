#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Scheduling/ScheduleSnapshot.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ScheduleSnapshotTraversalCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (root / "docs/FEATURE-RUNBOOKS/schedule-snapshot-traversal-count-stability.md").read_text(encoding="utf-8")

required = [
    "var moved = enumerator.MoveNext();",
    "var moveNextCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, moveNextCount, null, parameterName, collectionName);",
    "if (!moved)",
    "var item = enumerator.Current;",
    "var currentCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, currentCount, null, parameterName, collectionName);",
]
for token in required:
    if token not in source:
        raise SystemExit(f"ERROR: missing traversal Count contract token: {token}")

move = source.index("var moved = enumerator.MoveNext();")
move_count = source.index("var moveNextCount = ReadKnownCount", move)
current = source.index("var item = enumerator.Current;", move)
current_count = source.index("var currentCount = ReadKnownCount", current)
add = source.index("items.Add(item);", current)
if not (move < move_count < current < current_count < add):
    raise SystemExit("ERROR: Schedule traversal Count rebounds are not ordered before Current/item acceptance")

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
