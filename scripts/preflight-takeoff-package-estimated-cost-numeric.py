from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
workflow = (ROOT / "src/QS3D.Core/BenchmarkParity/Qs2DTakeoffWorkflow.cs").read_text(encoding="utf-8")
package = (ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageUx.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageUxSmoke.cs").read_text(encoding="utf-8")

required = {
    "line estimated-cost finite fence": (workflow, 'EstimatedCost = QsModelElementSnapshot.Finite(FormulaQuantity * UnitRate, "estimatedCost")'),
    "package canonical accumulator": (package, "var accumulator = new QuantityReportMath.FiniteAccumulator();"),
    "package canonical add": (package, 'accumulator.Add(line.EstimatedCost, "estimatedCost");'),
    "package canonical finite publication": (package, 'return accumulator.Value("estimatedCost");'),
    "aggregate overflow mapping": (package, 'throw new ArgumentOutOfRangeException("estimatedCost", "must be finite");'),
    "line overflow regression": (smoke, "RejectsNonFiniteLineEstimatedCost();"),
    "aggregate overflow regression": (smoke, "RejectsAggregateEstimatedCostOverflow();"),
    "high dynamic range regression": (smoke, "PreservesHighDynamicRangeEstimatedCost();"),
    "signed-zero regression": (smoke, "CanonicalizesSignedZeroEstimatedCost();"),
}

missing = [name for name, (text, needle) in required.items() if needle not in text]
if "Inventory.Sum(x => x.EstimatedCost)" in package:
    missing.append("naive Inventory.Sum estimated-cost fold still present")

if missing:
    for item in missing:
        print(f"ERROR: {item}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: Takeoff package estimated-cost arithmetic uses the canonical finite accumulator with overflow and signed-zero regressions.")
