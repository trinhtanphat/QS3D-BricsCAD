from pathlib import Path

SOURCE = Path("src/QS3D.Core/BenchmarkParity/Qs2DTakeoffWorkflow.cs")
text = SOURCE.read_text(encoding="utf-8")

required = [
    "var markupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);",
    "Evidence = new ReadOnlyCollection<TakeoffQuantityEvidence2D>(evidence.ToList());",
    "Takeoff evidence belongs to another sheet revision.",
    "Takeoff evidence belongs to another sheet source.",
    "Duplicate takeoff evidence markup id.",
]
missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("takeoff evidence snapshot regression: missing " + ", ".join(missing))

if "Evidence = evidence;" in text:
    raise SystemExit("takeoff evidence snapshot regression: mutable caller collection is still retained")

print("takeoff evidence snapshot preflight: OK")
