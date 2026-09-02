#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Scheduling/ScheduleSnapshot.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ScheduleSnapshotEnumeratorCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (root / "docs/FEATURE-RUNBOOKS/schedule-snapshot-enumerator-count-stability.md").read_text(encoding="utf-8")

required_source = [
    "var knownCount = ReadKnownCount(source, parameterName, collectionName);",
    "using (var enumerator = source.GetEnumerator())",
    "var acquiredCount = ReadKnownCount(source, parameterName, collectionName);",
    "ValidateReboundCount(knownCount, acquiredCount, null, parameterName, collectionName);",
    "while (enumerator.MoveNext())",
    "var reboundCount = ReadKnownCount(source, parameterName, collectionName);",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"ERROR: missing schedule acquisition Count contract token: {token}")

if source.index("var acquiredCount = ReadKnownCount") > source.index("while (enumerator.MoveNext())"):
    raise SystemExit("ERROR: schedule Count rebound must occur before first MoveNext")
if source.index("ValidateReboundCount(knownCount, acquiredCount, null") > source.index("while (enumerator.MoveNext())"):
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
