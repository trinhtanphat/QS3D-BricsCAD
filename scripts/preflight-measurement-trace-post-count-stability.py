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


def scope(text: str, start_token: str, end_token: str, label: str) -> str:
    start = text.find(start_token)
    end = text.find(end_token, start + 1)
    if start < 0 or end <= start:
        fail(f"missing {label} scope")
    return text[start:end]


def require_final_post_traversal_order(
    text: str,
    observed: str,
    rebound: str,
    publication: str,
    label: str,
) -> None:
    observed_at = text.find(observed)
    rebound_at = text.find(rebound, observed_at + len(observed))
    publication_at = text.find(publication, rebound_at + len(rebound))
    if observed_at < 0 or rebound_at < 0 or publication_at < 0:
        fail(f"missing {label} final post-traversal boundary")
    if not observed_at < rebound_at < publication_at:
        fail(f"invalid {label}: observed count must precede the final Count rebound, which must precede publication")


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    for token, label in (
        ("var finalCount = RequireSupportedCount(source, parameterName, collectionName);", "final deterministic Count sampling"),
        ("if (finalCount != admittedCount)", "admitted/final Count equality"),
        ("RequireSupportedCount(source, parameterName, collectionName)", "shared Count contract reuse"),
    ):
        require(source, token, label)

    facts = scope(
        source,
        "internal static IReadOnlyList<MeasurementTraceFact> SnapshotFacts",
        "internal static IReadOnlyList<MeasurementTraceAdjustment> SnapshotAdjustments",
        "facts",
    )
    adjustments = scope(
        source,
        "internal static IReadOnlyList<MeasurementTraceAdjustment> SnapshotAdjustments",
        "internal static IReadOnlyList<string> SnapshotMessages",
        "adjustments",
    )
    messages = scope(
        source,
        "internal static IReadOnlyList<string> SnapshotMessages",
        "private static int? RequireSupportedCount",
        "messages",
    )

    require_final_post_traversal_order(
        facts,
        "RequireObservedCount(knownCount, items.Count, parameterName, \"facts\");",
        "RequireKnownCountStable(source, knownCount, parameterName, \"facts\");",
        "items.Sort(CompareFacts);",
        "facts",
    )
    require_final_post_traversal_order(
        adjustments,
        "RequireObservedCount(knownCount, items.Count, parameterName, \"adjustments\");",
        "RequireKnownCountStable(source, knownCount, parameterName, \"adjustments\");",
        "items.Sort(CompareAdjustments);",
        "adjustments",
    )
    require_final_post_traversal_order(
        messages,
        "RequireObservedCount(knownCount, items.Count, nameof(source), \"messages\");",
        "RequireKnownCountStable(source, knownCount, nameof(source), \"messages\");",
        "items.Sort(StringComparer.Ordinal);",
        "messages",
    )

    # The historical post-traversal contract must coexist with stronger intra-traversal
    # rebounds. Require at least one earlier rebound as evidence that the final check is not
    # being mistaken for the only Count-stability boundary.
    for body, rebound, label in (
        (facts, "RequireKnownCountStable(source, knownCount, parameterName, \"facts\");", "facts"),
        (adjustments, "RequireKnownCountStable(source, knownCount, parameterName, \"adjustments\");", "adjustments"),
        (messages, "RequireKnownCountStable(source, knownCount, nameof(source), \"messages\");", "messages"),
    ):
        if body.count(rebound) < 2:
            fail(f"{label} must retain both traversal and final Count-stability rebounds")

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
