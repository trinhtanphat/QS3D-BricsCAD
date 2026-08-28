#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
V25 = ROOT / "src/QS3D.BricsCAD.V25/MepRecognitionProfileProvider.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionSmoke.cs"
errors = []

for path in (CORE, V25, SMOKE):
    if not path.is_file():
        errors.append("missing MEP recognition input-bound file: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    core = CORE.read_text(encoding="utf-8")
    for token in (
        "public static class MepRecognitionLimits",
        "public const int MaxRules = 500;",
        "public const int MaxTokensPerRule = 100;",
        "if (tokenIndex >= MepRecognitionLimits.MaxTokensPerRule)",
        "if (index >= MepRecognitionLimits.MaxRules)",
        '"Recognition rule may contain at most " + MepRecognitionLimits.MaxTokensPerRule + " tokens."',
        '"Recognition profile may contain at most " + MepRecognitionLimits.MaxRules + " rules."',
    ):
        if token not in core:
            errors.append("Core MEP recognition source missing bounded-input contract token: " + token)

    token_guard = core.find("if (tokenIndex >= MepRecognitionLimits.MaxTokensPerRule)")
    token_validation = core.find("var value = RequireText(token, nameof(tokens));")
    rule_guard = core.find("if (index >= MepRecognitionLimits.MaxRules)")
    rule_null = core.find("if (rule == null)")
    if min(token_guard, token_validation) < 0 or token_guard > token_validation:
        errors.append("token traversal limit must fail before validating/materializing element limit+1")
    if min(rule_guard, rule_null) < 0 or rule_guard > rule_null:
        errors.append("rule traversal limit must fail before validating/materializing element limit+1")

if V25.is_file():
    v25 = V25.read_text(encoding="utf-8")
    for token in (
        "private const int MaxRules = MepRecognitionLimits.MaxRules;",
        "private const int MaxTokensPerRule = MepRecognitionLimits.MaxTokensPerRule;",
    ):
        if token not in v25:
            errors.append("V25 recognition profile store must consume Core cardinality contract: " + token)
    if "private const int MaxRules = 500;" in v25 or "private const int MaxTokensPerRule = 100;" in v25:
        errors.append("V25 recognition profile store must not duplicate numeric rule/token limits")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RecognitionInputBounds();",
        "MepRecognitionLimits.MaxTokensPerRule",
        "MepRecognitionLimits.MaxRules",
        '"101 recognition tokens"',
        '"501 recognition rules"',
        '"infinite duplicate recognition tokens"',
        '"infinite recognition rules"',
        "MoveNextCalls <= MepRecognitionLimits.MaxTokensPerRule + 1",
        "MoveNextCalls <= MepRecognitionLimits.MaxRules + 1",
    ):
        if token not in smoke:
            errors.append("MEP recognition smoke missing cardinality control: " + token)

print("QS3D MEP recognition input-bound preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Core and V25 share bounded MEP recognition rule/token cardinality, including hostile enumerable termination.")
