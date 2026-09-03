#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TradeCostAggregationPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/trade-cost-aggregation-precision.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Trade Cost precision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

service_start = source.find("public sealed class TradeCostAnalysisService")
service_end = source.find("public sealed class BqLibraryEntry", service_start)
if service_start < 0 or service_end < 0:
    raise SystemExit("Trade Cost precision preflight cannot isolate TradeCostAnalysisService.")
service = source[service_start:service_end]

if 'AddPreservingNonZeroContribution(\n                            aggregate.TotalCost' in service:
    raise SystemExit("Trade Cost aggregation must not use pairwise decimal addition for grouped totals.")

for token in (
    "using System.Numerics;",
    "ExactNonNegativeDecimalAccumulator",
    "aggregate.TotalCost.Add(item.Cost);",
    "aggregate.TotalCost.ToDecimal()",
    "decimal.GetBits(value)",
    "BigInteger.One << 96",
    "PowerOfTen(scale - _scale)",
    "while (scale > 0 && coefficient % 10 == 0)",
    "coefficient > MaxDecimalCoefficient",
    "new decimal(low, mid, high, false, (byte)scale)",
):
    if token not in source:
        raise SystemExit("Trade Cost precision source missing exact aggregation contract: " + token)

for token in (
    "RecoverableHalfUnitsArePreserved();",
    "InputOrderDoesNotChangeRepresentableTotal();",
    "SeparateTradesRemainIndependent();",
    "OrdinaryAggregationRemainsExact();",
    "UnrepresentableFinalTotalFailsClosed();",
    "10000000000000000000000000000m",
    "10000000000000000000000000001m",
    "decimal.MaxValue",
):
    if token not in smoke:
        raise SystemExit("Trade Cost precision smoke missing contract: " + token)

for phrase in (
    "exact base-10",
    "representable final total",
    "order-independent",
    "decimal.MaxValue",
    "Count stability",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Trade Cost precision runbook missing boundary: " + phrase)

print("PASS Trade Cost aggregation precision")
