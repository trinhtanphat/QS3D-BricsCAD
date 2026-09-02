#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CostBenchmarkMedianPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/cost-benchmark-exact-average.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Cost benchmark exact-average preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "TryAverageNonNegativeExactly",
    "BigInteger.GreatestCommonDivisor",
    "MaximumDecimalCoefficient",
    "if (CostDecimalMath.TryAverageNonNegativeExactly(values, out var exactAverage))",
    "return exactAverage;",
    "TrySumNonNegativeExactly(values, out var exactSum)",
    "benchmark translated average contribution",
):
    if token not in source:
        raise SystemExit("Cost benchmark exact-average source contract missing: " + token)

for token in (
    "RepresentableOverflowedAggregateAverageRemainsExact();",
    "36566844237352771197020284770m",
    "decimal.MaxValue",
    "incremental-rounding drift",
    "UnrepresentableHighMagnitudeAverageFailsClosed();",
    "OrdinaryEvenMedianRemainsStable();",
):
    if token not in smoke:
        raise SystemExit("Cost benchmark exact-average smoke contract missing: " + token)

for phrase in (
    "Lane-Key: `issue-5351`",
    "seven zero-valued samples",
    "six `decimal.MaxValue` samples",
    "exact rational probe",
    "historical non-exact fallback",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Cost benchmark exact-average runbook missing boundary: " + phrase)

print("PASS cost benchmark exact representable average contract")
