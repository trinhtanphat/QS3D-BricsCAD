from pathlib import Path

root = Path(__file__).resolve().parents[1]
files = [
    root / "src/QS3D.Core/Export/RebarCsvExporter.cs",
    root / "src/QS3D.Core/Export/RebarProcurementCsvExporter.cs",
]

for path in files:
    text = path.read_text(encoding="utf-8")
    required = [
        "MaxCsvBytes",
        "StrictUtf8WithBom",
        "InvalidDataException",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        raise SystemExit(f"{path.name}: missing bounded CSV contract tokens: {missing}")
    if "new StringBuilder()" in text and "MaxCsvBytes" not in text:
        raise SystemExit(f"{path.name}: unbounded whole-file StringBuilder remains")

print("PASS rebar CSV bounded publication source guard")
