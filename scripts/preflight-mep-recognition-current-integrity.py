#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionCurrentIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    raise SystemExit("MEP recognition Current-integrity guard failed: " + message)


def ordered(text: str, tokens: tuple[str, ...], label: str) -> None:
    position = -1
    for token in tokens:
        next_position = text.find(token, position + 1)
        if next_position < 0:
            fail(f"{label} missing ordered token: {token}")
        position = next_position


try:
    token_start = source.index("if (tokens == null) throw new ArgumentNullException(nameof(tokens));")
    token_end = source.index("if (normalized.Count == 0)", token_start)
    rule_start = source.index("if (rules == null) throw new ArgumentNullException(nameof(rules));")
    rule_end = source.index("if (snapshot.Count == 0)", rule_start)
except ValueError as error:
    fail("constructor snapshot boundary missing: " + str(error))

token_block = source[token_start:token_end]
rule_block = source[rule_start:rule_end]

ordered(
    token_block,
    (
        "using (var enumerator = tokens.GetEnumerator())",
        "while (true)",
        "if (!enumerator.MoveNext())",
        "if (knownCount.HasValue && tokenIndex >= knownCount.Value)",
        "if (tokenIndex >= MepRecognitionLimits.MaxTokensPerRule)",
        "var token = enumerator.Current;",
        "tokenIndex++;",
        "var value = RequireText(token, nameof(tokens));",
    ),
    "token traversal",
)
ordered(
    rule_block,
    (
        "using (var enumerator = rules.GetEnumerator())",
        "while (true)",
        "if (!enumerator.MoveNext())",
        "if (knownCount.HasValue && index >= knownCount.Value)",
        "if (index >= MepRecognitionLimits.MaxRules)",
        "var rule = enumerator.Current;",
        "if (rule == null)",
        "snapshot.Add(rule);",
        "index++;",
    ),
    "rule traversal",
)

for forbidden in (
    "foreach (var token in tokens)",
    "foreach (var rule in rules)",
    "while (enumerator.MoveNext())",
):
    if forbidden in token_block or forbidden in rule_block:
        fail("caller-controlled Current ordering may bypass a pre-Current guard: " + forbidden)

for token in (
    "TokenLimitRejectsBeforeReadingOverrunCurrent",
    "RuleLimitRejectsBeforeReadingOverrunCurrent",
    "MepRecognitionLimits.MaxTokensPerRule + 1",
    "MepRecognitionLimits.MaxTokensPerRule, source.CurrentReads",
    "MepRecognitionLimits.MaxRules + 1",
    "MepRecognitionLimits.MaxRules, source.CurrentReads",
    "MEP token limit must reject element 101 before reading caller Current.",
    "MEP rule limit must reject element 501 before reading caller Current.",
    "TokenKnownCountOverrunRejectsBeforeCurrent",
    "RuleKnownCountOverrunRejectsBeforeCurrent",
):
    if token not in smoke:
        fail("deterministic smoke evidence missing: " + token)

print("PASS MEP recognition rule/token Current integrity")