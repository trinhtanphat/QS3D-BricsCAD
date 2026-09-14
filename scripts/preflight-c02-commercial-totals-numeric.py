from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
src = (root / "src/QS3D.Core/BenchmarkParity/QsBenchmarkParitySuite.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/BenchmarkParitySuiteSmoke.cs").read_text(encoding="utf-8")

checks = [
    ("finite progress admission", "double.IsNaN(progress)" in src and "double.IsInfinity(progress)" in src),
    ("summary weighted progress admission", "weightedProgress" in src and "double.IsNaN(weightedProgress)" in src and "double.IsInfinity(weightedProgress)" in src),
    ("decimal weighted accumulator", "(decimal)x.Progress" in src or "decimal progress" in src),
    ("high dynamic smoke", "PO-HUGE" in smoke and "weighted progress order invariance" in smoke),
]
failed = [name for name, ok in checks if not ok]
if failed:
    for name in failed:
        print("FAIL:", name)
    sys.exit(1)
print("PASS: C02 commercial totals numeric contract")
