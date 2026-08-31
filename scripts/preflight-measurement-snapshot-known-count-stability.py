#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Measurement/MeasurementSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementSnapshotKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"missing {label}: {token}")


def require_order(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        fail(f"invalid {label}: expected {first!r} before {second!r}")


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token, label in (
        ("var knownCount = RequireSupportedCount(traces, nameof(traces));", "admission Count bind"),
        ("using (var enumerator = traces.GetEnumerator())", "explicit single-pass enumerator"),
        ("var hasNext = enumerator.MoveNext();", "caller-controlled MoveNext boundary"),
        ("var trace = enumerator.Current;", "caller-controlled Current boundary"),
        ("RequireTraversalCapacity(knownCount, items.Count, nameof(traces));", "known-Count overrun guard"),
        ("RequireObservedCount(knownCount, items.Count, nameof(traces));", "under-yield guard"),
        ("RequireKnownCountStable(traces, knownCount, nameof(traces));", "Count stability rebound"),
        ("var observedCount = RequireSupportedCount(traces, paramName);", "deterministic Count resampling"),
        ("if (observedCount != admittedCount)", "admission/observed Count equality"),
        ("if (traces is ICollection<MeasurementTrace> collection)", "generic ICollection Count surface"),
        ("if (traces is IReadOnlyCollection<MeasurementTrace> readOnlyCollection)", "IReadOnlyCollection Count surface"),
        ("if (traces is System.Collections.ICollection nonGenericCollection)", "non-generic ICollection Count surface"),
    ):
        require(source, token, label)

    constructor_start = source.index("using (var enumerator = traces.GetEnumerator())")
    constructor_end = source.index("RequireObservedCount(knownCount, items.Count, nameof(traces));", constructor_start)
    traversal = source[constructor_start:constructor_end]

    pre_move = traversal.find("RequireKnownCountStable(traces, knownCount, nameof(traces));")
    move = traversal.find("var hasNext = enumerator.MoveNext();", pre_move + 1)
    post_move = traversal.find("RequireKnownCountStable(traces, knownCount, nameof(traces));", move + 1)
    capacity = traversal.find("RequireTraversalCapacity(knownCount, items.Count, nameof(traces));", post_move + 1)
    current = traversal.find("var trace = enumerator.Current;", capacity + 1)
    post_current = traversal.find("RequireKnownCountStable(traces, knownCount, nameof(traces));", current + 1)
    null_check = traversal.find("if (trace == null)", post_current + 1)
    retention = traversal.find("items.Add(trace);", null_check + 1)
    if min(pre_move, move, post_move, capacity, current, post_current, null_check, retention) < 0:
        fail("measurement snapshot traversal contract tokens are incomplete")
    if not (pre_move < move < post_move < capacity < current < post_current < null_check < retention):
        fail("measurement snapshot traversal must enforce Count -> MoveNext -> Count -> overrun -> Current -> Count -> payload acceptance -> retention")

    require_order(
        source,
        "RequireObservedCount(knownCount, items.Count, nameof(traces));",
        "items.Sort(CompareTraces);",
        "under-yield before canonical publication work",
    )
    final_rebound = source.find(
        "RequireKnownCountStable(traces, knownCount, nameof(traces));",
        source.find("RequireObservedCount(knownCount, items.Count, nameof(traces));"),
    )
    sort = source.find("items.Sort(CompareTraces);")
    if final_rebound < 0 or sort < 0 or final_rebound >= sort:
        fail("final Count rebound must remain before canonical sorting/publication")

    for token in (
        "TransientMoveNextCountDriftFailsBeforeCurrent();",
        "TransientCurrentCountDriftFailsBeforeRetention();",
        "CountDriftAfterExactTraversalFailsClosed();",
        "NegativeCountAfterExactTraversalFailsClosed();",
        "MultiInterfaceConflictAfterTraversalFailsClosed();",
        "StableMultiInterfaceCountRemainsAccepted();",
        "StableReadOnlyCountIsReboundAroundTraversal();",
        "KnownCountOverrunWinsBeforeInvalidExtraTrace();",
        "KnownCountUnderYieldStillFailsClosed();",
        "PureStreamingSourceRemainsAccepted();",
        "Equal(0, source.CurrentReads, \"MoveNext drift must fail before Current is read.\");",
        "Equal(1, source.CurrentReads, \"Current may be read once but its trace must not be retained before Count rebound.\");",
        "[ModuleInitializer]",
    ):
        require(smoke, token, "registered deterministic measurement Count regression")

    print("PASS measurement snapshot known-Count stability source guard")


if __name__ == "__main__":
    main()
