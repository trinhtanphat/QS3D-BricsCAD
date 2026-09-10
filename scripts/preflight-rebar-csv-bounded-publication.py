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
        "SnapshotRows(rows)",
        "ValidateCsvByteCount(snapshots)",
        "WriteCsv(writer, snapshots)",
        "GetPreamble().Length",
        "checked(byteCount + amount)",
        "AddQuotedCsvBytes",
        "StrictUtf8WithBom.GetByteCount(value)",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        raise SystemExit(f"{path.name}: missing bounded CSV contract tokens: {missing}")
    forbidden = [
        "var content = ToCsv(rows);",
        "writer.Write(content);",
    ]
    present = [token for token in forbidden if token in text]
    if present:
        raise SystemExit(f"{path.name}: eager whole-file Export path remains: {present}")

print("PASS rebar CSV bounded publication source guard")
