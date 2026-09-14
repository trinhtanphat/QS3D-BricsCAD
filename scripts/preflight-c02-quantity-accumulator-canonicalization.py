#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCES = {
    "2d": ROOT / "src/QS3D.Core/BenchmarkParity/Qs2DQuantityAggregation.cs",
    "workflow": ROOT / "src/QS3D.Core/BenchmarkParity/Qs2DTakeoffWorkflow.cs",
    "workbook": ROOT / "src/QS3D.Core/BenchmarkParity/QsLiveWorkbook2.cs",
    "cubicost": ROOT / "src/QS3D.Core/BenchmarkParity/QsCubicostQuantityAggregation.cs",
    "package": ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageUx.cs",
}
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs2DQuantityAggregationPrecisionSmoke.cs"
PACKAGE_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageUxSmoke.cs"
RUNNER = ROOT / "tests/QS3D.Core.SmokeTests/BenchmarkParitySuiteSmoke.cs"
for path in (*SOURCES.values(), SMOKE, PACKAGE_SMOKE, RUNNER):
    if not path.is_file():
        raise SystemExit("C02 accumulator preflight missing file: " + str(path.relative_to(ROOT)))
texts = {name: path.read_text(encoding="utf-8") for name, path in SOURCES.items()}
for name, text in texts.items():
    if "QuantityReportMath.FiniteAccumulator()" not in text:
        raise SystemExit(f"C02 {name} does not use canonical FiniteAccumulator")
    for stale in ("var compensation = 0d;", "Math.Abs(sum) >= Math.Abs(value)"):
        if stale in text:
            raise SystemExit(f"C02 {name} retains stale local compensation: {stale}")
if 'QsModelElementSnapshot.Finite(value, "quantity")' not in texts["2d"]:
    raise SystemExit("C02 2D finite-input admission drifted")
if 'QsModelElementSnapshot.Finite(value, "measuredQuantity")' not in texts["workflow"]:
    raise SystemExit("C02 workflow finite-input admission drifted")
if "catch (InvalidOperationException)" not in texts["workbook"] or "catch (OverflowException)" not in texts["workbook"]:
    raise SystemExit("C02 workbook fail-closed error mapping drifted")
if "Invalid Cubicost inventory quantity" not in texts["cubicost"]:
    raise SystemExit("C02 Cubicost non-finite diagnostic contract drifted")
if "Cubicost inventory quantity overflow" not in texts["cubicost"]:
    raise SystemExit("C02 Cubicost overflow diagnostic contract drifted")
if 'accumulator.Add(line.EstimatedCost, "estimatedCost")' not in texts["package"]:
    raise SystemExit("C02 package estimated-cost aggregation drifted")
smoke = SMOKE.read_text(encoding="utf-8")
for token in (
    "PreservesPositiveHighDynamicResidual();",
    "WorkflowPreservesPositiveHighDynamicResidual();",
    "LiveWorkbookPreservesPositiveHighDynamicResidual();",
    "CubicostInventoryPreservesPositiveHighDynamicResidual();",
    "10000000000000004d",
    "result.Trace.Count != 4",
    "inventory.SourceCount != 4",
):
    if token not in smoke:
        raise SystemExit("C02 accumulator smoke missing contract: " + token)
package_smoke = PACKAGE_SMOKE.read_text(encoding="utf-8")
for token in ("PreservesHighDynamicRangeEstimatedCost();", "RejectsAggregateEstimatedCostOverflow();", "10000000000000004d", "estimated cost evidence count invariant", "aggregate estimated cost overflow"):
    if token not in package_smoke:
        raise SystemExit("C02 package accumulator smoke missing contract: " + token)
runner = RUNNER.read_text(encoding="utf-8")
if "Qs2DQuantityAggregationPrecisionSmoke.Run();" not in runner:
    raise SystemExit("C02 accumulator smoke is not wired into deterministic suite")
print("PASS C02 quantity accumulator canonicalization contract")
