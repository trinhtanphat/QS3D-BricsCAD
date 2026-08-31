#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionCurrentIntegritySmoke.cs"
CURRENT_DRIFT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionCurrentCountDriftSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
current_drift_smoke = CURRENT_DRIFT_SMOKE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    raise SystemExit("MEP recognition Count-stability guard failed: " + message)


def require(token: str, text: str, label: str) -> None:
    if token not in text:
        fail(f"{label} missing token: {token}")


def ordered(text: str, tokens: tuple[str, ...], label: str) -> None:
    position = -1
    for token in tokens:
        position = text.find(token, position + 1)
        if position < 0:
            fail(f"{label} missing ordered token: {token}")

for token in (
    "using System.Collections;",
    "internal static class MepRecognitionCollectionContract",
    "source is ICollection<T> collection",
    "source is IReadOnlyCollection<T> readOnlyCollection",
    "source is ICollection nonGenericCollection",
    "reports a negative known count",
    "reports conflicting known counts",
    "known count changed during traversal",
):
    require(token, source, "production Count contract")

for constructor_marker, enumerator_marker, count_marker, index_marker, overrun_marker, current_marker, semantic_marker, under_yield_marker, label in (
    (
        "if (tokens == null) throw new ArgumentNullException(nameof(tokens));",
        "using (var enumerator = tokens.GetEnumerator())",
        "tokens, knownCount, MepRecognitionLimits.MaxTokensPerRule",
        "if (knownCount.HasValue && tokenIndex >= knownCount.Value)",
        "knownCount.Value, tokenIndex + 1",
        "var token = enumerator.Current;",
        "tokenIndex++;",
        "knownCount.Value, tokenIndex, nameof(tokens)",
        "token traversal",
    ),
    (
        "if (rules == null) throw new ArgumentNullException(nameof(rules));",
        "using (var enumerator = rules.GetEnumerator())",
        "rules, knownCount, MepRecognitionLimits.MaxRules",
        "if (knownCount.HasValue && index >= knownCount.Value)",
        "knownCount.Value, index + 1",
        "var rule = enumerator.Current;",
        "if (rule == null)",
        "knownCount.Value, index, nameof(rules)",
        "rule traversal",
    ),
):
    start = source.find(constructor_marker)
    end = source.find("if (normalized.Count == 0)" if "tokens" in constructor_marker else "if (snapshot.Count == 0)", start)
    if start < 0 or end < 0:
        fail(label + " boundary missing")
    block = source[start:end]
    ordered(
        block,
        (
            "SnapshotKnownCount(",
            enumerator_marker,
            count_marker,
            "if (!enumerator.MoveNext())",
            count_marker,
            index_marker,
            overrun_marker,
            current_marker,
            count_marker,
            semantic_marker,
            under_yield_marker,
            count_marker,
        ),
        label,
    )

for token in (
    "TokenKnownCountOverrunRejectsBeforeCurrent",
    "RuleKnownCountOverrunRejectsBeforeCurrent",
    "TokenKnownCountUnderYieldRejects",
    "RuleKnownCountUnderYieldRejects",
    "TokenTransientCountGrowthRejectsBeforeCurrent",
    "RuleTransientCountShrinkRejectsBeforeCurrent",
    "TokenTransientNegativeCountRejectsBeforeCurrent",
    "ConflictingCountEvidenceRejectsBeforeEnumeration",
    "StableCountedInputsRemainAccepted",
    "Equal(0, source.CurrentReads",
):
    require(token, smoke, "historical deterministic smoke")

for token in (
    "TokenCurrentCountDriftWinsBeforeMalformedTokenValidation",
    "RuleCurrentCountDriftWinsBeforeNullRuleValidation",
    "CurrentCountDriftProbe<string>(\" \")",
    "CurrentCountDriftProbe<MepRecognitionRule>(null!)",
    "known count changed during traversal",
    "_owner._count = 2;",
    "Equal(1, source.CurrentReads",
):
    require(token, current_drift_smoke, "post-Current deterministic smoke")

print("PASS MEP recognition known-Count stability")
