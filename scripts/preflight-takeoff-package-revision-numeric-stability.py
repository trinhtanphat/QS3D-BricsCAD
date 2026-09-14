#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageRevision.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageUxSmoke.cs"
PRECISION_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageRevisionNumericPrecisionSmoke.cs"
RUNNER = ROOT / "tests/QS3D.Core.SmokeTests/BenchmarkParitySuiteSmoke.cs"
REVIEW_SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/Qs2DRevisionPackageReview.cs"
REVIEW_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs2DRevisionPackageReviewSmoke.cs"
for path in (SOURCE, SMOKE, PRECISION_SMOKE, RUNNER, REVIEW_SOURCE, REVIEW_SMOKE):
    if not path.is_file():
        raise SystemExit("Takeoff revision numeric preflight missing file: " + str(path.relative_to(ROOT)))
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
precision_smoke = PRECISION_SMOKE.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")
review_source = REVIEW_SOURCE.read_text(encoding="utf-8")
review_smoke = REVIEW_SMOKE.read_text(encoding="utf-8")
for token in (
    "TakeoffRevisionNumeric.CompensatedSum(MarkupDeltas.Select(x => x.QuantityDelta)",
    "TakeoffRevisionNumeric.CompensatedSum(SheetDeltas.Select(x => x.QuantityDelta)",
    "internal static double CompensatedSum(IEnumerable<double> values, string label)",
    "QuantityReportMath.FiniteAccumulator()",
    "accumulator.Add(QsModelElementSnapshot.Finite(value, label), label)",
    "accumulator.Value(label)",
):
    if token not in source:
        raise SystemExit("Takeoff revision numeric production contract missing: " + token)
for stale in (
    "Math.Abs(sum) >= Math.Abs(value)",
    "compensation += correction;",
    "var result = sum + compensation;",
):
    if stale in source:
        raise SystemExit("Takeoff revision stale local accumulator remains: " + stale)
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
for token in (
    "PreservesCanonicalHighDynamicResidual();",
    "10000000000000004d",
    'Equal(3, sheet.AddedCount, "sheet added evidence count");',
    'Equal(1, sheet.RemovedCount, "sheet removed evidence count");',
):
    if token not in precision_smoke:
        raise SystemExit("Takeoff revision dedicated numeric smoke missing contract: " + token)
if "QsTakeoffPackageRevisionNumericPrecisionSmoke.Run();" not in runner:
    raise SystemExit("Takeoff revision dedicated numeric smoke is not wired into deterministic suite")
for token in (
    "QuantityReportMath.FiniteAccumulator()",
    'accumulator.Add(QsModelElementSnapshot.Finite(value, "quantity"), "revision package quantity total")',
    'accumulator.Value("revision package quantity total")',
):
    if token not in review_source:
        raise SystemExit("Revision package review numeric production contract missing: " + token)
for stale in (
    "var adjusted = finite - compensation;",
    "compensation = (next - sum) - adjusted;",
):
    if stale in review_source:
        raise SystemExit("Revision package review stale Kahan aggregation remains: " + stale)
for token in (
    "PreservesSignedDeltaResidualAcrossCancellation();",
    'Near(1d, quantity.QuantityDelta, "signed package delta preserves cancellation residual");',
    'Near(1d, group.QuantityDelta, "signed group delta preserves cancellation residual");',
):
    if token not in review_smoke:
        raise SystemExit("Revision package review numeric smoke missing contract: " + token)
print("PASS takeoff package revision numeric stability contract")