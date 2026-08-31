#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Measurement/MeasurementTrace.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementTracePostTraversalCountStabilitySmoke.cs"


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
        ("RequireKnownCountStable(source, knownCount, parameterName, \"facts\");", "fact post-traversal Count rebind"),
        ("RequireKnownCountStable(source, knownCount, parameterName, \"adjustments\");", "adjustment post-traversal Count rebind"),
        ("RequireKnownCountStable(source, knownCount, nameof(source), \"messages\");", "message post-traversal Count rebind"),
        ("var finalCount = RequireSupportedCount(source, parameterName, collectionName);", "final deterministic Count sampling"),
        ("if (finalCount != admittedCount)", "admitted/final Count equality"),
        ("RequireSupportedCount(source, parameterName, collectionName)", "shared Count contract reuse"),
    ):
        require(source, token, label)

    require_order(
        source,
        "RequireObservedCount(knownCount, items.Count, parameterName, \"facts\");",
        "RequireKnownCountStable(source, knownCount, parameterName, \"facts\");",
        "facts observed-count before Count stability rebind",
    )
    require_order(
        source,
        "RequireKnownCountStable(source, knownCount, parameterName, \"facts\");",
        "items.Sort(CompareFacts);",
        "facts Count stability before canonical sorting",
    )
    require_order(
        source,
        "RequireObservedCount(knownCount, items.Count, parameterName, \"adjustments\");",
        "RequireKnownCountStable(source, knownCount, parameterName, \"adjustments\");",
        "adjustment observed-count before Count stability rebind",
    )
    require_order(
        source,
        "RequireKnownCountStable(source, knownCount, parameterName, \"adjustments\");",
        "items.Sort(CompareAdjustments);",
        "adjustment Count stability before canonical sorting",
    )
    require_order(
        source,
        "RequireObservedCount(knownCount, items.Count, nameof(source), \"messages\");",
        "RequireKnownCountStable(source, knownCount, nameof(source), \"messages\");",
        "message observed-count before Count stability rebind",
    )
    require_order(
        source,
        "RequireKnownCountStable(source, knownCount, nameof(source), \"messages\");",
        "items.Sort(StringComparer.Ordinal);",
        "message Count stability before canonical sorting",
    )

    for token in (
        "FactCountDriftAfterExactTraversalFailsClosed",
        "AdjustmentCountDriftAfterExactTraversalFailsClosed",
        "MessageCountDriftAfterExactTraversalFailsClosed",
        "NegativeFinalCountFailsClosed",
        "StableCountedChildrenRemainAccepted",
        "PureStreamingChildrenRemainAccepted",
        "CountReads",
    ):
        require(smoke, token, "deterministic regression")

    print("PASS measurement trace post-traversal Count stability source guard")


if __name__ == "__main__":
    main()
