#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / "src/QS3D.Core/Commercial/EstimatingWorkflow.cs"
ACCUMULATOR = ROOT / "src/QS3D.Core/Commercial/CommercialExactDecimalAccumulator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/EstimatingPortfolioAggregationPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/estimating-portfolio-aggregation-precision.md"

for path in (WORKFLOW, ACCUMULATOR, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Estimating aggregation precision preflight missing file: " + str(path.relative_to(ROOT)))

workflow = WORKFLOW.read_text(encoding="utf-8")
accumulator = ACCUMULATOR.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "var total = new CommercialExactDecimalAccumulator();",
    'total.Add(amount.Value, "Estimating portfolio total");',
    'return total.ToDecimal("Estimating portfolio total");',
    "var before = new CommercialExactDecimalAccumulator();",
    "var after = new CommercialExactDecimalAccumulator();",
    'aggregate.Quantity.Add(line.Quantity, "Bulk rate assignment unit quantity");',
    'before.Add(oldAmount.Value, "Bulk rate assignment total before");',
    'after.Add(newAmount, "Bulk rate assignment total after");',
    'pair.Value.Quantity.ToDecimal("Bulk rate assignment unit quantity")',
    'before.ToDecimal("Bulk rate assignment total before")',
    'after.ToDecimal("Bulk rate assignment total after")',
):
    if token not in workflow:
        raise SystemExit("Estimating aggregation workflow contract missing: " + token)

for stale in (
    'total = CommercialGuard.Add(total, amount.Value, "Estimating portfolio total")',
    'aggregate.Quantity = CommercialGuard.Add(aggregate.Quantity, line.Quantity, "Bulk rate assignment unit quantity")',
    'before = CommercialGuard.Add(before, oldAmount.Value, "Bulk rate assignment total before")',
    'after = CommercialGuard.Add(after, newAmount, "Bulk rate assignment total after")',
):
    if stale in workflow:
        raise SystemExit("Estimating aggregation stale pairwise fold remains: " + stale)

# The accumulator may share exact decimal helpers with CommercialGuard.Add/Subtract.
# Assert the semantic scale-alignment and final materialization contract rather than
# pinning the historical inline `_scale` implementation detail.
for token in (
    "using System.Numerics;",
    "internal sealed class CommercialExactDecimalAccumulator",
    "MaximumDecimalCoefficient",
    "decimal.GetBits(value)",
    "AlignScales(ref _coefficient, ref _scale, ref coefficient, ref scale);",
    "BigInteger.Pow(10, rightScale - leftScale)",
    "BigInteger.Pow(10, leftScale - rightScale)",
    "while (scale > 0 && signedCoefficient % 10 == 0)",
    "exact aggregate cannot be represented as decimal",
):
    if token not in accumulator:
        raise SystemExit("Estimating exact accumulator contract missing: " + token)

for token in (
    "PortfolioPreservesRecoverableContributions();",
    "OrdinaryAndUnpricedControlsRemainCorrect();",
    "BulkPreviewPreservesRecoverableAggregates();",
    "FinalUnrepresentablePortfolioTotalFailsClosed();",
    "10000000000000000000000000000m",
    "10000000000000000000000000001m",
    "0.5m",
    "decimal.MaxValue",
):
    if token not in smoke:
        raise SystemExit("Estimating aggregation smoke missing contract: " + token)

for phrase in (
    "complete exact aggregate",
    "PricedTotal",
    "bulk-rate preview",
    "unit quantity",
    "unpriced lines",
    "final mathematical total",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Estimating aggregation runbook missing boundary: " + phrase)

print("PASS estimating portfolio aggregation precision production contract")
