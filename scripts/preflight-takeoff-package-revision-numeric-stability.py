#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageRevision.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageUxSmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Takeoff revision numeric preflight missing file: " + str(path.relative_to(ROOT)))
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
for token in (
    "TakeoffRevisionNumeric.CompensatedSum(MarkupDeltas.Select(x => x.QuantityDelta)",
    "TakeoffRevisionNumeric.CompensatedSum(SheetDeltas.Select(x => x.QuantityDelta)",
    "internal static double CompensatedSum(IEnumerable<double> values, string label)",
    "Math.Abs(sum) >= Math.Abs(value)",
    "compensation += correction;",
    "double.IsNaN(result) || double.IsInfinity(result)",
):
    if token not in source:
        raise SystemExit("Takeoff revision numeric production contract missing: " + token)
for stale in (
    "MarkupDeltas.Sum(x => x.QuantityDelta)",
    "SheetDeltas.Sum(x => x.QuantityDelta)",
):
    if stale in source:
        raise SystemExit("Takeoff revision naive aggregation remains: " + stale)
for token in (
    "PreservesHighDynamicRangePackageQuantityDelta();",
    "1e16",
    'Near(1d, comparison.QuantityDelta, 0d, "high dynamic range revision quantity delta");',
):
    if token not in smoke:
        raise SystemExit("Takeoff revision numeric smoke missing contract: " + token)
print("PASS takeoff package revision numeric stability contract")