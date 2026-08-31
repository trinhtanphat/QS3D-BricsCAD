#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "ModelHealthBaselineService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ModelHealthBaselineCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

stable = "RequireKnownCountStable(issues, expectedKnownCount);"
move = "var moved = enumerator.MoveNext();"
overrun = "expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value"
current = "var issue = enumerator.Current;"
retain = "result.Add(issue);"

for token in (
    "RequireKnownCountsWithinLimit(issues)",
    "private static void RequireKnownCountStable(",
    stable,
    move,
    overrun,
    current,
    retain,
    "issues is ICollection<ModelHealthIssue> collection",
    "issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection",
    "issues is System.Collections.ICollection nonGenericCollection",
    "known issue count changed during enumeration",
):
    if token not in source:
        raise SystemExit("ModelHealthBaseline Count-integrity guard missing source token: " + token)

pre_move = source.index(stable)
move_pos = source.index(move, pre_move)
post_move = source.index(stable, move_pos + len(move))
overrun_pos = source.index(overrun, post_move)
current_pos = source.index(current, overrun_pos)
post_current = source.index(stable, current_pos + len(current))
retain_pos = source.index(retain, post_current)
if not (pre_move < move_pos < post_move < overrun_pos < current_pos < post_current < retain_pos):
    raise SystemExit(
        "ModelHealthBaseline ordering must be rebound -> MoveNext -> rebound -> overrun/bound -> Current -> rebound -> semantic retention"
    )
if "while (enumerator.MoveNext())" in source:
    raise SystemExit("ModelHealthBaseline must not regress to unguarded while(MoveNext) traversal")

for token in (
    "TransientMoveNextCountDriftFailsBeforeCurrent",
    "TransientCurrentCountDriftFailsBeforeRetention",
    "StableCountedInputRemainsAccepted",
    "PureStreamingInputRemainsAccepted",
    "Equal(0, source.CurrentReads, \"MoveNext drift Current reads\")",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("ModelHealthBaseline Count-integrity smoke missing scenario/token: " + token)

print("PASS model health baseline known-Count traversal integrity source guard")
