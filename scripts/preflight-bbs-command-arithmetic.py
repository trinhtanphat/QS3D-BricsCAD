#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs"
SCHEDULE = ROOT / "src/QS3D.Core/Rebar/RebarSchedule.cs"
REBAR_MATH = ROOT / "src/QS3D.Core/Rebar/RebarMath.cs"
REGRESSION = ROOT / "tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs"
errors = []

for path in (COMMANDS, WINDOW, EXPORTER, SCHEDULE, REBAR_MATH, REGRESSION):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    required = (
        'var totals = RebarScheduleBuilder.CalculateTotals(rows);',
        'totals.TotalWeightKg.ToString("0.###")',
    )
    for token in required:
        if token not in text:
            errors.append("QS3DBBS missing safe aggregate token: " + token)
    if "rows.Sum(x => x.TotalWeightKg)" in text:
        errors.append("QS3DBBS must not use unchecked LINQ Sum for total weight")
    if 'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")' in text:
        errors.append("QS3DBBS must not restore pairwise status aggregation after canonical BBS validation")
    total_index = text.find('var totals = RebarScheduleBuilder.CalculateTotals(rows);')
    dialog_index = text.find('Title = "Xuất Bar Bending Schedule"')
    confirm_index = text.find("if (dialog.ShowDialog() != true) return;", dialog_index + 1)
    export_index = text.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
    if min(total_index, dialog_index, confirm_index, export_index) >= 0 and not total_index < dialog_index < confirm_index < export_index:
        errors.append("QS3DBBS must validate finite aggregate weight before asking for a destination, then write only after Save confirmation")

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        'var totals = RebarScheduleBuilder.CalculateTotals(_rows);',
        'totals.Quantity',
        'totals.TotalLengthM',
        'totals.TotalWeightKg',
    ):
        if token not in text:
            errors.append("BBS modeless visible total missing canonical aggregate token: " + token)
    if 'QuantityReportMath.Add(totalWeightKg, row.TotalWeightKg, "BBS visible total weight")' in text:
        errors.append("BBS modeless visible total must not restore pairwise status aggregation")

if EXPORTER.is_file():
    text = EXPORTER.read_text(encoding="utf-8")
    if "double.IsNaN(value) || double.IsInfinity(value)" not in text:
        errors.append("BBS XLSX exporter must reject non-finite numeric cells")
    if "AtomicFileCommit.ReplaceWithoutBackup" not in text:
        errors.append("BBS XLSX exporter must retain atomic final-file replacement")

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    for token in (
        'if (row == null) throw new InvalidOperationException("BBS row cannot be null.");',
        'var totalLength = new CompensatedNonNegativeTotal();',
        'var totalWeight = new CompensatedNonNegativeTotal();',
        'var length = totalLength.Value("BBS aggregate length");',
        'var weight = totalWeight.Value("BBS aggregate weight");',
    ):
        if token not in text:
            errors.append("BBS canonical aggregate missing safety token: " + token)
    if 'RebarMath.CeilingNearInteger(intervals, "spacing interval count")' not in text:
        errors.append("BBS spacing count must use the shared bounded-ULP interval ceiling")
    if "Math.Max(1d, Math.Abs(intervals)) * 1e-12d" in text:
        errors.append("BBS spacing count must not use scale-growing relative tolerance")

if REBAR_MATH.is_file():
    text = REBAR_MATH.read_text(encoding="utf-8")
    required = (
        "public static double CeilingNearInteger(double value, string label)",
        "IntegerSnapTolerance(value)",
        "BitConverter.DoubleToInt64Bits(magnitude)",
        "BitConverter.Int64BitsToDouble(bits + 1L)",
        "return (next - magnitude) * 8d;",
    )
    for token in required:
        if token not in text:
            errors.append("BBS spacing count missing bounded ULP snap token: " + token)

if REGRESSION.is_file():
    text = REGRESSION.read_text(encoding="utf-8")
    required = (
        "SpacingRealOverrunIsNotSnappedAtLargeScale();",
        "DistributionLengthM = 2000000.000001d",
        "Equal(2000000002, actualOverrun.Quantity);",
    )
    for token in required:
        if token not in text:
            errors.append("BBS spacing count missing large-scale overrun regression token: " + token)

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] QS3DBBS validates canonical compensated aggregate totals before SaveFileDialog, writes only after confirmation, uses bounded-ULP spacing counts, and matches modeless BBS arithmetic")
