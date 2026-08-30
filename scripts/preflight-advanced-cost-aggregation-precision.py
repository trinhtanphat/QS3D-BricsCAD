#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AdvancedCostAggregationPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/advanced-cost-aggregation-precision.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Advanced cost precision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

for token in (
    "public sealed class CostRateBuildUp",
    "public sealed class CostBenchmarkService",
):
    if token not in source:
        raise SystemExit("Advanced cost precision source boundary missing: " + token)

for token in (
    "RateBuildUpPreservesRecoverableContributions();",
    "RateBuildUpIsOrderIndependent();",
    "BenchmarkAveragePreservesRecoverableContributions();",
    "BenchmarkAverageIsOrderIndependent();",
    "OrdinaryControlsRemainExact();",
    "FinalUnrepresentableRateBuildUpFailsClosed();",
    "10000000000000000000000000000m",
    "10000000000000000000000000001m",
    "decimal.MaxValue",
):
    if token not in smoke:
        raise SystemExit("Advanced cost precision smoke missing contract: " + token)

for phrase in (
    "complete bounded aggregate",
    "rate build-up",
    "benchmark average",
    "order-independent",
    "final exact result",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Advanced cost precision runbook missing boundary: " + phrase)

# This guard is intentionally regression-first: production tokens are added by the correcting commit.
# Once production is fixed the guard must pin the exact accumulator contract and reject pairwise aggregate loops.
print("PASS advanced cost aggregation precision regression contract")
