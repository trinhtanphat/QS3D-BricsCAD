#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityGeometryExplanation.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QuantityGeometryExplanationAggregationPrecisionSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "var compensation = 0d;",
    "AddCompensated(ref total, ref compensation, value, label + \"[\" + index + \"]\");",
    "return FinalizeCompensated(total, compensation, label);",
    "private static void AddCompensated(ref double total, ref double compensation, double value, string label)",
    "var correction = Math.Abs(total) >= Math.Abs(value)",
    "private static double FinalizeCompensated(double total, double compensation, string label)",
    "if (compensation != 0d && result == total && !IsStrictlyBelowHalfUlp(total, compensation))",
    "private static bool IsStrictlyBelowHalfUlp(double current, double compensation)",
)
for token in required_source:
    if token not in source:
        raise SystemExit(f"quantity geometry explanation aggregation guard missing source contract: {token}")

for forbidden in (
    "total = QuantityReportMath.Add(total, value, label);",
    "total += value",
):
    if forbidden in source:
        raise SystemExit(f"quantity geometry explanation aggregation regressed to pairwise accumulation: {forbidden}")

required_smoke = (
    "PreservesLargeFirstRepresentableTotals();",
    "PreservesSmallFirstRepresentableTotals();",
    "PreservesDeductionAggregationAndValidation();",
    "OrdinarySelectorsRemainIsolated();",
    "FinalUnrepresentableTotalStillFailsClosed();",
    "10000000000000002d",
    "9007199254740992d",
    "Capture<OverflowException>",
    "double.PositiveInfinity",
    "[ModuleInitializer]",
)
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"quantity geometry explanation aggregation guard missing smoke contract: {token}")

print("PASS quantity geometry explanation compensated aggregation precision source guard")
