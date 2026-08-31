#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallScheduleCommands.cs"

source = SOURCE.read_text(encoding="utf-8")

required = (
    "var glass = new CompensatedStatusTotal();",
    "var frame = new CompensatedStatusTotal();",
    "panels = QuantityReportMath.AddCount(panels, row.PanelCount);",
    "glass.Add(row.NetGlassAreaM2, \"Curtain export net glass area\");",
    "frame.Add(row.FrameLengthM, \"Curtain export frame length\");",
    "var glassTotal = glass.Value(\"Curtain export net glass area\");",
    "var frameTotal = frame.Value(\"Curtain export frame length\");",
    "private sealed class CompensatedStatusTotal",
    "var incoming = QuantityReportMath.NonNegative(value, label);",
    "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
    "if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))",
    "private static bool IsStrictlyBelowHalfUlp(double current, double compensation)",
    "glassTotal.ToString(\"0.###\")",
    "frameTotal.ToString(\"0.###\")",
)
for token in required:
    if token not in source:
        raise SystemExit(f"Curtain export status aggregation precision guard missing source contract: {token}")

for forbidden in (
    "glass = QuantityReportMath.Add(glass, row.NetGlassAreaM2",
    "frame = QuantityReportMath.Add(frame, row.FrameLengthM",
    "glass += row.NetGlassAreaM2",
    "frame += row.FrameLengthM",
):
    if forbidden in source:
        raise SystemExit(f"Curtain export status aggregation precision regressed to pairwise accumulation: {forbidden}")

finalize_glass = source.find('var glassTotal = glass.Value("Curtain export net glass area");')
finalize_frame = source.find('var frameTotal = frame.Value("Curtain export frame length");')
export_call = source.find("CurtainWallXlsxExporter.Export(dialog.FileName, rows);")
if min(finalize_glass, finalize_frame, export_call) < 0 or not (finalize_glass < export_call and finalize_frame < export_call):
    raise SystemExit("Curtain export status totals must finalize before XLSX publication.")

print("PASS curtain export compensated status aggregation precision source guard")
