#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Rebar" / "RebarSchedule.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarScheduleInputCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

stable = "RequireKnownInputCountStable(inputs, expectedInputCount, nameof(inputs));"
move = "if (!enumerator.MoveNext())"
current = "var input = enumerator.Current;"
retain = "Append(input ?? throw new ArgumentException"
overrun = "expectedInputCount.HasValue && observedInputCount >= expectedInputCount.Value"

for token in (
    "ValidateKnownInputCount(inputs, nameof(inputs))",
    "private static void RequireKnownInputCountStable(",
    stable,
    move,
    current,
    retain,
    overrun,
    "inputs is ICollection<RebarScheduleInput> genericCollection",
    "inputs is IReadOnlyCollection<RebarScheduleInput> readOnlyCollection",
    "inputs is ICollection nonGenericCollection",
    "known Count changed during traversal",
):
    if token not in source:
        raise SystemExit("RebarSchedule Count-integrity source guard missing token: " + token)

pre_move = source.index(stable)
move_pos = source.index(move, pre_move)
post_move = source.index(stable, move_pos + len(move))
overrun_pos = source.index(overrun, post_move)
current_pos = source.index(current, overrun_pos)
post_current = source.index(stable, current_pos + len(current))
retain_pos = source.index(retain, post_current)
if not (pre_move < move_pos < post_move < overrun_pos < current_pos < post_current < retain_pos):
    raise SystemExit(
        "RebarSchedule Count ordering must be rebound -> MoveNext -> rebound -> overrun/bound -> Current -> rebound -> semantic acceptance"
    )
if "while (enumerator.MoveNext())" in source:
    raise SystemExit("RebarSchedule must not regress to caller-controlled while(MoveNext) traversal")

for token in (
    "TransientMoveNextCountDriftFailsBeforeCurrent",
    "TransientCurrentCountDriftFailsBeforeSemanticAcceptance",
    "StableCountedInputRemainsAccepted",
    "PureStreamingInputRemainsAccepted",
    "Equal(0, source.CurrentReads, \"MoveNext drift Current reads\")",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("RebarSchedule Count-integrity smoke missing scenario/token: " + token)

print("PASS rebar schedule input known-Count traversal integrity source guard")
