from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsLiveWorkbook2.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "var aggregateInputs = new List<double> { sourceValue };",
    "aggregateInputs.Add(dependency.Value);",
    "if (!TryCompensatedSum(aggregateInputs, out aggregateValue))",
    '"Refresh source/dependency aggregation produced a non-finite value."',
    "var value = aggregateValue * binding.Multiplier + binding.Offset;",
]
for marker in required:
    if marker not in text:
        raise SystemExit(f"missing live workbook source/dependency aggregation marker: {marker}")

for forbidden in [
    "var value = (sourceValue + dependencyValue) * binding.Multiplier + binding.Offset;",
    "TryCompensatedSum(dependencyValues, out dependencyValue)",
]:
    if forbidden in text:
        raise SystemExit(f"live workbook must not split source from dependency aggregation: {forbidden}")

print("live workbook source residual aggregation guard: PASS")
