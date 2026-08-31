#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CommercialAggregatePrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/commercial-aggregate-precision.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Commercial aggregate precision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "public sealed class TenderEvaluationService",
    "var evaluatedContributions = new List<decimal>(requirementList.Count);",
    "evaluatedContributions.Add(lineCost);",
    "CostDecimalMath.TrySumNonNegativeExactly(evaluatedContributions, out var total)",
    "Tender evaluated total exact aggregate cannot be represented as decimal.",
    "public sealed class ProgressClaimService",
    "var grossContributions = new decimal[itemCodes.Count];",
    "grossContributions[i] = value;",
    "CostDecimalMath.TrySumNonNegativeExactly(grossContributions, out var gross)",
    "Progress gross certified exact aggregate cannot be represented as decimal.",
):
    if token not in source:
        raise SystemExit("Commercial aggregate precision source boundary missing: " + token)

for stale in (
    'total = CostDecimalMath.AddPreservingNonZeroContribution(\n                        total,\n                        lineCost,\n                        "tender evaluated total")',
    'gross = CostDecimalMath.AddPreservingNonZeroContribution(\n                        gross,\n                        value,\n                        "progress gross certified this period")',
):
    if stale in source:
        raise SystemExit("Commercial aggregate precision stale pairwise total remains: " + stale.splitlines()[0])

for token in (
    "TenderPreservesRecoverableContributions();",
    "TenderCanonicalOrderIsCallerOrderIndependent();",
    "ProgressPreservesRecoverableContributions();",
    "ProgressCanonicalOrderIsCallerOrderIndependent();",
    "OrdinaryControlsRemainExact();",
    "FinalUnrepresentableTotalsFailClosed();",
    "10000000000000000000000000000m",
    "10000000000000000000000000001m",
    "decimal.MaxValue",
):
    if token not in smoke:
        raise SystemExit("Commercial aggregate precision smoke missing contract: " + token)

for phrase in (
    "Tender evaluated totals",
    "progress gross-certified totals",
    "complete exact aggregate",
    "caller enumeration order",
    "fail-closed",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Commercial aggregate precision runbook missing boundary: " + phrase)

print("PASS commercial aggregate precision production contract")
