#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SCHEDULE = ROOT / "src/QS3D.Core/Rebar/RebarSchedule.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
CSV = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"

errors = []
for path in (SCHEDULE, COMMANDS, CSV, WINDOW):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    required = (
        "public sealed class RebarScheduleTotals",
        "public static RebarScheduleTotals CalculateTotals(IReadOnlyList<RebarScheduleRow> rows)",
        "var totalLength = new CompensatedNonNegativeTotal();",
        "var totalWeight = new CompensatedNonNegativeTotal();",
        'totalLength.Add(row.TotalLengthM, "BBS aggregate length");',
        'totalWeight.Add(row.TotalWeightKg, "BBS aggregate weight");',
        'var length = totalLength.Value("BBS aggregate length");',
        'var weight = totalWeight.Value("BBS aggregate weight");',
        "private struct CompensatedNonNegativeTotal",
        "var incoming = RebarMath.NonNegative(value, label);",
        "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
        "if (_compensation != 0d && result == _sum && !IsAtMostHalfUlp(_sum, _compensation))",
        "private static bool IsAtMostHalfUlp(double current, double compensation)",
        "return Math.Abs(compensation) <= spacing / 2d;",
    )
    for token in required:
        if token not in text:
            errors.append("BBS aggregate precision contract missing Core token: " + token)
    for forbidden in (
        'totalLength = RebarMath.Add(totalLength, row.TotalLengthM, "BBS aggregate length")',
        'totalWeight = RebarMath.Add(totalWeight, row.TotalWeightKg, "BBS aggregate weight")',
        "IsStrictlyBelowHalfUlp",
    ):
        if forbidden in text:
            errors.append("BBS aggregate precision regressed to pairwise/over-strict accumulation: " + forbidden)

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        "var totals = RebarScheduleBuilder.CalculateTotals(rows);",
        'totals.TotalWeightKg.ToString("0.###")',
    ):
        if token not in text:
            errors.append("QS3DBBS must use canonical compensated totals: " + token)
    if 'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")' in text:
        errors.append("QS3DBBS must not re-sum BBS weight pairwise after Core validation")

if CSV.is_file():
    text = CSV.read_text(encoding="utf-8")
    for token in (
        "var totals = RebarScheduleBuilder.CalculateTotals(rows);",
        'totals.TotalWeightKg.ToString("0.###")',
    ):
        if token not in text:
            errors.append("QS3DBBSCSV must use canonical compensated totals: " + token)
    if 'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS CSV total weight")' in text:
        errors.append("QS3DBBSCSV must not re-sum BBS weight pairwise after Core validation")

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        "var totals = RebarScheduleBuilder.CalculateTotals(_rows);",
        "totals.Quantity",
        "totals.TotalLengthM",
        "totals.TotalWeightKg",
    ):
        if token not in text:
            errors.append("BBS modeless totals must use canonical compensated totals: " + token)
    for forbidden in (
        'QuantityReportMath.Add(totalLengthM, row.TotalLengthM, "BBS visible total length")',
        'QuantityReportMath.Add(totalWeightKg, row.TotalWeightKg, "BBS visible total weight")',
    ):
        if forbidden in text:
            errors.append("BBS modeless totals must not use pairwise presentation accumulation: " + forbidden)

if errors:
    print("BBS aggregate precision preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS schedule validation, XLSX/CSV status and modeless totals share one compensated finite aggregate contract with ordinary half-ULP rounding preserved.")
