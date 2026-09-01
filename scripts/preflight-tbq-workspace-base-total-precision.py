#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/TbqProjectWorkspaceState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/TbqWorkspaceBaseTotalPrecisionSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/tbq-workspace-base-total-precision.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("TBQ base-total precision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

start = source.find("public decimal BaseTotal")
end = source.find("public CostAdjustmentResult PreviewAdjustment", start)
if start < 0 or end < 0:
    raise SystemExit("TBQ base-total precision preflight cannot isolate BaseTotal.")
base_total = source[start:end]

if "AddPreservingNonZeroContribution" in base_total:
    raise SystemExit("TBQ BaseTotal must not use pairwise decimal addition for project aggregation.")

for token in (
    "var contributions = new decimal[BillItems.Count];",
    "contributions[i] = BillItems[i].TotalCost;",
    "CostDecimalMath.TrySumNonNegativeExactly(contributions, out var total)",
    "TBQ workspace base total is not representable as decimal.",
    "TBQ workspace base total overflowed decimal arithmetic.",
):
    if token not in base_total:
        raise SystemExit("TBQ base-total precision source missing exact aggregation contract: " + token)

for token in (
    "PreservesRepresentableRecoveredAggregate();",
    "PreservesCanonicalOrderIndependence();",
    "PreservesOrdinaryAggregateAndPreview();",
    "RejectsFinalUnrepresentableAggregate();",
    "10000000000000000000000000000m",
    "0.5m",
    "Large + 1m",
):
    if token not in smoke:
        raise SystemExit("TBQ base-total precision smoke missing contract: " + token)

for phrase in (
    "complete exact aggregate",
    "representable final total",
    "canonical bill-item ordering",
    "final aggregate is not representable",
    "per-item multiplication",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("TBQ base-total precision runbook missing boundary: " + phrase)

print("PASS TBQ workspace base-total precision")
