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
        ("RequireTraversalCapacity(knownCount, items.Count, nameof(traces));", "known-Count overrun guard"),
        ("RequireObservedCount(knownCount, items.Count, nameof(traces));", "under-yield guard"),
        ("RequireKnownCountStable(traces, knownCount, nameof(traces));", "post-traversal Count rebind"),
        ("var finalCount = RequireSupportedCount(traces, paramName);", "final deterministic Count sampling"),
        ("if (finalCount != admittedCount)", "admission/final Count equality"),
        ("if (traces is ICollection<MeasurementTrace> collection)", "generic ICollection Count surface"),
        ("if (traces is IReadOnlyCollection<MeasurementTrace> readOnlyCollection)", "IReadOnlyCollection Count surface"),
        ("if (traces is System.Collections.ICollection nonGenericCollection)", "non-generic ICollection Count surface"),
    ):
        require(source, token, label)

    require_order(
        source,
        "RequireObservedCount(knownCount, items.Count, nameof(traces));",
        "RequireKnownCountStable(traces, knownCount, nameof(traces));",
        "post-traversal stability ordering",
    )
    require_order(
        source,
        "RequireKnownCountStable(traces, knownCount, nameof(traces));",
        "items.Sort(CompareTraces);",
        "Count stability before canonical publication work",
    )

    for token in (
        "CountDriftAfterExactTraversalFailsClosed();",
        "NegativeCountAfterExactTraversalFailsClosed();",
        "MultiInterfaceConflictAfterTraversalFailsClosed();",
        "StableMultiInterfaceCountRemainsAccepted();",
        "StableReadOnlyCountIsReboundAfterTraversal();",
        "KnownCountOverrunWinsBeforeInvalidExtraTrace();",
        "KnownCountUnderYieldStillFailsClosed();",
        "PureStreamingSourceRemainsAccepted();",
        "[ModuleInitializer]",
    ):
        require(smoke, token, "registered deterministic measurement Count regression")

    print("PASS measurement snapshot known-Count stability source guard")


if __name__ == "__main__":
    main()
