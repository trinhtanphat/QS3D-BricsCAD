#!/usr/bin/env python3
from pathlib import Path
import math

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Reporting/QuantityGeometryExplanationService.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/v25-quantity-geometry-deduction-aggregation-precision.md"

if not SOURCE.is_file():
    raise SystemExit("native quantity geometry precision source is missing")

source = SOURCE.read_text(encoding="utf-8")

# Deterministic binary64 reproduction: pairwise traversal loses both low-order units.
values = (1e16, 1.0, 1.0)
pairwise = 0.0
for value in values:
    pairwise += value
expected = math.fsum(values)
if pairwise == expected or expected != 10000000000000002.0:
    raise SystemExit("numeric RED fixture is invalid")

required_source = (
    "QuantityReportMath.FiniteAccumulator",
    "grossVolumeAccumulator.Add(SafeVolumeCad(target)",
    "netVolumeAccumulator.Add(SafeVolumeCad(volumeResidual)",
    "individualVolumeAccumulators",
    "individualAreaAccumulators",
    "residualAreaAccumulators",
    "coverageAccumulator.Add(areaCad",
    "FinalizeAccumulators(individualVolumeAccumulators",
    "FinalizeNestedAccumulators(individualAreaAccumulators",
    "SumFinite(individualVolumeCad.Values",
)
for token in required_source:
    if token not in source:
        raise SystemExit("native quantity geometry compensated aggregation missing: " + token)

for stale in (
    "grossVolumeCad += SafeVolumeCad(target)",
    "netVolumeCad += SafeVolumeCad(volumeResidual)",
    "individualVolumeCad.Values.Sum()",
    "residualAreasCad[best] += areaCad",
    "totalCad += areaCad",
    "? current : 0d) + value",
):
    if stale in source:
        raise SystemExit("native quantity geometry pairwise aggregation remains: " + stale)

if RUNBOOK.is_file():
    runbook = RUNBOOK.read_text(encoding="utf-8")
    for phrase in (
        "FiniteAccumulator",
        "1e16 + 1 + 1",
        "fail closed",
        "LOCAL_ONLY",
    ):
        if phrase not in runbook:
            raise SystemExit("native quantity geometry runbook missing contract: " + phrase)
print("PASS native V25 quantity geometry compensated aggregation precision guard")
