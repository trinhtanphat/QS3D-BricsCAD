#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CostBenchmarkMedianPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/cost-benchmark-average-precision.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Cost benchmark average precision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "private static decimal CalculateAverage(IReadOnlyList<decimal> values)",
    "var baseline = values[0];",
    "var translated = new decimal[values.Count];",
    "translated[i] = checked(values[i] - baseline);",
    "CostDecimalMath.TrySumNonNegativeExactly(translated, out var translatedSum)",
    '"benchmark translated average contribution"',
    '"benchmark translated average"',
):
    if token not in source:
        raise SystemExit("Cost benchmark average source missing translation-stable contract: " + token)

raw_sum = source.index("CostDecimalMath.TrySumNonNegativeExactly(values, out var exactSum)")
baseline = source.index("var baseline = values[0];", raw_sum)
translated_sum = source.index("CostDecimalMath.TrySumNonNegativeExactly(translated, out var translatedSum)", baseline)
final_rebind = source.index('"benchmark translated average"', translated_sum)
if not raw_sum < baseline < translated_sum < final_rebind:
    raise SystemExit("Cost benchmark average must preserve the exact-sum fast path before translated aggregation and guarded baseline rebind.")

for token in (
    "RepresentableHighMagnitudeAverageRemainsAccepted();",
    "UnrepresentableHighMagnitudeAverageFailsClosed();",
    "decimal.MaxValue - 2m",
    "decimal.MaxValue - 1m",
    "Equal(expected, result.AverageUnitCost",
    '"benchmark translated average"',
    "OrdinaryEvenMedianRemainsStable();",
):
    if token not in smoke:
        raise SystemExit("Cost benchmark average smoke missing deterministic contract: " + token)

for phrase in (
    "Lane-Key: `issue-5318`",
    "decimal.MaxValue - 1",
    "translation",
    "representable final average",
    "fail closed",
    "NOT_APPLICABLE",
):
    if phrase not in runbook:
        raise SystemExit("Cost benchmark average runbook missing phrase: " + phrase)

print("PASS cost benchmark average precision")
