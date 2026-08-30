#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DoorOpeningScheduleCommands.cs"

source = SOURCE.read_text(encoding="utf-8")

required = (
    "var area = new CompensatedExportAreaTotal();",
    "count = QuantityReportMath.AddCount(count, row.Count);",
    "area.Add(row.OpeningAreaM2, \"Door/Opening export area\");",
    "var totalAreaM2 = area.Value(\"Door/Opening export area\");",
    "private sealed class CompensatedExportAreaTotal",
    "var incoming = QuantityReportMath.NonNegative(value, label);",
    "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
    "if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))",
    "private static bool IsStrictlyBelowHalfUlp(double current, double compensation)",
    'totalAreaM2.ToString("0.###") + " m²',
)
for token in required:
    if token not in source:
        raise SystemExit(f"Door XLSX status-area precision guard missing source contract: {token}")

for forbidden in (
    'area = QuantityReportMath.Add(area, row.OpeningAreaM2, "Door/Opening export area")',
    "area += row.OpeningAreaM2",
):
    if forbidden in source:
        raise SystemExit(f"Door XLSX status-area precision regressed to pairwise accumulation: {forbidden}")

loop = source.find("foreach (var row in rows)")
finalize = source.find('var totalAreaM2 = area.Value("Door/Opening export area");')
export = source.find("DoorOpeningXlsxExporter.Export(dialog.FileName, rows);")
status = source.find('totalAreaM2.ToString("0.###") + " m²')
if min(loop, finalize, export, status) < 0:
    raise SystemExit("Door XLSX status-area precision guard cannot bind traversal/finalization/export ordering.")
if not (loop < finalize < export < status):
    raise SystemExit("Door XLSX status area must finalize after row traversal, before export/status formatting.")

print("PASS Door XLSX compensated status-area precision source guard")
