#!/usr/bin/env python3
"""Fail closed if CadHandleService loses bounded/canonical handle ingestion."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "CadHandleService.cs"


def fail(message: str) -> None:
    raise AssertionError(message)


def require(source: str, token: str, message: str) -> None:
    if token not in source:
        fail(message)


def require_before(source: str, first: str, second: str, message: str) -> None:
    first_index = source.find(first)
    second_index = source.find(second)
    if first_index < 0 or second_index < 0 or first_index >= second_index:
        fail(message)


def validate(source: str) -> None:
    require(source, "private const int MaxHandleInputCount = 10000;", "10,000 raw-handle bound is missing")
    require(source, "ValidateKnownCount(handles);", "known-count fast rejection is missing")
    require(source, "handles is ICollection<string> collection", "generic ICollection Count contract is missing")
    require(source, "handles is IReadOnlyCollection<string> readOnlyCollection", "IReadOnlyCollection Count contract is missing")
    require(source, "handles is ICollection nonGenericCollection", "non-generic ICollection Count contract is missing")
    require(source, "ObserveKnownCount(collection.Count", "generic Count must be observed")
    require(source, "ObserveKnownCount(readOnlyCollection.Count", "read-only Count must be observed")
    require(source, "ObserveKnownCount(nonGenericCollection.Count", "non-generic Count must be observed")
    require(source, "if (conflictingKnownCounts)", "conflicting Count evidence must fail closed")
    require(source, "if (invalidKnownCount)", "negative Count evidence must fail closed")
    require(source, "rawCount++;", "streaming raw-item counter is missing")
    require(source, "if (rawCount > MaxHandleInputCount)", "streaming limit+1 rejection is missing")
    require_before(
        source,
        "ValidateKnownCount(handles);",
        "var candidates = new List<ObjectId>();",
        "known-count rejection must happen before candidate materialization and host lookup",
    )
    require_before(
        source,
        "rawCount++;",
        "var normalized = NormalizeHexHandle(text);",
        "limit+1 rejection must happen before normalization and host lookup",
    )
    require_before(
        source,
        "if (knownCount.HasValue && knownCount.Value > MaxHandleInputCount)",
        "if (conflictingKnownCounts)",
        "capacity rejection must retain deterministic precedence over conflicting Count diagnostics",
    )

    require(source, "if (string.IsNullOrWhiteSpace(text)) return null;", "blank handle skip contract is missing")
    require(source, "var nonNullText = text!;", "nullable-safe handle local is missing")
    require(
        source,
        "if (!string.Equals(nonNullText, nonNullText.Trim(), StringComparison.Ordinal)) return null;",
        "padded nonblank handle rejection must remain nullable-safe",
    )
    require(source, "var normalized = nonNullText;", "normalization must continue from the proven non-null handle")
    if "var normalized = (text ?? string.Empty).Trim();" in source:
        fail("padded handle tokens are being canonicalized by Trim")

    require(source, 'StartsWith("0x", StringComparison.OrdinalIgnoreCase)', "canonical 0x prefix support is missing")
    require(source, "StringComparer.OrdinalIgnoreCase", "case-insensitive canonical dedupe is missing")
    require(source, "document.Database.GetObjectId(false, new Handle(value), 0)", "live database handle resolution changed unexpectedly")
    require(source, "IsRecoverableDiagnosticFailure", "recoverable host-failure boundary is missing")
    require(source, "var ids = Resolve(document, handles);", "selection/live-solid paths must reuse bounded Resolve")
    require(source, "foreach (var id in Resolve(document, handles))", "live-handle path must reuse bounded Resolve")


def assert_mutation_fails(source: str, old: str, new: str, label: str) -> None:
    if old not in source:
        fail(f"self-test setup missing token for {label}")
    mutated = source.replace(old, new, 1)
    try:
        validate(mutated)
    except AssertionError:
        return
    fail(f"guard self-test failed to catch mutation: {label}")


def main() -> None:
    source = SOURCE.read_text(encoding="utf-8")
    validate(source)

    assert_mutation_fails(
        source,
        "handles is ICollection nonGenericCollection",
        "handles is IEnumerable nonGenericCollection",
        "non-generic known-count removal",
    )
    assert_mutation_fails(
        source,
        "if (rawCount > MaxHandleInputCount)",
        "if (rawCount > int.MaxValue)",
        "streaming limit+1 weakening",
    )
    assert_mutation_fails(
        source,
        "if (!string.Equals(nonNullText, nonNullText.Trim(), StringComparison.Ordinal)) return null;",
        "// padded tokens accepted",
        "padded-handle canonicality weakening",
    )
    assert_mutation_fails(
        source,
        "var nonNullText = text!;",
        "var nonNullText = text;",
        "nullable-safety weakening",
    )
    assert_mutation_fails(
        source,
        "if (conflictingKnownCounts)",
        "if (false && conflictingKnownCounts)",
        "Count-conflict fail-closed contract",
    )
    assert_mutation_fails(
        source,
        "if (invalidKnownCount)",
        "if (false && invalidKnownCount)",
        "negative Count fail-closed contract",
    )

    print("CadHandleService bounded/canonical ingestion guard passed.")


if __name__ == "__main__":
    main()
