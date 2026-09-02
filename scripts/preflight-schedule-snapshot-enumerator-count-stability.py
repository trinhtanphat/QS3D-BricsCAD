#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Scheduling/ScheduleSnapshot.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ScheduleSnapshotEnumeratorCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (root / "docs/FEATURE-RUNBOOKS/schedule-snapshot-enumerator-count-stability.md").read_text(encoding="utf-8")

method_start = source.index("private static List<T> Snapshot<T>")
method_end = source.index("private static int? ReadKnownCount<T>", method_start)
snapshot = source[method_start:method_end]

required_source = [
    "var knownCount = ReadKnownCount(source, parameterName, collectionName);",
    "using (var enumerator = source.GetEnumerator())",
    "var acquiredCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, acquiredCount, null, parameterName, collectionName);",
    "enumerator.MoveNext()",
    "var reboundCount = ReadKnownCount(source, parameterName, collectionName);",
]
for token in required_source:
    if token not in snapshot:
        raise SystemExit(f"ERROR: missing schedule acquisition Count contract token in Snapshot<T>: {token}")

# The acquisition guard protects the first caller-controlled traversal callback, not a
# particular loop spelling. Snapshot<T> may use `while (enumerator.MoveNext())` or the
# stronger traversal form `var moved = enumerator.MoveNext()` introduced by the
# traversal-count guard; both must remain behind the acquisition rebound.
first_move_next = snapshot.index("enumerator.MoveNext()")
if snapshot.index("var acquiredCount = ReadKnownCount") > first_move_next:
    raise SystemExit("ERROR: schedule Count rebound must occur before first MoveNext")
if snapshot.index("ValidateReboundCount(knownCount, acquiredCount, null") > first_move_next:
    raise SystemExit("ERROR: schedule acquisition Count validation must occur before first MoveNext")

for token in [
    "MoveNextCalls != 0",
    "AcquisitionCountDriftFailsBeforeTraversal",
    "StableCountedActivitiesRemainAccepted",
    "StreamingActivitiesRemainAccepted",
    "[ModuleInitializer]",
]:
    if token not in smoke:
        raise SystemExit(f"ERROR: missing deterministic schedule regression token: {token}")

for token in ["GetEnumerator", "before first MoveNext", "10,000", "pure streaming"]:
    if token not in runbook:
        raise SystemExit(f"ERROR: missing runbook contract token: {token}")

print("PASS schedule snapshot enumerator-acquisition Count stability")
