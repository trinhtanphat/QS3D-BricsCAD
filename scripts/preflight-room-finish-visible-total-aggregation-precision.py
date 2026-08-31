#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RoomFinishScheduleWindow.xaml.cs"

source = SOURCE.read_text(encoding="utf-8")

required = (
    "var length = new CompensatedVisibleTotal();",
    "var area = new CompensatedVisibleTotal();",
    "elementCount = QuantityReportMath.AddCount(elementCount, row.Count);",
    "length.Add(row.LengthM, \"HT_Phòng visible length\");",
    "area.Add(row.AreaM2, \"HT_Phòng visible area\");",
    "var totalLengthM = length.Value(\"HT_Phòng visible length\");",
    "var totalAreaM2 = area.Value(\"HT_Phòng visible area\");",
    "private sealed class CompensatedVisibleTotal",
    "var incoming = QuantityReportMath.NonNegative(value, label);",
    "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
    "if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))",
    "private static bool IsStrictlyBelowHalfUlp(double current, double compensation)",
    "LengthText.Text = totalLengthM.ToString(\"0.###\", CultureInfo.InvariantCulture) + \" m\";",
    "AreaText.Text = totalAreaM2.ToString(\"0.###\", CultureInfo.InvariantCulture) + \" m²\";",
)
for token in required:
    if token not in source:
        raise SystemExit(f"Room Finish visible-total aggregation precision guard missing source contract: {token}")

for forbidden in (
    "totalLengthM = QuantityReportMath.Add(totalLengthM, row.LengthM",
    "totalAreaM2 = QuantityReportMath.Add(totalAreaM2, row.AreaM2",
    "totalLengthM += row.LengthM",
    "totalAreaM2 += row.AreaM2",
):
    if forbidden in source:
        raise SystemExit(f"Room Finish visible-total aggregation precision regressed to pairwise accumulation: {forbidden}")

loop = source.find("foreach (var row in visible)")
finalize_length = source.find('var totalLengthM = length.Value("HT_Phòng visible length");')
finalize_area = source.find('var totalAreaM2 = area.Value("HT_Phòng visible area");')
format_length = source.find('LengthText.Text = totalLengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m";')
format_area = source.find('AreaText.Text = totalAreaM2.ToString("0.###", CultureInfo.InvariantCulture) + " m²";')
if min(loop, finalize_length, finalize_area, format_length, format_area) < 0:
    raise SystemExit("Room Finish visible-total aggregation precision guard cannot bind accumulation/finalization order.")
if not (loop < finalize_length < format_length and loop < finalize_area < format_area):
    raise SystemExit("Room Finish visible totals must finalize after row traversal and before display formatting.")

print("PASS Room Finish visible Length/Area compensated aggregation precision source guard")
